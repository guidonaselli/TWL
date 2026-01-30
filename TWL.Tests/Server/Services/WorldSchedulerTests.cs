using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using TWL.Server.Services;
using TWL.Server.Simulation.Managers;
using Xunit;

namespace TWL.Tests.Server.Services;

public class WorldSchedulerTests
{
    [Fact]
    public async Task Schedule_ExecutesActionAfterDelay()
    {
        using var scheduler = new WorldScheduler(NullLogger<WorldScheduler>.Instance, new ServerMetrics());
        scheduler.Start();

        bool executed = false;
        var tcs = new TaskCompletionSource<bool>();

        scheduler.Schedule(() =>
        {
            executed = true;
            tcs.SetResult(true);
        }, TimeSpan.FromMilliseconds(50));

        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(2000));
        Assert.Same(tcs.Task, completedTask);
        Assert.True(executed);
    }

    [Fact]
    public async Task ScheduleRepeating_ExecutesRepeatedly()
    {
        using var scheduler = new WorldScheduler(NullLogger<WorldScheduler>.Instance, new ServerMetrics());
        scheduler.Start();

        int count = 0;

        scheduler.ScheduleRepeating(() =>
        {
            Interlocked.Increment(ref count);
        }, TimeSpan.FromMilliseconds(50));

        await Task.Delay(250); // Increased from 170 to account for loop alignment and overhead

        Assert.True(count >= 2, $"Expected at least 2 executions, got {count}");
    }

    [Fact]
    public async Task Slippage_ShouldBeRecorded_WhenLoopBlocked()
    {
        var metrics = new ServerMetrics();
        using var scheduler = new WorldScheduler(NullLogger<WorldScheduler>.Instance, metrics);
        scheduler.Start();

        // Block the loop for > 50ms
        var evt = new ManualResetEventSlim(false);
        scheduler.Schedule(() =>
        {
            Thread.Sleep(150); // Block intentionally
            evt.Set();
        }, TimeSpan.FromMilliseconds(10));

        Assert.True(evt.Wait(500));

        // Wait for next tick to record slippage
        await Task.Delay(200);

        var snapshot = metrics.GetSnapshot();
        Assert.True(snapshot.WorldLoopSlippageMs > 0, $"Slippage should be > 0, got {snapshot.WorldLoopSlippageMs}");
    }
}
