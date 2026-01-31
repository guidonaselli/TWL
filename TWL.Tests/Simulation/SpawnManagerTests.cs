using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Moq;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Domain.World;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Net.Network;
using TWL.Shared.Net.Messages;

namespace TWL.Tests.Simulation;

public class TestClientSession : ClientSession
{
    public NetMessage? LastMessage { get; private set; }

    public TestClientSession(ServerCharacter character)
    {
        Character = character;
    }

    public override async Task SendAsync(NetMessage msg)
    {
        LastMessage = msg;
        await Task.CompletedTask;
    }
}

public class SpawnManagerTests
{
    [Fact]
    public void Load_ShouldLoadConfigs()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "twl_spawns_" + System.Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        var configFile = Path.Combine(tempDir, "1001.spawns.json");

        var config = new ZoneSpawnConfig
        {
            MapId = 1001,
            RandomEncounterEnabled = true,
            StepChance = 0.5f,
            SpawnRegions = new List<SpawnRegion>
            {
                new SpawnRegion { AllowedMonsterIds = new List<int> { 1 } }
            }
        };

        File.WriteAllText(configFile, JsonSerializer.Serialize(config));

        var mockMonsters = new Mock<MonsterManager>();
        var mockCombat = new Mock<CombatManager>(null, null, null, null);
        var manager = new SpawnManager(mockMonsters.Object, mockCombat.Object);

        // Act
        manager.Load(tempDir);

        // Cleanup
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void OnPlayerMoved_ShouldTriggerEncounter_WhenChanceIsHigh_AndPositionValid()
    {
        // Arrange
        var mockMonsters = new Mock<MonsterManager>();
        mockMonsters.Setup(m => m.GetDefinition(It.IsAny<int>())).Returns(new MonsterDefinition { MonsterId = 1, Name = "TestMob", BaseHp = 10 });

        var mockCombat = new Mock<CombatManager>(null, null, null, null);
        mockCombat.Setup(c => c.StartEncounter(It.IsAny<int>(), It.IsAny<List<ServerCharacter>>())).Verifiable();

        var manager = new SpawnManager(mockMonsters.Object, mockCombat.Object);

        // Inject Config via file
        var config = new ZoneSpawnConfig
        {
            MapId = 1001,
            RandomEncounterEnabled = true,
            StepChance = 2.0f, // > 1.0 ensures trigger
            SpawnRegions = new List<SpawnRegion>
            {
                new SpawnRegion { X = 0, Y = 0, Width = 100, Height = 100, AllowedMonsterIds = new List<int> { 1 } }
            }
        };

        var tempDir = Path.Combine(Path.GetTempPath(), "twl_spawns_test_" + System.Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "1001.spawns.json"), JsonSerializer.Serialize(config));
        manager.Load(tempDir);

        // Setup Player
        var player = new ServerCharacter { Id = 1, Name = "TestPlayer", MapId = 1001, X = 10, Y = 10 };
        var session = new TestClientSession(player);

        // Act
        manager.OnPlayerMoved(session);

        // Assert
        mockCombat.Verify(c => c.StartEncounter(It.IsAny<int>(), It.IsAny<List<ServerCharacter>>()), Times.Once);
        Assert.NotNull(session.LastMessage);
        Assert.Equal(Opcode.EncounterStarted, session.LastMessage.Op);

        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void OnPlayerMoved_ShouldNOTTriggerEncounter_WhenOutsideRegion()
    {
        // Arrange
        var mockMonsters = new Mock<MonsterManager>();
        mockMonsters.Setup(m => m.GetDefinition(It.IsAny<int>())).Returns(new MonsterDefinition { MonsterId = 1, Name = "TestMob", BaseHp = 10 });

        var mockCombat = new Mock<CombatManager>(null, null, null, null);

        var manager = new SpawnManager(mockMonsters.Object, mockCombat.Object);

        var config = new ZoneSpawnConfig
        {
            MapId = 1001,
            RandomEncounterEnabled = true,
            StepChance = 2.0f,
            SpawnRegions = new List<SpawnRegion>
            {
                new SpawnRegion { X = 0, Y = 0, Width = 10, Height = 10, AllowedMonsterIds = new List<int> { 1 } }
            }
        };

        var tempDir = Path.Combine(Path.GetTempPath(), "twl_spawns_test_" + System.Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "1001.spawns.json"), JsonSerializer.Serialize(config));
        manager.Load(tempDir);

        // Setup Player OUTSIDE region (X=20 > 10)
        var player = new ServerCharacter { Id = 1, Name = "TestPlayer", MapId = 1001, X = 20, Y = 20 };
        var session = new TestClientSession(player);

        // Act
        manager.OnPlayerMoved(session);

        // Assert
        mockCombat.Verify(c => c.StartEncounter(It.IsAny<int>(), It.IsAny<List<ServerCharacter>>()), Times.Never);
        Assert.Null(session.LastMessage);

        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void OnPlayerMoved_ShouldNOTTriggerEncounter_WhenAlreadyInCombat()
    {
        // Arrange
        var mockMonsters = new Mock<MonsterManager>();
        mockMonsters.Setup(m => m.GetDefinition(It.IsAny<int>())).Returns(new MonsterDefinition { MonsterId = 1, Name = "TestMob", BaseHp = 10 });

        var mockCombat = new Mock<CombatManager>(null, null, null, null);
        mockCombat.Setup(c => c.GetCombatant(It.IsAny<int>())).Returns(new ServerCharacter()); // Simulate existing combatant

        var manager = new SpawnManager(mockMonsters.Object, mockCombat.Object);

        var config = new ZoneSpawnConfig
        {
            MapId = 1001,
            RandomEncounterEnabled = true,
            StepChance = 2.0f,
            SpawnRegions = new List<SpawnRegion>
            {
                new SpawnRegion { X = 0, Y = 0, Width = 100, Height = 100, AllowedMonsterIds = new List<int> { 1 } }
            }
        };

        var tempDir = Path.Combine(Path.GetTempPath(), "twl_spawns_test_" + System.Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "1001.spawns.json"), JsonSerializer.Serialize(config));
        manager.Load(tempDir);

        var player = new ServerCharacter { Id = 1, Name = "TestPlayer", MapId = 1001, X = 10, Y = 10 };
        var session = new TestClientSession(player);

        // Act
        manager.OnPlayerMoved(session);

        // Assert
        mockCombat.Verify(c => c.StartEncounter(It.IsAny<int>(), It.IsAny<List<ServerCharacter>>()), Times.Never);
        Assert.Null(session.LastMessage);

        Directory.Delete(tempDir, true);
    }
}
