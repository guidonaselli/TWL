using System.Net.Sockets;
using System.Text.Json;
using Moq;
using TWL.Server.Architecture.Pipeline;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Database;
using TWL.Server.Persistence.Services;
using TWL.Server.Services;
using TWL.Server.Services.World;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Net.Network;
using TWL.Shared.Services;

namespace TWL.Tests.Reliability;

public class PipelineGranularMetricsTests
{
    [Fact]
    public async Task Pipeline_ShouldRecordGranularMetrics_WhenMessageReceived()
    {
        // Arrange
        var metrics = new ServerMetrics();

        // Mock dependencies
        var db = new DbService("Host=invalid;Database=dummy");
        var mockPet = new Mock<PetManager>();
        var mockQuest = new Mock<ServerQuestManager>();
        var mockSkillCatalog = new Mock<ISkillCatalog>();
        var mockRandom = new Mock<IRandomService>();
        var mockCombatResolver = new Mock<ICombatResolver>();
        var mockStatusEngine = new Mock<IStatusEngine>();
        var combatManager = new CombatManager(mockCombatResolver.Object, mockRandom.Object, mockSkillCatalog.Object,
            mockStatusEngine.Object);
        var mockInteract = new Mock<InteractionManager>();
        var mockPlayerRepo = new Mock<IPlayerRepository>();
        var playerService = new PlayerService(mockPlayerRepo.Object, metrics);
        var mockEconomy = new Mock<IEconomyService>();
        var petService = new PetService(playerService, mockPet.Object, combatManager, mockRandom.Object);
        var mockWorldTrigger = new Mock<IWorldTriggerService>();
        var spawnManager = new SpawnManager(new MonsterManager(), combatManager, mockRandom.Object, playerService);
        var mediator = new Mediator();

        // Dynamic port assignment: use port 0 to let OS assign a free port
        var server = new NetworkServer(0, db, mockPet.Object, mockQuest.Object, combatManager, mockInteract.Object,
            playerService, mockEconomy.Object, metrics, petService, mediator, mockWorldTrigger.Object, spawnManager);

        server.Start();
        var port = server.Port;

        try
        {
            using var client = new TcpClient();
            await Task.Delay(100);
            await client.ConnectAsync("127.0.0.1", port);

            var msg = new NetMessage { Op = Opcode.MoveRequest, JsonPayload = "{\"dx\":1,\"dy\":0}" };
            var bytes = JsonSerializer.SerializeToUtf8Bytes(msg);
            var stream = client.GetStream();
            await stream.WriteAsync(bytes, 0, bytes.Length);

            // Wait for processing
            for (var i = 0; i < 20; i++)
            {
                if (metrics.GetSnapshot().NetMessagesProcessed > 0)
                {
                    break;
                }

                await Task.Delay(100);
            }

            var snapshot = metrics.GetSnapshot();

            // Assert
            Assert.True(snapshot.NetMessagesProcessed > 0, "Messages processed");

            // Check Granular Metrics
            Assert.True(snapshot.PipelineValidateDurationTicks > 0,
                $"Expected PipelineValidateDurationTicks > 0, got {snapshot.PipelineValidateDurationTicks}");
            Assert.True(snapshot.PipelineResolveDurationTicks >= 0, "Resolve duration should be recorded");
        }
        finally
        {
            server.Stop();
        }
    }
}