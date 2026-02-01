using Microsoft.Extensions.Logging.Abstractions;
using TWL.Server.Services;
using TWL.Server.Simulation.Managers;

namespace TWL.Tests.Reliability;

public class WorldLoopObservabilityTests
{
    [Fact]
    public async Task SlowTasks_AreRecordedInMetrics()
    {
        var metrics = new ServerMetrics();
        using var scheduler = new WorldScheduler(NullLogger<WorldScheduler>.Instance, metrics);
        scheduler.Start();

        // Schedule a task that sleeps for 20ms (above 10ms threshold)
        scheduler.Schedule(() => { Thread.Sleep(20); }, TimeSpan.Zero, "SlowTask");

        // Wait for it to execute
        await Task.Delay(100);

        scheduler.Stop();

        var snapshot = metrics.GetSnapshot();
        Assert.True(snapshot.WorldLoopSlowTasks > 0,
            $"Should record at least one slow task. Actual: {snapshot.WorldLoopSlowTasks}");
        // It might not trigger a SlowTick because 20ms < 50ms tick budget
    }

    [Fact]
    public async Task SlowTicks_AreRecordedInMetrics()
    {
        var metrics = new ServerMetrics();
        using var scheduler = new WorldScheduler(NullLogger<WorldScheduler>.Instance, metrics);
        scheduler.Start();

        // Schedule a task that sleeps for 70ms (above 50ms tick threshold)
        scheduler.Schedule(() => { Thread.Sleep(70); }, TimeSpan.Zero, "VerySlowTask");

        // Wait for it to execute
        await Task.Delay(200);

        scheduler.Stop();

        var snapshot = metrics.GetSnapshot();
        Assert.True(snapshot.WorldLoopSlowTicks > 0,
            $"Should record at least one slow tick. Actual: {snapshot.WorldLoopSlowTicks}");
        Assert.True(snapshot.WorldLoopSlowTasks > 0,
            $"Should also record slow task. Actual: {snapshot.WorldLoopSlowTasks}");
    }
}