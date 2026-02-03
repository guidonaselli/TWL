using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TWL.Server.Architecture.Observability;
using TWL.Server.Simulation.Managers;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;

namespace Benchmarks;

public class MockPlayerRepository : IPlayerRepository
{
    public PlayerSaveData? Load(int userId) => null;
    public Task SaveAsync(int userId, PlayerSaveData data) => Task.CompletedTask;
    public Task<PlayerSaveData?> LoadAsync(int userId) => Task.FromResult<PlayerSaveData?>(null);
}

public class TestClientSession : ClientSession
{
    public TestClientSession(int id, bool active)
    {
        UserId = id;
        if (active)
        {
            Character = new ServerCharacter
            {
                Id = id,
                Hp = 100,
                Name = $"Player{id}"
            };
        }
        else
        {
            Character = null;
        }
    }
}

public class AllocationBenchmark
{
    private PlayerService _playerService;
    private const int TotalSessions = 2000;
    private const int ActiveSessions = 100;
    private const int Iterations = 10000;

    public void Setup()
    {
        var repo = new MockPlayerRepository();
        var metrics = new ServerMetrics();
        _playerService = new PlayerService(repo, metrics);

        for (int i = 0; i < TotalSessions; i++)
        {
            // First 100 are active
            bool active = i < ActiveSessions;
            var session = new TestClientSession(i + 1, active);
            _playerService.RegisterSession(session);
        }
    }

    public void Run()
    {
        Console.WriteLine($"Running AllocationBenchmark: {Iterations} iterations, {TotalSessions} sessions ({ActiveSessions} active).");

        GC.Collect();
        GC.WaitForPendingFinalizers();
        long startBytes = GC.GetAllocatedBytesForCurrentThread();
        var sw = Stopwatch.StartNew();

        long totalItems = 0;
        for (int i = 0; i < Iterations; i++)
        {
            var list = _playerService.GetAllSessions()
                .Where(s => s.Character != null && s.Character.Hp > 0)
                .ToList();
            totalItems += list.Count;
        }

        sw.Stop();
        long endBytes = GC.GetAllocatedBytesForCurrentThread();

        Console.WriteLine($"[Baseline] Time: {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"[Baseline] Allocations: {(endBytes - startBytes) / 1024.0 / 1024.0:F2} MB");
        Console.WriteLine($"[Baseline] Total Active Found: {totalItems}");

        // Optimized Run
        GC.Collect();
        GC.WaitForPendingFinalizers();
        startBytes = GC.GetAllocatedBytesForCurrentThread();
        sw.Restart();

        totalItems = 0;
        var buffer = new List<ClientSession>();
        for (int i = 0; i < Iterations; i++)
        {
            _playerService.GetSessions(buffer, s => s.Character != null && s.Character.Hp > 0);
            totalItems += buffer.Count;
        }

        sw.Stop();
        endBytes = GC.GetAllocatedBytesForCurrentThread();

        Console.WriteLine($"[Optimized] Time: {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"[Optimized] Allocations: {(endBytes - startBytes) / 1024.0 / 1024.0:F2} MB");
        Console.WriteLine($"[Optimized] Total Active Found: {totalItems}");
    }
}
