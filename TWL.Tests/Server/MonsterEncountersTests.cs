using TWL.Shared.Domain.Battle;
using Moq;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Persistence.Services;
using TWL.Server.Persistence;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.World;
using TWL.Shared.Services;
using System.Text.Json;
using TWL.Shared.Net.Network;
using Xunit;
using TWL.Server.Features.Combat;

namespace TWL.Tests.Server;

public class MonsterEncountersTests
{
    private readonly Mock<MonsterManager> _mockMonsterManager;
    private readonly Mock<CombatManager> _mockCombatManager;
    private readonly Mock<IRandomService> _mockRandom;
    private readonly Mock<PlayerService> _mockPlayerService;
    private readonly Mock<ICombatResolver> _mockCombatResolver;
    private readonly Mock<ISkillCatalog> _mockSkillCatalog;
    private readonly Mock<IStatusEngine> _mockStatusEngine;

    public MonsterEncountersTests()
    {
        _mockMonsterManager = new Mock<MonsterManager>();
        _mockRandom = new Mock<IRandomService>();
        _mockPlayerService = new Mock<PlayerService>(new Mock<IPlayerRepository>().Object, new ServerMetrics());
        _mockCombatResolver = new Mock<ICombatResolver>();
        _mockSkillCatalog = new Mock<ISkillCatalog>();
        _mockStatusEngine = new Mock<IStatusEngine>();

        _mockCombatManager = new Mock<CombatManager>(_mockCombatResolver.Object, _mockRandom.Object, _mockSkillCatalog.Object, _mockStatusEngine.Object);
    }

    [Fact]
    public void ZoneSpawnConfig_ShouldDeserializeCorrectly()
    {
        var json = @"{
            ""MapId"": 1001,
            ""SpawnRegions"": [
                {
                    ""X"": 0,
                    ""Y"": 0,
                    ""Width"": 20,
                    ""Height"": 20,
                    ""AllowedMonsterIds"": [2001, 2002]
                }
            ],
            ""RandomEncounterEnabled"": true,
            ""StepChance"": 0.05
        }";

        var config = JsonSerializer.Deserialize<ZoneSpawnConfig>(json);

        Assert.NotNull(config);
        Assert.Equal(1001, config.MapId);
        Assert.True(config.RandomEncounterEnabled);
        Assert.Equal(0.05f, config.StepChance);
        Assert.Single(config.SpawnRegions);
        Assert.Equal(2, config.SpawnRegions[0].AllowedMonsterIds.Count);
    }

    [Fact]
    public void StartEncounter_ShouldFail_WhenPlayerHasElementNone()
    {
        // Arrange
        var spawnManager = new SpawnManager(_mockMonsterManager.Object, _mockCombatManager.Object, _mockRandom.Object, _mockPlayerService.Object);
        var session = new Mock<ClientSession>(); // Need a way to mock ClientSession or use a testable subclass if available.
        // ClientSession is hard to mock due to TcpClient dependency.
        // Let's create a partial mock or a fake character directly passed to a testable method if possible.
        // Or better, SpawnManager.StartEncounter takes a ClientSession.

        // Since ClientSession is tightly coupled with TcpClient, we might need to modify SpawnManager to accept IClientSession or just the Character.
        // However, SpawnManager.StartEncounter signature is: public virtual int StartEncounter(ClientSession session, ...)

        // For unit testing purposes without refactoring the whole networking stack, we can test the validation logic if we can create a dummy session.
        // But ClientSession constructor requires TcpClient.

        // Let's rely on the fact that SpawnManager.StartEncounter validates participants.
        // We can test a lower level method if we refactor, or we can use a "TestClientSession" if one exists.
        // The codebase showed "protected ClientSession() { }" which is for testing.

        var testSession = new TestClientSession();
        var character = new ServerCharacter { Id = 1, Name = "TestPlayer", CharacterElement = Element.None };
        testSession.SetCharacter(character);

        var enemies = new List<ServerCharacter>
        {
            new ServerCharacter { Id = -1, Name = "Mob", MonsterId = 2001, CharacterElement = Element.Earth }
        };

        // Act
        var encounterId = spawnManager.StartEncounter(testSession, enemies, EncounterSource.Scripted);

        // Assert
        Assert.Equal(0, encounterId); // Should return 0 (fail)
    }

    [Fact]
    public void StartEncounter_ShouldSucceed_WhenParticipantsAreValid()
    {
        // Arrange
        var spawnManager = new SpawnManager(_mockMonsterManager.Object, _mockCombatManager.Object, _mockRandom.Object, _mockPlayerService.Object);
        var testSession = new TestClientSession();
        var character = new ServerCharacter { Id = 1, Name = "TestPlayer", CharacterElement = Element.Fire };
        testSession.SetCharacter(character);

        var enemies = new List<ServerCharacter>
        {
            new ServerCharacter { Id = -1, Name = "Mob", MonsterId = 2001, CharacterElement = Element.Earth }
        };

        _mockCombatManager.Setup(c => c.StartEncounter(It.IsAny<int>(), It.IsAny<IEnumerable<ServerCombatant>>(), It.IsAny<int>()));

        // Act
        var encounterId = spawnManager.StartEncounter(testSession, enemies, EncounterSource.Scripted);

        // Assert
        Assert.NotEqual(0, encounterId);
        _mockCombatManager.Verify(c => c.StartEncounter(It.IsAny<int>(), It.IsAny<IEnumerable<ServerCombatant>>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public void StartEncounter_ShouldBeIdempotent()
    {
        // Arrange
        var spawnManager = new SpawnManager(_mockMonsterManager.Object, _mockCombatManager.Object, _mockRandom.Object, _mockPlayerService.Object);
        var testSession = new TestClientSession();
        var character = new ServerCharacter { Id = 1, Name = "TestPlayer", CharacterElement = Element.Fire };
        testSession.SetCharacter(character);

        var enemies = new List<ServerCharacter>
        {
            new ServerCharacter { Id = -1, Name = "Mob", MonsterId = 2001, CharacterElement = Element.Earth }
        };

        // Mock CombatManager to return an existing combatant
        var existingCombatant = new ServerCharacter { Id = 1, EncounterId = 999 };
        _mockCombatManager.Setup(c => c.GetCombatant(1)).Returns(existingCombatant);
        _mockCombatManager.Setup(c => c.GetAllCombatants()).Returns(new List<ServerCombatant> { existingCombatant });

        // Act
        var encounterId = spawnManager.StartEncounter(testSession, enemies, EncounterSource.Scripted);

        // Assert
        Assert.Equal(999, encounterId); // Should return existing ID
        _mockCombatManager.Verify(c => c.StartEncounter(It.IsAny<int>(), It.IsAny<IEnumerable<ServerCombatant>>(), It.IsAny<int>()), Times.Never);
    }

    // Helper class to bypass protected constructor of ClientSession
    private class TestClientSession : ClientSession
    {
        public TestClientSession() : base() { }

        public void SetCharacter(ServerCharacter character)
        {
            Character = character;
        }

        public override Task SendAsync(NetMessage msg)
        {
            // Mock send, do nothing
            return Task.CompletedTask;
        }
    }
}
