using Moq;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Services;
using TWL.Server.Features.Combat;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Services;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Skills;
using System.Collections.Generic;
using System.Threading.Tasks;
using TWL.Shared.Net.Network;

namespace TWL.Tests.Server;

public class SpawnManagerValidationTests
{
    private readonly Mock<MonsterManager> _mockMonsterManager;
    private readonly Mock<CombatManager> _mockCombatManager;
    private readonly Mock<IRandomService> _mockRandom;
    private readonly Mock<PlayerService> _mockPlayerService;
    private readonly SpawnManager _spawnManager;

    public SpawnManagerTests()
    {
        _mockMonsterManager = new Mock<MonsterManager>();

        var mockCombatResolver = new Mock<ICombatResolver>();
        var mockSkillCatalog = new Mock<ISkillCatalog>();
        var mockStatusEngine = new Mock<IStatusEngine>();

        // Mock CombatManager virtual methods
        _mockCombatManager = new Mock<CombatManager>(mockCombatResolver.Object, new Mock<IRandomService>().Object, mockSkillCatalog.Object, mockStatusEngine.Object);

        _mockRandom = new Mock<IRandomService>();

        var mockRepo = new Mock<IPlayerRepository>();
        var metrics = new ServerMetrics();
        _mockPlayerService = new Mock<PlayerService>(mockRepo.Object, metrics);

        _spawnManager = new SpawnManager(_mockMonsterManager.Object, _mockCombatManager.Object, _mockRandom.Object, _mockPlayerService.Object);
    }

    [Fact]
    public void StartEncounter_Rejects_ElementNone_Player()
    {
        // Arrange
        var player = new ServerCharacter { Id = 1, Name = "TestPlayer", CharacterElement = Element.None };
        var session = new TestClientSession();
        session.SetCharacter(player);

        var enemy = new ServerCharacter { MonsterId = 100, CharacterElement = Element.Fire, Name = "FireMob" };
        var enemies = new List<ServerCharacter> { enemy };

        // Act
        var result = _spawnManager.StartEncounter(session, enemies, EncounterSource.Scripted);

        // Assert
        Assert.Equal(0, result);
        _mockCombatManager.Verify(m => m.StartEncounter(It.IsAny<int>(), It.IsAny<IEnumerable<ServerCombatant>>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void StartEncounter_Rejects_ElementNone_Mob_Without_QuestOnly_Tag()
    {
        // Arrange
        var player = new ServerCharacter { Id = 1, Name = "TestPlayer", CharacterElement = Element.Earth };
        var session = new TestClientSession();
        session.SetCharacter(player);

        var enemy = new ServerCharacter { MonsterId = 100, CharacterElement = Element.None, Name = "InvalidMob" };
        var enemies = new List<ServerCharacter> { enemy };

        _mockMonsterManager.Setup(m => m.GetDefinition(100)).Returns(new MonsterDefinition
        {
            MonsterId = 100,
            Tags = new List<string>() // Missing QuestOnly
        });

        // Act
        var result = _spawnManager.StartEncounter(session, enemies, EncounterSource.Scripted);

        // Assert
        Assert.Equal(0, result);
        _mockCombatManager.Verify(m => m.StartEncounter(It.IsAny<int>(), It.IsAny<IEnumerable<ServerCombatant>>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void StartEncounter_Accepts_ElementNone_Mob_With_QuestOnly_Tag()
    {
        // Arrange
        var player = new ServerCharacter { Id = 1, Name = "TestPlayer", CharacterElement = Element.Earth };
        var session = new TestClientSession();
        session.SetCharacter(player);

        var enemy = new ServerCharacter { MonsterId = 100, CharacterElement = Element.None, Name = "QuestMob" };
        var enemies = new List<ServerCharacter> { enemy };

        _mockMonsterManager.Setup(m => m.GetDefinition(100)).Returns(new MonsterDefinition
        {
            MonsterId = 100,
            Tags = new List<string> { "QuestOnly" }
        });

        // Act
        var result = _spawnManager.StartEncounter(session, enemies, EncounterSource.Scripted);

        // Assert
        Assert.True(result > 0);
        _mockCombatManager.Verify(m => m.StartEncounter(It.IsAny<int>(), It.IsAny<IEnumerable<ServerCombatant>>(), It.IsAny<int>()), Times.Once);
    }
}

public class TestClientSession : ClientSession
{
    public TestClientSession() : base() { }

    public void SetCharacter(ServerCharacter character)
    {
        Character = character;
    }

    public override Task SendAsync(NetMessage msg)
    {
        return Task.CompletedTask;
    }
}
