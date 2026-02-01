using System.Diagnostics;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;

namespace TWL.Tests.Persistence;

public class TestClientSession : ClientSession
{
    public TestClientSession(int userId)
    {
        UserId = userId;
    }

    public void SetCharacter(ServerCharacter character) => Character = character;

    public void SetQuestComponent(PlayerQuestComponent component) => QuestComponent = component;
}

public class MockPlayerRepository : IPlayerRepository
{
    public int SaveCallCount { get; private set; }
    public bool ShouldThrow { get; set; }

    public async Task SaveAsync(int userId, PlayerSaveData data)
    {
        if (ShouldThrow)
        {
            throw new Exception("Simulated disk failure");
        }

        // Mimic I/O delay
        await Task.Delay(5);
        SaveCallCount++;
    }

    public PlayerSaveData? Load(int userId) => null;

    public Task<PlayerSaveData?> LoadAsync(int userId) => Task.FromResult<PlayerSaveData?>(null);
}

public class PlayerServiceReliabilityTests
{
    [Fact]
    public async Task Flush_SavesOnlyDirtySessions()
    {
        var repo = new MockPlayerRepository();
        var service = new PlayerService(repo, new ServerMetrics());
        var qm = new ServerQuestManager();

        // Dirty session
        var s1 = new TestClientSession(1);
        var c1 = new ServerCharacter { Id = 1, Name = "Dirty" };
        c1.AddGold(10); // Makes it dirty
        s1.SetCharacter(c1);
        s1.SetQuestComponent(new PlayerQuestComponent(qm));

        service.RegisterSession(s1);

        // Clean session
        var s2 = new TestClientSession(2);
        var c2 = new ServerCharacter { Id = 2, Name = "Clean" };
        // Not dirty
        s2.SetCharacter(c2);
        s2.SetQuestComponent(new PlayerQuestComponent(qm));
        service.RegisterSession(s2);

        await service.FlushAllDirtyAsync();

        Assert.Equal(1, repo.SaveCallCount);
        Assert.False(c1.IsDirty); // Should be cleared
    }

    [Fact]
    public async Task Flush_RetainsDirtyFlag_OnFailure()
    {
        var repo = new MockPlayerRepository { ShouldThrow = true };
        var service = new PlayerService(repo, new ServerMetrics());
        var qm = new ServerQuestManager();

        var s1 = new TestClientSession(1);
        var c1 = new ServerCharacter { Id = 1, Name = "Dirty" };
        c1.AddGold(10);
        s1.SetCharacter(c1);
        s1.SetQuestComponent(new PlayerQuestComponent(qm));
        service.RegisterSession(s1);

        await service.FlushAllDirtyAsync();

        Assert.Equal(0, repo.SaveCallCount); // Save threw exception
        Assert.True(c1.IsDirty); // Should still be dirty
        Assert.Equal(1, service.Metrics.TotalSaveErrors);
    }

    [Fact]
    public async Task Benchmark_Flush_Performance()
    {
        var repo = new MockPlayerRepository();
        var service = new PlayerService(repo, new ServerMetrics());
        var count = 100;
        var qm = new ServerQuestManager();

        for (var i = 0; i < count; i++)
        {
            var s = new TestClientSession(i + 100);
            var c = new ServerCharacter { Id = i + 100, Name = $"Bencher_{i}" };
            c.AddGold(1);
            s.SetCharacter(c);
            s.SetQuestComponent(new PlayerQuestComponent(qm));
            service.RegisterSession(s);
        }

        var sw = Stopwatch.StartNew();
        await service.FlushAllDirtyAsync();
        sw.Stop();

        Assert.Equal(count, repo.SaveCallCount);
        Assert.Equal(count, service.Metrics.SessionsSavedInLastFlush);

        // Assert that metrics captured duration correctly (within margin)
        // With optimization, this should be much faster than 450ms (sequential 5ms * 100).
        // We just ensure it's recorded correctly.
        Assert.True(service.Metrics.LastFlushDurationMs >= 0);
        Assert.InRange(service.Metrics.LastFlushDurationMs, 0,
            sw.ElapsedMilliseconds + 20); // Metrics duration should be close to stopwatch

        Console.WriteLine(
            $"Benchmark: Flushed {count} sessions in {sw.ElapsedMilliseconds}ms (Metrics: {service.Metrics.LastFlushDurationMs}ms)");
    }
}