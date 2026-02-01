using System.Diagnostics;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using Xunit.Abstractions;

namespace TWL.Tests.Benchmarks;

public class PlayerServicePerformanceTests
{
    private readonly ITestOutputHelper _output;

    public PlayerServicePerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Benchmark_Flush_Synchronous_Wrapper_Optimized()
    {
        var repo = new BenchmarkMockPlayerRepository();
        var service = new PlayerService(repo, new ServerMetrics());
        var count = 50;

        // Create dirty sessions
        for (var i = 0; i < count; i++)
        {
            var s = new BenchmarkClientSession(i + 1);
            var c = new ServerCharacter { Id = i + 1, Name = $"Bencher_{i}" };
            c.AddGold(1); // Make dirty
            s.SetCharacter(c);
            s.SetQuestComponent(new PlayerQuestComponent(null));
            service.RegisterSession(s);
        }

        var sw = Stopwatch.StartNew();
        service.FlushAllDirty();
        sw.Stop();

        _output.WriteLine($"[SYNC-WRAPPER] Flushed {count} sessions in {sw.ElapsedMilliseconds}ms");

        Assert.Equal(count, repo.SaveCallCount);
        // It should also be fast now because it delegates to FlushAllDirtyAsync
        Assert.True(sw.ElapsedMilliseconds < count * repo.DelayMs * 0.5,
            "Synchronous wrapper is not using optimization!");
    }

    [Fact]
    public async Task Benchmark_Flush_Async_Optimized()
    {
        var repo = new BenchmarkMockPlayerRepository();
        var service = new PlayerService(repo, new ServerMetrics());
        var count = 50;

        // Create dirty sessions
        for (var i = 0; i < count; i++)
        {
            var s = new BenchmarkClientSession(i + 1);
            var c = new ServerCharacter { Id = i + 1, Name = $"Bencher_{i}" };
            c.AddGold(1); // Make dirty
            s.SetCharacter(c);
            s.SetQuestComponent(new PlayerQuestComponent(null));
            service.RegisterSession(s);
        }

        var sw = Stopwatch.StartNew();
        await service.FlushAllDirtyAsync();
        sw.Stop();

        _output.WriteLine(
            $"[OPTIMIZED] Flushed {count} sessions in {sw.ElapsedMilliseconds}ms (Expected << {count * repo.DelayMs}ms)");

        Assert.Equal(count, repo.SaveCallCount);
        // It should be much faster than sequential. Ideally closer to DelayMs (plus overhead) than count * DelayMs.
        // With 50 concurrent tasks and 10ms delay, total time should be around 20-50ms depending on thread pool.
        // Let's being conservative and say it must be at least 2x faster than sequential.
        long sequentialExpected = count * repo.DelayMs;
        Assert.True(sw.ElapsedMilliseconds < sequentialExpected * 0.5,
            "Optimization did not improve performance significantly!");
    }

    public class BenchmarkClientSession : ClientSession
    {
        public BenchmarkClientSession(int userId)
        {
            UserId = userId;
        }

        public void SetCharacter(ServerCharacter character) => Character = character;

        public void SetQuestComponent(PlayerQuestComponent component) => QuestComponent = component;
    }

    public class BenchmarkMockPlayerRepository : IPlayerRepository
    {
        private int _saveCallCount;
        public int SaveCallCount => _saveCallCount;

        public int DelayMs { get; set; } = 10;

        public PlayerSaveData? Load(int userId) => null;

        public async Task SaveAsync(int userId, PlayerSaveData data)
        {
            // Simulate non-blocking I/O
            await Task.Delay(DelayMs);
            Interlocked.Increment(ref _saveCallCount);
        }

        public Task<PlayerSaveData?> LoadAsync(int userId) => Task.FromResult<PlayerSaveData?>(null);

        public void Save(int userId, PlayerSaveData data)
        {
            // Simulate blocking I/O
            Thread.Sleep(DelayMs);
            Interlocked.Increment(ref _saveCallCount);
        }
    }
}