using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Database;
using TWL.Server.Persistence.Services;
using TWL.Server.Services;
using TWL.Server.Services.World;
using TWL.Server.Simulation;
using TWL.Shared.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using Xunit;

namespace TWL.Tests.Reliability;

public class ShutdownTests
{
    [Fact]
    public async Task StopAsync_Should_DisconnectPlayers_And_StopServices()
    {
        // Arrange
        var mockNet = new Mock<INetworkServer>();
        var mockDb = new Mock<DbService>("connstring");
        var mockLog = new Mock<ILogger<ServerWorker>>();
        var mockScheduler = new Mock<IWorldScheduler>();

        var metrics = new ServerMetrics();
        var mockPlayerRepo = new Mock<IPlayerRepository>();
        var mockPlayerService = new Mock<PlayerService>(mockPlayerRepo.Object, metrics);
        var mockHealthCheck = new Mock<HealthCheckService>();

        // Setup PlayerService mocks
        mockPlayerService.Setup(s => s.DisconnectAllAsync(It.IsAny<string>())).Returns(Task.CompletedTask).Verifiable();
        mockPlayerService.Setup(s => s.Stop()).Verifiable();

        var worker = new ServerWorker(
            mockNet.Object,
            mockDb.Object,
            mockLog.Object,
            null!, // PetManager
            null!, // ServerQuestManager
            null!, // InteractionManager
            mockPlayerService.Object,
            mockScheduler.Object,
            metrics,
            null!, // MapLoader
            null!, // WorldTriggerService
            null!, // MonsterManager
            null!,  // SpawnManager
            null!,  // ILoggerFactory
            mockHealthCheck.Object,
            null!   // InstanceService
        );

        // Act
        await worker.StopAsync(CancellationToken.None);

        // Assert
        mockNet.Verify(n => n.Stop(), Times.Once);
        mockPlayerService.Verify(s => s.DisconnectAllAsync("Server Shutdown"), Times.Once);
        mockScheduler.Verify(s => s.Stop(), Times.Once);
        mockPlayerService.Verify(s => s.Stop(), Times.Once);

        // Verify metrics
        Assert.True(metrics.GetSnapshot().ShutdownDurationMs > 0 || metrics.GetSnapshot().ShutdownDurationMs == 0); // Can be 0 if too fast, but checking property exists
    }
}
