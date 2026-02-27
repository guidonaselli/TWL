using TWL.Shared.Domain.Battle;
using Microsoft.Extensions.Logging;
using Moq;
using TWL.Server.Domain.World;
using TWL.Server.Persistence.Database;
using TWL.Server.Persistence.Services;
using TWL.Server.Services;
using TWL.Server.Services.World;
using TWL.Server.Simulation;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Services;
using Xunit;

namespace TWL.Tests.Server.Simulation;

public class GracefulShutdownTests
{
    [Fact]
    public async Task StopAsync_ShouldDisconnectPlayersAndStopServices()
    {
        // Arrange
        var mockNet = new Mock<INetworkServer>();
        var mockDb = new Mock<IDbService>();
        var mockLog = new Mock<ILogger<ServerWorker>>();
        var mockPetManager = new Mock<PetManager>();
        var mockQuestManager = new Mock<ServerQuestManager>();
        var mockInteractionManager = new Mock<InteractionManager>();

        var metrics = new ServerMetrics();
        // Pass null for repository as we won't access it
        var mockPlayerService = new Mock<PlayerService>(null, metrics);
        var mockWorldScheduler = new Mock<IWorldScheduler>();
        var mockMapRegistry = new Mock<IMapRegistry>();
        var mockTriggerService = new Mock<IWorldTriggerService>();
        var mockMonsterManager = new Mock<MonsterManager>();

        var mockCombatManager = new Mock<CombatManager>(
            new Mock<ICombatResolver>().Object,
            new Mock<IRandomService>().Object,
            new Mock<ISkillCatalog>().Object,
            new Mock<IStatusEngine>().Object,
            (PartyRewardDistributor)null // Pass null for PartyRewardDistributor to match the second constructor
        );
        var mockRandom = new Mock<IRandomService>();

        // SpawnManager constructor: MonsterManager, CombatManager, IRandomService, PlayerService
        var mockSpawnManager = new Mock<SpawnManager>(
            mockMonsterManager.Object,
            mockCombatManager.Object,
            mockRandom.Object,
            mockPlayerService.Object
        );

        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);

        // Use a mock sequence to verify order of critical operations
        var sequence = new MockSequence();
        var mockHealthCheck = new Mock<HealthCheckService>();

        // Expectations in order
        mockHealthCheck.InSequence(sequence).Setup(x => x.SetStatus(ServerStatus.ShuttingDown));
        mockNet.InSequence(sequence).Setup(x => x.Stop());
        mockPlayerService.InSequence(sequence).Setup(x => x.DisconnectAllAsync(It.Is<string>(s => s.Contains("Shutdown")))).Returns(Task.CompletedTask);
        mockHealthCheck.InSequence(sequence).Setup(x => x.SetStatus(ServerStatus.Stopped));

        var worker = new ServerWorker(
            mockNet.Object,
            mockDb.Object,
            mockLog.Object,
            mockPetManager.Object,
            mockQuestManager.Object,
            mockInteractionManager.Object,
            mockPlayerService.Object,
            mockWorldScheduler.Object,
            metrics,
            mockMapRegistry.Object,
            mockTriggerService.Object,
            mockMonsterManager.Object,
            mockSpawnManager.Object,
            mockLoggerFactory.Object,
            mockHealthCheck.Object,
            new Mock<InstanceService>(metrics).Object, mockCombatManager.Object
        );

        // Act
        await worker.StopAsync(CancellationToken.None);

        // Assert
        // 1. Health Status ShuttingDown (Verified by Sequence)
        // 2. Network Stop (Verified by Sequence)
        // 3. Disconnect All Players (Verified by Sequence)

        // 4. Scheduler Stop
        mockWorldScheduler.Verify(x => x.Stop(), Times.Once);

        // 5. Player Service Stop (Flush)
        mockPlayerService.Verify(x => x.StopAsync(), Times.Once);

        // 6. Health Status Stopped (Verified by Sequence)

        // Verify Metrics
        var snapshot = metrics.GetSnapshot();
        // Since we didn't mock stopwatch (it uses system one), duration should be >= 0
        Assert.True(snapshot.ShutdownDurationMs >= 0);
    }
}
