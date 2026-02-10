using Moq;
using System.Text.Json;
using TWL.Server.Features.Combat;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Domain.World;
using TWL.Shared.Services;

namespace TWL.Tests.Server.Services;

public class SpawnManagerTests
{
    private readonly Mock<MonsterManager> _monsterManager;
    private readonly Mock<CombatManager> _combatManager;
    private readonly Mock<IRandomService> _random;
    private readonly Mock<PlayerService> _playerService;
    private readonly SpawnManager _spawnManager;

    public SpawnManagerTests()
    {
        _monsterManager = new Mock<MonsterManager>();

        var resolver = new Mock<ICombatResolver>();
        var status = new Mock<IStatusEngine>();
        var skills = new Mock<ISkillCatalog>();

        _combatManager = new Mock<CombatManager>(resolver.Object, new Mock<IRandomService>().Object, skills.Object, status.Object);
        _random = new Mock<IRandomService>();

        var repo = new Mock<IPlayerRepository>();
        var metrics = new ServerMetrics();
        _playerService = new Mock<PlayerService>(repo.Object, metrics);

        _spawnManager = new SpawnManager(_monsterManager.Object, _combatManager.Object, _random.Object, _playerService.Object);
    }

    [Fact]
    public void StartEncounter_IsIdempotent()
    {
        // Arrange
        var player = new ServerCharacter { Id = 1, Name = "TestPlayer", CharacterElement = Element.Fire };
        var session = new TestClientSession { Character = player };

        var mob = new ServerCharacter { Id = -1, Name = "Mob", MonsterId = 100, CharacterElement = Element.Water };
        var participants = new List<ServerCharacter> { mob };

        // First Call
        _combatManager.Setup(cm => cm.GetCombatant(player.Id)).Returns((ServerCombatant?)null);
        _random.Setup(r => r.Next(It.IsAny<string?>())).Returns(12345);

        // Capture encounter ID
        int encounterId1 = _spawnManager.StartEncounter(session, participants, EncounterSource.Scripted);

        Assert.True(encounterId1 > 0);

        // Simulate CombatManager having the combatant now
        var combatant = player;
        combatant.EncounterId = encounterId1;
        _combatManager.Setup(cm => cm.GetCombatant(player.Id)).Returns(combatant);
        _combatManager.Setup(cm => cm.GetAllCombatants()).Returns(new List<ServerCombatant> { player, mob });

        // Second Call
        int encounterId2 = _spawnManager.StartEncounter(session, participants, EncounterSource.Scripted);

        // Assert
        Assert.Equal(encounterId1, encounterId2);
        // Verify StartEncounter on CombatManager was called ONLY ONCE
        _combatManager.Verify(cm => cm.StartEncounter(It.IsAny<int>(), It.IsAny<IEnumerable<ServerCombatant>>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public void StartEncounter_IncludesActivePet()
    {
        // Arrange
        var player = new ServerCharacter { Id = 1, Name = "Player", CharacterElement = Element.Fire };
        var pet = new ServerPet { Id = -1, InstanceId = "pet1", Name = "Pet", CharacterElement = Element.Water };
        player.AddPet(pet);
        player.SetActivePet(pet.InstanceId);

        var session = new TestClientSession { Character = player };
        var mob = new ServerCharacter { Id = -2, Name = "Mob", MonsterId = 100, CharacterElement = Element.Earth };
        var participants = new List<ServerCharacter> { mob };

        _random.Setup(r => r.Next(It.IsAny<string?>())).Returns(123);

        // Act
        _spawnManager.StartEncounter(session, participants, EncounterSource.Scripted);

        // Assert
        _combatManager.Verify(cm => cm.StartEncounter(
            It.IsAny<int>(),
            It.Is<IEnumerable<ServerCombatant>>(list => list.Contains(player) && list.Contains(pet) && list.Contains(mob)),
            It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public void StartEncounter_AbortsIfElementNone()
    {
        // Arrange
        var player = new ServerCharacter { Id = 1, Name = "Player", CharacterElement = Element.None }; // Invalid
        var session = new TestClientSession { Character = player };
        var mob = new ServerCharacter { Id = -2, Name = "Mob", MonsterId = 100, CharacterElement = Element.Earth };

        // Act
        var result = _spawnManager.StartEncounter(session, new List<ServerCharacter> { mob }, EncounterSource.Scripted);

        // Assert
        Assert.Equal(0, result);
        _combatManager.Verify(cm => cm.StartEncounter(It.IsAny<int>(), It.IsAny<IEnumerable<ServerCombatant>>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void OnPlayerMoved_AccumulatesSteps_AndTriggersEncounter()
    {
        // Arrange
        var player = new ServerCharacter { Id = 1, MapId = 1001, X = 100, Y = 100, CharacterElement = Element.Fire };
        var session = new TestClientSession { Character = player };

        // Inject config via Load
        var config = new ZoneSpawnConfig
        {
            MapId = 1001,
            RandomEncounterEnabled = true,
            StepChance = 1.0f, // 100% chance
            SpawnRegions = new List<SpawnRegion>
            {
                // Must be within region. Player is at 100,100 (Pixels).
                // Region is in Tiles (32px).
                // 100px = 3.125 tiles.
                // Region 0,0 10x10 tiles covers 0-320px.
                new SpawnRegion { X = 0, Y = 0, Width = 10, Height = 10, AllowedMonsterIds = new List<int> { 2001 } }
            }
        };

        var json = JsonSerializer.Serialize(config);
        var tempDir = Path.Combine(Path.GetTempPath(), "spawns_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "1001.spawns.json"), json);

        try
        {
            _spawnManager.Load(tempDir);

            // Mock Monster
            _monsterManager.Setup(m => m.GetDefinition(2001)).Returns(new MonsterDefinition { MonsterId = 2001, Name = "Slime", Element = Element.Water });

            // Mock Random: Step Check returns 0.0 (< 1.0)
            _random.Setup(r => r.NextDouble(It.IsAny<string?>())).Returns(0.0);
            _random.Setup(r => r.Next(1, 4, It.IsAny<string?>())).Returns(1); // Count
            _random.Setup(r => r.Next(0, It.IsAny<int>(), It.IsAny<string?>())).Returns(0); // For list selection

            // Act
            _spawnManager.OnPlayerMoved(session);

            // Assert
            // Verify CombatManager.StartEncounter called
            _combatManager.Verify(cm => cm.StartEncounter(It.IsAny<int>(), It.IsAny<IEnumerable<ServerCombatant>>(), It.IsAny<int>()), Times.Once);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}

// Helper for Session
public class TestClientSession : ClientSession
{
    public new ServerCharacter? Character
    {
        get => base.Character;
        set => base.Character = value;
    }

    public TestClientSession() : base()
    {
    }

    // Override SendAsync to avoid NRE on null stream/client
    public override Task SendAsync(TWL.Shared.Net.Network.NetMessage msg)
    {
        return Task.CompletedTask;
    }
}
