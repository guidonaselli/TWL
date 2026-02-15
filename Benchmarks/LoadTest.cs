using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
using TWL.Shared.Domain.Skills;
using TWL.Shared.Net.Network;
using TWL.Shared.Net.Payloads;
using TWL.Shared.Services;

namespace Benchmarks;

public class MockDbService : DbService
{
    public MockDbService() : base("Host=dummy") { }

    public override void Init() { /* No-op */ }
    public override void InitDatabase() { /* No-op */ }

    public override Task<int> CheckLoginAsync(string username, string passHash)
    {
        // Simple hash of username to int for deterministic ID
        int id = Math.Abs(username.GetHashCode());
        if (id == 0) id = 1;
        return Task.FromResult(id);
    }
}

public class MockRandomService : IRandomService
{
    private readonly Random _random = new Random(12345);
    public int Next(string? context = null) => _random.Next();
    public int Next(int minValue, int maxValue, string? context = null) => _random.Next(minValue, maxValue);
    public double NextDouble(string? context = null) => _random.NextDouble();
    public float NextFloat(string? context = null) => _random.NextSingle();
    public float NextFloat(float min, float max, string? context = null) => min + (float)_random.NextDouble() * (max - min);
}

public class LoadTest
{
    private const int BotCount = 50;
    private const int DurationSeconds = 10;
    private const int TickRateMs = 100;

    private readonly NetworkServer _server;
    private readonly ServerMetrics _metrics;
    private readonly EconomyManager _economy;
    private readonly PlayerService _playerService;

    public LoadTest()
    {
        // Setup Server Dependencies
        _metrics = new ServerMetrics();
        var db = new MockDbService();
        var random = new MockRandomService();

        var skillCatalog = SkillRegistry.Instance; // Assuming singleton available or I can mock it
        // If SkillRegistry is empty, some things might fail, but for basic load test it might be fine.

        // Mocks for simple stuff
        var statusEngine = new StatusEngine(); // Concrete is fine
        var combatResolver = new StandardCombatResolver(random, skillCatalog);
        var combatManager = new CombatManager(combatResolver, random, skillCatalog, statusEngine);

        var petManager = new PetManager();
        var questManager = new ServerQuestManager();
        var interactionManager = new InteractionManager();

        // Repo
        var repo = new FilePlayerRepository("LoadTest_Saves"); // Temp folder
        _playerService = new PlayerService(repo, _metrics);

        _economy = new EconomyManager("loadtest_economy.log");

        var monsterManager = new MonsterManager();
        var petService = new PetService(_playerService, petManager, monsterManager, combatManager, random, NullLogger<PetService>.Instance);
        var mapLoader = new MapLoader(NullLogger<MapLoader>.Instance);
        var scheduler = new WorldScheduler(NullLogger<WorldScheduler>.Instance, _metrics);
        var mapRegistry = new MapRegistry(NullLogger<MapRegistry>.Instance, mapLoader);
        var worldTrigger = new WorldTriggerService(NullLogger<WorldTriggerService>.Instance, _metrics, _playerService, scheduler, mapRegistry);
        // Load some dummy map or skip map loading? For load test, if bots move, they might trigger stuff if maps loaded.
        // For simplicity, we skip loading maps, so CheckTriggers returns early.

        var spawnManager = new SpawnManager(monsterManager, combatManager, random, _playerService);

        var mediator = new Mediator(); // Using concrete Mediator for load test
        _server = new NetworkServer(0, db, petManager, questManager, combatManager, interactionManager,
            _playerService, _economy, _metrics, petService, mediator, worldTrigger, spawnManager);
    }

    public async Task RunAsync()
    {
        Console.WriteLine($"[LoadTest] Starting Server with {BotCount} bots for {DurationSeconds}s...");

        // Clean up previous runs
        if (Directory.Exists("LoadTest_Saves")) Directory.Delete("LoadTest_Saves", true);
        Directory.CreateDirectory("LoadTest_Saves"); // Ensure it exists
        if (File.Exists("loadtest_economy.log")) File.Delete("loadtest_economy.log");
        if (File.Exists("loadtest_economy.snapshot.json")) File.Delete("loadtest_economy.snapshot.json");

        _playerService.Start();
        _server.Start();
        var port = _server.Port;
        Console.WriteLine($"[LoadTest] Server listening on port {port}");

        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(DurationSeconds));

        var bots = new List<Task>();
        var errors = new ConcurrentBag<string>();
        var requestCount = 0;

        for (int i = 0; i < BotCount; i++)
        {
            int botId = i;
            bots.Add(Task.Run(async () =>
            {
                try
                {
                    await RunBotAsync(botId, port, cts.Token, () => Interlocked.Increment(ref requestCount));
                }
                catch (OperationCanceledException)
                {
                    // Expected
                }
                catch (Exception ex)
                {
                    errors.Add($"Bot {botId}: {ex.Message}");
                }
            }));
            await Task.Delay(20); // Stagger login
        }

        try
        {
            await Task.WhenAll(bots);
        }
        catch { }

        Console.WriteLine("[LoadTest] Stopping Server...");
        _server.Stop();
        await _playerService.StopAsync();
        if (_economy is IDisposable disp) disp.Dispose();

        // Report
        Console.WriteLine("\n=== Load Test Report ===");
        Console.WriteLine($"Total Requests: {requestCount}");
        Console.WriteLine($"Requests/Sec: {requestCount / (double)DurationSeconds:F2}");
        Console.WriteLine($"Total Errors: {errors.Count}");

        var snapshot = _metrics.GetSnapshot();
        Console.WriteLine(snapshot.ToString());

        if (errors.Count > 0)
        {
            Console.WriteLine("Sample Errors:");
            foreach (var err in errors.Take(5))
            {
                Console.WriteLine(err);
            }
        }

        // Clean up
        if (Directory.Exists("LoadTest_Saves")) Directory.Delete("LoadTest_Saves", true);
        if (File.Exists("loadtest_economy.log")) File.Delete("loadtest_economy.log");
    }

    private async Task RunBotAsync(int id, int port, CancellationToken token, Action onRequest)
    {
        using var client = new TcpClient();
        client.NoDelay = true;
        await client.ConnectAsync("127.0.0.1", port);
        using var stream = client.GetStream();

        // Login
        var username = $"bot_{id}";
        // 64-char hex string for passhash
        var passHash = new string('a', 64);
        var loginMsg = new NetMessage
        {
            Op = Opcode.LoginRequest,
            JsonPayload = JsonSerializer.Serialize(new { Username = username, PassHash = passHash })
        };
        await SendAsync(stream, loginMsg);

        // Read Login Response (Blocking read for simplicity in harness)
        var buffer = new byte[4096];
        int read = await stream.ReadAsync(buffer, 0, buffer.Length, token);
        if (read <= 0)
        {
             throw new Exception("Login failed (no response)");
        }

        var random = new Random(id);

        while (!token.IsCancellationRequested)
        {
            // Move
            var moveMsg = new NetMessage
            {
                Op = Opcode.MoveRequest,
                JsonPayload = JsonSerializer.Serialize(new { dx = random.Next(-1, 2), dy = random.Next(-1, 2) })
            };
            await SendAsync(stream, moveMsg);
            onRequest();

            // Buy Item occasionally
            if (random.Next(10) == 0)
            {
                var buyMsg = new NetMessage
                {
                    Op = Opcode.BuyShopItemRequest,
                    JsonPayload = JsonSerializer.Serialize(new { ShopItemId = 1, Quantity = 1 })
                };
                await SendAsync(stream, buyMsg);
                onRequest();
            }

            await Task.Delay(TickRateMs, token);
        }
    }

    private async Task SendAsync(NetworkStream stream, NetMessage msg)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(msg);
        await stream.WriteAsync(bytes, 0, bytes.Length);
    }
}
