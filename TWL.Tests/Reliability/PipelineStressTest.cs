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
using TWL.Server.Architecture.Pipeline;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Net.Network;
using TWL.Shared.Services;

namespace TWL.Tests.Reliability;

public class PipelineStressTest
{
    [Fact]
    public async Task Pipeline_ShouldPopulateHistograms_UnderLoad()
    {
        // Arrange
        var metrics = new ServerMetrics();

        // Dependencies (Mocks)
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
        var mockMediator = new Mock<IMediator>();

        // Dynamic port assignment: use port 0 to let OS assign a free port
        var server = new NetworkServer(0, db, mockPet.Object, mockQuest.Object, combatManager, mockInteract.Object,
            playerService, mockEconomy.Object, metrics, petService, mockMediator.Object, mockWorldTrigger.Object, spawnManager);

        server.Start();
        var port = server.Port;

        try
        {
            using var client = new TcpClient();
            await Task.Delay(100);
            await client.ConnectAsync("127.0.0.1", port);

            var stream = client.GetStream();
            var msgCount = 50;
            var msg = new NetMessage { Op = Opcode.MoveRequest, JsonPayload = "{\"dx\":1,\"dy\":0}" };
            var bytes = JsonSerializer.SerializeToUtf8Bytes(msg);

            // Act
            for (var i = 0; i < msgCount; i++)
            {
                await stream.WriteAsync(bytes, 0, bytes.Length);
                // Delay to avoid TCP coalescing (since we lack framing) and RateLimit
                await Task.Delay(50);
            }

            // Allow catch up
            await Task.Delay(1000);

            var snapshot = metrics.GetSnapshot();

            // Assert
            Assert.True(snapshot.NetMessagesProcessed >= msgCount,
                $"Expected >= {msgCount} processed, got {snapshot.NetMessagesProcessed}");
            Assert.True(snapshot.PipelineValidateDurationTicks > 0);
            Assert.True(snapshot.PipelineResolveDurationTicks > 0);

            // Check Histograms
            var valHist = snapshot.ValidateHistogram;
            var resHist = snapshot.ResolveHistogram;

            var totalVal = valHist.Bucket1ms + valHist.Bucket5ms + valHist.Bucket10ms + valHist.Bucket50ms +
                           valHist.Bucket100ms + valHist.BucketOver100ms;
            var totalRes = resHist.Bucket1ms + resHist.Bucket5ms + resHist.Bucket10ms + resHist.Bucket50ms +
                           resHist.Bucket100ms + resHist.BucketOver100ms;

            Assert.True(totalVal > 0, "Validate Histogram should have entries");
            Assert.True(totalRes > 0, "Resolve Histogram should have entries");

            Console.WriteLine("Validate Histogram: " + valHist);
            Console.WriteLine("Resolve Histogram: " + resHist);
        }
        finally
        {
            server.Stop();
        }
    }
}