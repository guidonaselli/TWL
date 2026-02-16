using Microsoft.Extensions.Logging.Abstractions;
using TWL.Server.Services;
using TWL.Server.Simulation.Managers;

namespace TWL.Tests.Server.Services;

public class WorldSchedulerTests
{
    [Fact]
    public async Task Schedule_ExecutesActionAfterDelay()
    {
        using var scheduler = new WorldScheduler(NullLogger<WorldScheduler>.Instance, new ServerMetrics());
        scheduler.Start();

        var executed = false;
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

        // Give scheduler time to start
        await Task.Delay(50);

        var count = 0;

        scheduler.ScheduleRepeating(() => { Interlocked.Increment(ref count); }, TimeSpan.FromMilliseconds(50));

        // Should run at roughly 50, 100, 150, 200, 250ms (5 times)
        // Timing is loose in tests, so check at least 2. Increased wait time for CI reliability.
        await Task.Delay(300);

        Assert.True(count >= 2, $"Expected at least 2 executions, got {count}");
    }

    [Fact]
    public async Task CurrentTick_IncrementsOverTime()
    {
        using var scheduler = new WorldScheduler(NullLogger<WorldScheduler>.Instance, new ServerMetrics());
        scheduler.Start();

        var startTick = scheduler.CurrentTick;
        await Task.Delay(1000); // Expect ~20 ticks (50ms each), increased for reliability

        Assert.True(scheduler.CurrentTick > startTick,
            $"Expected tick to increment from {startTick}, got {scheduler.CurrentTick}");
    }

    [Fact]
    public async Task OnTick_InvokesHandler()
    {
        using var scheduler = new WorldScheduler(NullLogger<WorldScheduler>.Instance, new ServerMetrics());
        scheduler.Start();

        long capturedTick = -1;
        var tcs = new TaskCompletionSource<bool>();

        scheduler.OnTick += tick =>
        {
            capturedTick = tick;
            tcs.TrySetResult(true);
        };

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(500));
        Assert.Same(tcs.Task, completed);
        Assert.True(capturedTick > 0);
    }
}