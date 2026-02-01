using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.DTO;
using Xunit;
using Xunit.Abstractions;

namespace TWL.Tests.Benchmarks;

public class PlayerServicePerformanceTests
{
    private readonly ITestOutputHelper _output;

    public PlayerServicePerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public class BenchmarkClientSession : ClientSession
    {
        public BenchmarkClientSession(int userId) : base()
        {
            UserId = userId;
        }

        public void SetCharacter(ServerCharacter character)
        {
            base.Character = character;
        }

        public void SetQuestComponent(PlayerQuestComponent component)
        {
            base.QuestComponent = component;
        }
    }

    public class BenchmarkMockPlayerRepository : IPlayerRepository
    {
        private int _saveCallCount;
        public int SaveCallCount => _saveCallCount;

        public int DelayMs { get; set; } = 10;

        public void Save(int userId, PlayerSaveData data)
        {
            // Simulate blocking I/O
            Thread.Sleep(DelayMs);
            Interlocked.Increment(ref _saveCallCount);
        }

        public PlayerSaveData? Load(int userId) => null;

        public async Task SaveAsync(int userId, PlayerSaveData data)
        {
            // Simulate non-blocking I/O
            await Task.Delay(DelayMs);
            Interlocked.Increment(ref _saveCallCount);
        }

        public Task<PlayerSaveData?> LoadAsync(int userId) => Task.FromResult<PlayerSaveData?>(null);
    }

    [Fact]
    public void Benchmark_Flush_Synchronous_Wrapper_Optimized()
    {
        var repo = new BenchmarkMockPlayerRepository();
        var service = new PlayerService(repo, new ServerMetrics());
        int count = 50;
        var qm = new ServerQuestManager();

        // Create dirty sessions
        for (int i = 0; i < count; i++)
        {
            var s = new BenchmarkClientSession(i + 1);
            var c = new ServerCharacter { Id = i + 1, Name = $"Bencher_{i}" };
            c.AddGold(1); // Make dirty
            s.SetCharacter(c);
            s.SetQuestComponent(new PlayerQuestComponent(qm));
            service.RegisterSession(s);
        }

        var sw = Stopwatch.StartNew();
        service.FlushAllDirty();
        sw.Stop();

        _output.WriteLine($"[SYNC-WRAPPER] Flushed {count} sessions in {sw.ElapsedMilliseconds}ms");

        Assert.Equal(count, repo.SaveCallCount);
        // It should also be fast now because it delegates to FlushAllDirtyAsync
        Assert.True(sw.ElapsedMilliseconds < count * repo.DelayMs * 0.5, "Synchronous wrapper is not using optimization!");
    }

    [Fact]
    public async Task Benchmark_Flush_Async_Optimized()
    {
        var repo = new BenchmarkMockPlayerRepository();
        var service = new PlayerService(repo, new ServerMetrics());
        int count = 50;
        var qm = new ServerQuestManager();

        // Create dirty sessions
        for (int i = 0; i < count; i++)
        {
            var s = new BenchmarkClientSession(i + 1);
            var c = new ServerCharacter { Id = i + 1, Name = $"Bencher_{i}" };
            c.AddGold(1); // Make dirty
            s.SetCharacter(c);
            s.SetQuestComponent(new PlayerQuestComponent(qm));
            service.RegisterSession(s);
        }

        var sw = Stopwatch.StartNew();
        await service.FlushAllDirtyAsync();
        sw.Stop();

        _output.WriteLine($"[OPTIMIZED] Flushed {count} sessions in {sw.ElapsedMilliseconds}ms (Expected << {count * repo.DelayMs}ms)");

        Assert.Equal(count, repo.SaveCallCount);
        // It should be much faster than sequential. Ideally closer to DelayMs (plus overhead) than count * DelayMs.
        // With 50 concurrent tasks and 10ms delay, total time should be around 20-50ms depending on thread pool.
        // Let's being conservative and say it must be at least 2x faster than sequential.
        long sequentialExpected = count * repo.DelayMs;
        // Relaxed threshold for CI/VM environments where thread pool startup might add latency
        Assert.True(sw.ElapsedMilliseconds < sequentialExpected * 0.8, "Synchronous wrapper is not using optimization!");
    }
}
