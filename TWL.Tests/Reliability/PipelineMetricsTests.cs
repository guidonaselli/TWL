using Microsoft.Extensions.Logging.Abstractions;
using System.Threading.Tasks;
using TWL.Shared.Constants;
using Microsoft.Extensions.Options;
using TWL.Shared.Domain.Battle;
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
using TWL.Server.Security;
using TWL.Shared.Net.Network;
using TWL.Shared.Services;
using TWL.Shared.Constants;

namespace TWL.Tests.Reliability;

public class PipelineMetricsTests
{
    [Fact]
    public async Task Pipeline_ShouldRecordMetrics_WhenMessageReceived()
    {
        // Arrange
        var metrics = new ServerMetrics();

        // Mock dependencies
        var db = new DbService("Host=invalid;Database=dummy", new Mock<IServiceProvider>().Object);

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
        var monsterManager = new MonsterManager();

        var petService = new PetService(playerService, mockPet.Object, monsterManager, combatManager, mockRandom.Object, new Moq.Mock<Microsoft.Extensions.Logging.ILogger<PetService>>().Object);
        var mockWorldTrigger = new Mock<IWorldTriggerService>();
        var spawnManager = new SpawnManager(monsterManager, combatManager, mockRandom.Object, playerService, new Mock<TWL.Server.Simulation.Managers.IPartyService>().Object);
        var mockMediator = new Mock<IMediator>();

        var guildRepository = new Mock<TWL.Shared.Domain.Guilds.IGuildRepository>();
        var guildManager = new GuildManager(guildRepository.Object);
        var guildChatService = new GuildChatService(guildManager, playerService);
        var guildRosterService = new GuildRosterService(guildManager, playerService);
        var guildAuditLogService = new GuildAuditLogService();
        var guildStorageService = new GuildStorageService(guildManager, guildRepository.Object, guildAuditLogService, NullLogger<GuildStorageService>.Instance);

        var mockMarket = new Mock<IMarketService>();
        var mockCompound = new Mock<ICompoundService>();

        // Dynamic port assignment: use port 0 to let OS assign a free port
        var server = new NetworkServer(0, db, mockPet.Object, mockQuest.Object, combatManager, mockInteract.Object,
            playerService, mockEconomy.Object, metrics, petService, new Mock<IMediator>().Object, mockWorldTrigger.Object, spawnManager,
            new ReplayGuard(new ReplayGuardOptions()), new MovementValidator(new MovementValidationOptions()), new PartyManager(), new TWL.Server.Simulation.Managers.PartyChatService(new PartyManager(), playerService, new Microsoft.Extensions.Logging.Abstractions.NullLogger<TWL.Server.Simulation.Managers.PartyChatService>()), guildManager, guildChatService, guildRosterService, guildStorageService, new RebirthManager(new Microsoft.Extensions.Logging.Abstractions.NullLogger<RebirthManager>()), mockMarket.Object, new MarketQueryService(mockMarket.Object), mockCompound.Object, new TradeSessionManager(playerService, new TradeManager()), Options.Create(new RateLimiterOptions()));

        server.Start();
        var port = server.Port;

        try
        {
            using var client = new TcpClient();
            // Give server a moment to bind
            await Task.Delay(100);

            await client.ConnectAsync("127.0.0.1", port);

            var msg = new NetMessage { Op = Opcode.MoveRequest, JsonPayload = "{\"dx\":1,\"dy\":0}", SchemaVersion = ProtocolConstants.CurrentSchemaVersion };
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
            Assert.True(snapshot.NetBytesReceived > 0,
                $"Expected NetBytesReceived > 0, got {snapshot.NetBytesReceived}");
            Assert.True(snapshot.NetMessagesProcessed > 0,
                $"Expected NetMessagesProcessed > 0, got {snapshot.NetMessagesProcessed}");
            Assert.True(snapshot.TotalMessageProcessingTimeTicks > 0, "Should have recorded processing time");
        }
        finally
        {
            server.Stop();
        }
    }
}
