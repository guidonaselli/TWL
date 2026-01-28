using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using TWL.Server.Services;
using TWL.Server.Simulation.Managers;
using Xunit;

namespace TWL.Tests.Reliability;

public class WorldLoopObservabilityTests
{
    [Fact]
    public async Task WorldScheduler_ShouldRecordMetrics_WhenRunning()
    {
        // Arrange
        var metrics = new ServerMetrics();
        var mockLogger = new Mock<ILogger<WorldScheduler>>();

        using var scheduler = new WorldScheduler(mockLogger.Object, metrics);

        // Act
        scheduler.Start();

        // Schedule some tasks
        int taskRunCount = 0;
        scheduler.Schedule(() => { taskRunCount++; }, TimeSpan.FromMilliseconds(10));
        scheduler.Schedule(() => { taskRunCount++; }, TimeSpan.FromMilliseconds(20));

        // Wait for loop to run a few times
        await Task.Delay(200);

        scheduler.Stop();

        // Assert
        var snapshot = metrics.GetSnapshot();

        Assert.True(snapshot.WorldLoopTicks > 0, $"Expected WorldLoopTicks > 0, got {snapshot.WorldLoopTicks}");
        Assert.True(snapshot.WorldLoopTotalDurationMs >= 0, "Expected WorldLoopTotalDurationMs >= 0");
        Assert.Equal(2, taskRunCount);
    }

    [Fact]
    public async Task WorldScheduler_ShouldReportQueueDepth()
    {
         // Arrange
        var metrics = new ServerMetrics();
        var mockLogger = new Mock<ILogger<WorldScheduler>>();
        using var scheduler = new WorldScheduler(mockLogger.Object, metrics);

        // Act
        // We want to verify that QueueDepth is reported.
        // We schedule something far in the future so it stays in queue.

        scheduler.Schedule(() => { }, TimeSpan.FromMinutes(1));
        scheduler.Start();

        await Task.Delay(100);

        var snapshot = metrics.GetSnapshot();

        // Should be at least 1 because we have a task scheduled for future
        Assert.True(snapshot.WorldSchedulerQueueDepth >= 1, $"Expected QueueDepth >= 1, got {snapshot.WorldSchedulerQueueDepth}");

        scheduler.Stop();
    }
}
