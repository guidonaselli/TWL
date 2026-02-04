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

        // Schedule a task that sleeps for 40ms (above presumed 10ms threshold, safer for OS scheduling)
        scheduler.Schedule(() => { Thread.Sleep(40); }, TimeSpan.Zero, "SlowTask");

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

    [Fact]
    public async Task Drift_IsRecordedInMetrics()
    {
        var metrics = new ServerMetrics();
        using var scheduler = new WorldScheduler(NullLogger<WorldScheduler>.Instance, metrics);
        scheduler.Start();

        // Induce lag by blocking the thread pool or simply waiting
        // We can't easily block the internal loop of scheduler from outside without a task.
        // But if we schedule a task that sleeps, it runs ON the loop.
        // So:
        // 1. Schedule a task that sleeps 150ms.
        // 2. This runs in ProcessTick.
        // 3. Next Loop iteration sees frameTime > 150ms.
        // 4. accumulator increases by 150.
        // 5. It records drift (150) and processes 3 ticks.

        scheduler.Schedule(() => { Thread.Sleep(150); }, TimeSpan.Zero, "LagSpike");

        // Wait for it to happen
        await Task.Delay(300);

        scheduler.Stop();

        var snapshot = metrics.GetSnapshot();
        Assert.True(snapshot.WorldLoopDriftTotalMs > 0,
            $"Should record drift. Actual: {snapshot.WorldLoopDriftTotalMs}");
        Assert.True(snapshot.AverageWorldLoopDriftMs > 0,
            $"Should have positive average drift. Actual: {snapshot.AverageWorldLoopDriftMs}");
    }
}