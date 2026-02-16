using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TWL.Server.Services;
using TWL.Server.Simulation.Managers;
using Xunit;

namespace TWL.Tests.Services;

public class WorldSchedulerTests : IDisposable
{
    private readonly ServerMetrics _metrics;
    private readonly WorldScheduler _scheduler;
    private readonly List<int> _executionOrder = new();
    private readonly object _lock = new();

    public WorldSchedulerTests()
    {
        _metrics = new ServerMetrics();
        _scheduler = new WorldScheduler(NullLogger<WorldScheduler>.Instance, _metrics);
    }

    public void Dispose()
    {
        _scheduler.Stop();
        _scheduler.Dispose();
    }

    [Fact]
    public void Schedule_OneOffTask_ExecutesOnce()
    {
        // Arrange
        int executionCount = 0;
        using var mre = new ManualResetEventSlim(false);

        _scheduler.Start();

        // Act
        // Schedule task to run immediately (1 tick delay to be safe)
        _scheduler.Schedule(() =>
        {
            Interlocked.Increment(ref executionCount);
            mre.Set();
        }, 1, "TestTask");

        // Wait for execution (timeout 1s)
        Assert.True(mre.Wait(1000), "Task did not execute in time");

        // Wait a bit more to ensure it doesn't run again
        Thread.Sleep(100);

        // Assert
        Assert.Equal(1, executionCount);
    }

    [Fact]
    public void ScheduleRepeating_Task_ExecutesMultipleTimes()
    {
        // Arrange
        int executionCount = 0;

        _scheduler.Start();

        // Act
        // Schedule task to run every tick (interval 1 tick)
        _scheduler.ScheduleRepeating(() =>
        {
            Interlocked.Increment(ref executionCount);
        }, 1, "RepeatingTask");

        // Wait for ~5 ticks (5 * 50ms = 250ms), increased for reliability
        Thread.Sleep(1000);

        // Assert
        Assert.True(executionCount >= 4, $"Expected at least 4 executions, got {executionCount}");
    }

    [Fact]
    public void Tasks_Run_In_FIFO_Order()
    {
        // Arrange
        using var mre = new ManualResetEventSlim(false);
        int tasksRun = 0;

        _scheduler.Start();

        // Act
        // Schedule 3 tasks for the same tick
        _scheduler.Schedule(() => { lock(_lock) { _executionOrder.Add(1); tasksRun++; } }, 2, "Task1");
        _scheduler.Schedule(() => { lock(_lock) { _executionOrder.Add(2); tasksRun++; } }, 2, "Task2");
        _scheduler.Schedule(() => { lock(_lock) { _executionOrder.Add(3); tasksRun++; if (tasksRun == 3) mre.Set(); } }, 2, "Task3");

        Assert.True(mre.Wait(1000), "Tasks did not complete in time");

        // Assert
        lock(_lock)
        {
            Assert.Equal(new[] { 1, 2, 3 }, _executionOrder);
        }
    }
}
