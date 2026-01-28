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

        await Task.Delay(170); // Should run at roughly 50, 100, 150. (3 times)
        // Timing is loose in tests, so check at least 2.

        Assert.True(count >= 2, $"Expected at least 2 executions, got {count}");
    }
}
