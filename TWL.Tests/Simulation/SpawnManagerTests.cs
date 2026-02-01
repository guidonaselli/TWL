using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TWL.Server.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.World;
using TWL.Shared.Net.Network;
using TWL.Shared.Services;

namespace TWL.Tests.Simulation;

public class TestClientSession : ClientSession
{
    public TestClientSession(ServerCharacter character)
    {
        Character = character;
    }

    public NetMessage? LastMessage { get; private set; }

    public override async Task SendAsync(NetMessage msg)
    {
        LastMessage = msg;
        await Task.CompletedTask;
    }
}

public class SpawnManagerTests
{
    private readonly IRandomService _random = new SeedableRandomService(NullLogger<SeedableRandomService>.Instance, 12345);

    [Fact]
    public void Load_ShouldLoadConfigs()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "twl_spawns_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        var configFile = Path.Combine(tempDir, "1001.spawns.json");

        var config = new ZoneSpawnConfig
        {
            MapId = 1001,
            RandomEncounterEnabled = true,
            StepChance = 0.5f,
            SpawnRegions = new List<SpawnRegion>
            {
                new() { AllowedMonsterIds = new List<int> { 1 } }
            }
        };

        File.WriteAllText(configFile, JsonSerializer.Serialize(config));

        var mockMonsters = new Mock<MonsterManager>();
        var mockCombat = new Mock<CombatManager>(null, null, null, null);
        var manager = new SpawnManager(mockMonsters.Object, mockCombat.Object, _random);

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
        mockMonsters.Setup(m => m.GetDefinition(It.IsAny<int>())).Returns(new MonsterDefinition
            { MonsterId = 1, Name = "TestMob", BaseHp = 10 });

        var mockCombat = new Mock<CombatManager>(null, null, null, null);
        mockCombat.Setup(c => c.StartEncounter(It.IsAny<int>(), It.IsAny<List<ServerCharacter>>(), It.IsAny<int>()))
            .Verifiable();

        var manager = new SpawnManager(mockMonsters.Object, mockCombat.Object, _random);

        // Inject Config via file
        var config = new ZoneSpawnConfig
        {
            MapId = 1001,
            RandomEncounterEnabled = true,
            StepChance = 2.0f, // > 1.0 ensures trigger
            SpawnRegions = new List<SpawnRegion>
            {
                new() { X = 0, Y = 0, Width = 100, Height = 100, AllowedMonsterIds = new List<int> { 1 } }
            }
        };

        var tempDir = Path.Combine(Path.GetTempPath(), "twl_spawns_test_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "1001.spawns.json"), JsonSerializer.Serialize(config));
        manager.Load(tempDir);

        // Setup Player
        var player = new ServerCharacter { Id = 1, Name = "TestPlayer", MapId = 1001, X = 10, Y = 10 };
        var session = new TestClientSession(player);

        // Act
        manager.OnPlayerMoved(session);

        // Assert
        mockCombat.Verify(c => c.StartEncounter(It.IsAny<int>(), It.IsAny<List<ServerCharacter>>(), It.IsAny<int>()),
            Times.Once);
        Assert.NotNull(session.LastMessage);
        Assert.Equal(Opcode.EncounterStarted, session.LastMessage.Op);

        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void OnPlayerMoved_ShouldNOTTriggerEncounter_WhenOutsideRegion()
    {
        // Arrange
        var mockMonsters = new Mock<MonsterManager>();
        mockMonsters.Setup(m => m.GetDefinition(It.IsAny<int>())).Returns(new MonsterDefinition
            { MonsterId = 1, Name = "TestMob", BaseHp = 10 });

        var mockCombat = new Mock<CombatManager>(null, null, null, null);

        var manager = new SpawnManager(mockMonsters.Object, mockCombat.Object, _random);

        var config = new ZoneSpawnConfig
        {
            MapId = 1001,
            RandomEncounterEnabled = true,
            StepChance = 2.0f,
            SpawnRegions = new List<SpawnRegion>
            {
                new() { X = 0, Y = 0, Width = 10, Height = 10, AllowedMonsterIds = new List<int> { 1 } }
            }
        };

        var tempDir = Path.Combine(Path.GetTempPath(), "twl_spawns_test_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "1001.spawns.json"), JsonSerializer.Serialize(config));
        manager.Load(tempDir);

        // Setup Player OUTSIDE region (X=20 > 10)
        var player = new ServerCharacter { Id = 1, Name = "TestPlayer", MapId = 1001, X = 20, Y = 20 };
        var session = new TestClientSession(player);

        // Act
        manager.OnPlayerMoved(session);

        // Assert
        mockCombat.Verify(c => c.StartEncounter(It.IsAny<int>(), It.IsAny<List<ServerCharacter>>(), It.IsAny<int>()),
            Times.Never);
        Assert.Null(session.LastMessage);

        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void OnPlayerMoved_ShouldNOTTriggerEncounter_WhenAlreadyInCombat()
    {
        // Arrange
        var mockMonsters = new Mock<MonsterManager>();
        mockMonsters.Setup(m => m.GetDefinition(It.IsAny<int>())).Returns(new MonsterDefinition
            { MonsterId = 1, Name = "TestMob", BaseHp = 10 });

        var mockCombat = new Mock<CombatManager>(null, null, null, null);
        mockCombat.Setup(c => c.GetCombatant(It.IsAny<int>()))
            .Returns(new ServerCharacter()); // Simulate existing combatant

        var manager = new SpawnManager(mockMonsters.Object, mockCombat.Object, _random);

        var config = new ZoneSpawnConfig
        {
            MapId = 1001,
            RandomEncounterEnabled = true,
            StepChance = 2.0f,
            SpawnRegions = new List<SpawnRegion>
            {
                new() { X = 0, Y = 0, Width = 100, Height = 100, AllowedMonsterIds = new List<int> { 1 } }
            }
        };

        var tempDir = Path.Combine(Path.GetTempPath(), "twl_spawns_test_" + Guid.NewGuid());
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

    [Fact]
    public void StartEncounter_ShouldBeDeterministic_WithSameSeed()
    {
        // Arrange
        var mockMonsters = new Mock<MonsterManager>();
        // Return different definitions to verify selection logic
        mockMonsters.Setup(m => m.GetDefinition(1)).Returns(new MonsterDefinition { MonsterId = 1, Name = "Mob1", BaseHp = 10, EncounterWeight = 10 });
        mockMonsters.Setup(m => m.GetDefinition(2)).Returns(new MonsterDefinition { MonsterId = 2, Name = "Mob2", BaseHp = 20, EncounterWeight = 10 });

        // Capture results
        int encounterSeed1 = -1;
        int encounterSeed2 = -1;
        List<ServerCharacter> participants1 = null;
        List<ServerCharacter> participants2 = null;

        var mockCombat1 = new Mock<CombatManager>(null, null, null, null);
        mockCombat1.Setup(c => c.StartEncounter(It.IsAny<int>(), It.IsAny<List<ServerCharacter>>(), It.IsAny<int>()))
            .Callback<int, List<ServerCharacter>, int>((id, parts, seed) =>
            {
                encounterSeed1 = seed;
                participants1 = parts;
            });

        var mockCombat2 = new Mock<CombatManager>(null, null, null, null);
        mockCombat2.Setup(c => c.StartEncounter(It.IsAny<int>(), It.IsAny<List<ServerCharacter>>(), It.IsAny<int>()))
             .Callback<int, List<ServerCharacter>, int>((id, parts, seed) =>
             {
                 encounterSeed2 = seed;
                 participants2 = parts;
             });

        // Seed
        int seed = 54321;
        var rng1 = new SeedableRandomService(NullLogger<SeedableRandomService>.Instance, seed);
        var rng2 = new SeedableRandomService(NullLogger<SeedableRandomService>.Instance, seed);

        var manager1 = new SpawnManager(mockMonsters.Object, mockCombat1.Object, rng1);
        var manager2 = new SpawnManager(mockMonsters.Object, mockCombat2.Object, rng2);

        // Config
        var config = new ZoneSpawnConfig
        {
            MapId = 1001,
            RandomEncounterEnabled = true,
            StepChance = 2.0f,
            SpawnRegions = new List<SpawnRegion>
            {
                new() { X = 0, Y = 0, Width = 100, Height = 100, AllowedMonsterIds = new List<int> { 1, 2 } }
            }
        };

        var tempDir = Path.Combine(Path.GetTempPath(), "twl_spawns_det_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "1001.spawns.json"), JsonSerializer.Serialize(config));
        manager1.Load(tempDir);
        manager2.Load(tempDir);

        var player1 = new ServerCharacter { Id = 1, Name = "P1", MapId = 1001, X = 10, Y = 10 };
        var player2 = new ServerCharacter { Id = 2, Name = "P2", MapId = 1001, X = 10, Y = 10 }; // Different player ID shouldn't affect RNG if called in same order

        var session1 = new TestClientSession(player1);
        var session2 = new TestClientSession(player2);

        // Act
        manager1.OnPlayerMoved(session1);
        manager2.OnPlayerMoved(session2);

        // Assert
        Assert.Equal(encounterSeed1, encounterSeed2);
        Assert.NotNull(participants1);
        Assert.NotNull(participants2);
        Assert.Equal(participants1.Count, participants2.Count);
        for (int i = 1; i < participants1.Count; i++) // Skip player at index 0
        {
            Assert.Equal(participants1[i].Name, participants2[i].Name);
            Assert.Equal(participants1[i].Hp, participants2[i].Hp);
        }

        Directory.Delete(tempDir, true);
    }
}
