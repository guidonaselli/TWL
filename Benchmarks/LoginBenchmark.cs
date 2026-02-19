using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using TWL.Server.Architecture.Observability;
using TWL.Server.Architecture.Pipeline;
using TWL.Server.Features.Combat;
using TWL.Server.Features.Interactions;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Database;
using TWL.Server.Persistence.Services;
using TWL.Server.Services;
using TWL.Server.Services.World;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.DTO;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Net.Network;
using TWL.Shared.Net.Payloads;
using TWL.Shared.Services;
using TWL.Server.Security;

namespace Benchmarks;

public class LoginBenchmark
{
    private const int BotCount = 100;
    private const string SaveDir = "LoginBenchmark_Saves";

    private NetworkServer _server;
    private ServerMetrics _metrics;
    private PlayerService _playerService;
    private MockDbService _db;

    public async Task RunAsync()
    {
        Console.WriteLine($"[LoginBenchmark] Preparing {BotCount} users...");

        // 1. Setup Environment
        if (Directory.Exists(SaveDir)) Directory.Delete(SaveDir, true);
        Directory.CreateDirectory(SaveDir);

        _metrics = new ServerMetrics();
        _db = new MockDbService(); // Reuse from LoadTest.cs logic
        var repo = new InMemoryPlayerRepository();
        _playerService = new PlayerService(repo, _metrics);

        // Populate dummy data so LoadData hits the disk
        Console.WriteLine("[LoginBenchmark] Pre-populating save files...");
        for (int i = 0; i < BotCount; i++)
        {
             var username = $"bot_{i}";
             int id = await _db.CheckLoginAsync(username, ""); // Get ID from mock DB logic

             var saveData = new PlayerSaveData
             {
                 Character = new ServerCharacterData
                 {
                     Id = id,
                     Name = username,
                     Hp = 100,
                     // CharacterElement is not in ServerCharacterData but in ServerCharacter, assuming it's loaded from other fields or defaulted
                     // Looking at ServerCharacterData, it doesn't have CharacterElement enum, maybe it is stored as something else?
                     // But wait, ServerCharacterData has many properties but no Element?
                     // Let's re-read ServerCharacterData definition in previous memory.
                 },
                 Quests = new QuestData()
             };
             await repo.SaveAsync(id, saveData);
        }

        // 2. Setup Server
        var random = new MockRandomService();
        var combatManager = new CombatManager(new StandardCombatResolver(random, SkillRegistry.Instance), random, SkillRegistry.Instance, new StatusEngine());
        var petManager = new PetManager();
        var questManager = new ServerQuestManager();
        var interactionManager = new InteractionManager();
        var economy = new EconomyManager("login_bench_economy.log");
        var monsterManager = new MonsterManager();
        var petService = new PetService(_playerService, petManager, monsterManager, combatManager, random, NullLogger<PetService>.Instance);
        var spawnManager = new SpawnManager(monsterManager, combatManager, random, _playerService);

        var mapLoader = new MapLoader(NullLogger<MapLoader>.Instance);
        var scheduler = new WorldScheduler(NullLogger<WorldScheduler>.Instance, _metrics);
        var mapRegistry = new MapRegistry(NullLogger<MapRegistry>.Instance, mapLoader);
        var worldTrigger = new WorldTriggerService(NullLogger<WorldTriggerService>.Instance, _metrics, _playerService, scheduler, mapRegistry);

        _server = new NetworkServer(0, _db, petManager, questManager, combatManager, interactionManager,
            _playerService, economy, _metrics, petService, new Mediator(), worldTrigger, spawnManager,
            new ReplayGuard(new ReplayGuardOptions()));

        _playerService.Start();
        _server.Start();
        var port = _server.Port;

        Console.WriteLine($"[LoginBenchmark] Server started on port {port}. Starting {BotCount} concurrent logins...");

        var sw = Stopwatch.StartNew();
        var tasks = new List<Task>();
        var errorCount = 0;

        // Launch all logins concurrently
        for (int i = 0; i < BotCount; i++)
        {
            int id = i;
            tasks.Add(Task.Run(async () => {
                try
                {
                    await PerformLogin(id, port);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref errorCount);
                    // Console.WriteLine($"Login failed for bot_{id}: {ex.Message}");
                }
            }));
        }

        await Task.WhenAll(tasks);
        sw.Stop();

        Console.WriteLine($"[LoginBenchmark] Completed in {sw.ElapsedMilliseconds} ms. Errors: {errorCount}");
        double throughput = BotCount / sw.Elapsed.TotalSeconds;
        Console.WriteLine($"Throughput: {throughput:F2} logins/sec");

        _server.Stop();
        await _playerService.StopAsync();
        if (economy is IDisposable d) d.Dispose();

        // Cleanup
        if (Directory.Exists(SaveDir)) Directory.Delete(SaveDir, true);
        if (File.Exists("login_bench_economy.log")) File.Delete("login_bench_economy.log");
        if (File.Exists("login_bench_economy.snapshot.json")) File.Delete("login_bench_economy.snapshot.json");
    }

    private async Task PerformLogin(int id, int port)
    {
        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", port);
        using var stream = client.GetStream();

        var username = $"bot_{id}";
        var passHash = new string('a', 64);
        var loginMsg = new NetMessage
        {
            Op = Opcode.LoginRequest,
            JsonPayload = JsonSerializer.Serialize(new LoginDTO { Username = username, PassHash = passHash })
        };

        var bytes = JsonSerializer.SerializeToUtf8Bytes(loginMsg);
        await stream.WriteAsync(bytes, 0, bytes.Length);

        // Read response
        var buffer = new byte[4096];
        int read = await stream.ReadAsync(buffer, 0, buffer.Length);
        if (read <= 0) throw new Exception("Disconnected");

        var response = NetMessage.Deserialize(buffer, read);
        if (response == null) throw new Exception("Failed to deserialize response");
        if (response.Op != Opcode.LoginResponse) throw new Exception($"Unexpected Op: {response.Op}");

        // Ensure success
        var loginResp = JsonSerializer.Deserialize<LoginResponseDto>(response.JsonPayload, new JsonSerializerOptions{ PropertyNameCaseInsensitive = true});
        if (loginResp == null || !loginResp.Success) throw new Exception("Login refused by server");
    }
}
