using System.Collections.Concurrent;
using System.Text.Json;
using Moq;
using TWL.Server.Features.Combat;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.World;
using TWL.Shared.Net.Network;
using TWL.Shared.Services;
using Xunit;

namespace TWL.Tests.Simulation;

public class EncounterTestClientSession : ClientSession
{
    public List<NetMessage> SentMessages { get; } = new();

    public EncounterTestClientSession(ServerCharacter character)
    {
        Character = character;
    }

    public override Task SendAsync(NetMessage msg)
    {
        SentMessages.Add(msg);
        return Task.CompletedTask;
    }
}

public class EncounterTests
{
    private readonly Mock<IRandomService> _randomMock;
    private readonly Mock<IPlayerRepository> _playerRepoMock;
    private readonly PlayerService _playerService;
    private readonly MonsterManager _monsterManager;
    private readonly CombatManager _combatManager;
    private readonly SpawnManager _spawnManager;

    public EncounterTests()
    {
        _randomMock = new Mock<IRandomService>();
        _playerRepoMock = new Mock<IPlayerRepository>();
        _playerService = new PlayerService(_playerRepoMock.Object, new ServerMetrics());
        _monsterManager = new MonsterManager();

        var skills = new Mock<ISkillCatalog>();
        var resolver = new StandardCombatResolver(_randomMock.Object, skills.Object);
        var statusEngine = new StatusEngine();

        _combatManager = new CombatManager(resolver, _randomMock.Object, skills.Object, statusEngine);
        _spawnManager = new SpawnManager(_monsterManager, _combatManager, _randomMock.Object, _playerService);
    }

    private ServerCharacter CreatePlayer(Element element = Element.Earth)
    {
        return new ServerCharacter
        {
            Id = 1,
            Name = "TestPlayer",
            CharacterElement = element,
            Con = 10, // MaxHp = 100
            Hp = 100,
            Team = Team.Player
        };
    }

    private ServerCharacter CreateEnemy()
    {
        return new ServerCharacter
        {
            Id = -1,
            Name = "TestMob",
            CharacterElement = Element.Fire,
            Con = 5, // MaxHp = 50
            Hp = 50,
            Team = Team.Enemy,
            MonsterId = 2001
        };
    }

    [Fact]
    public void StartEncounter_ShouldStart_WhenValid()
    {
        var player = CreatePlayer();
        var session = new EncounterTestClientSession(player);
        var enemy = CreateEnemy();

        var encounterId = _spawnManager.StartEncounter(session, new List<ServerCharacter> { enemy }, EncounterSource.Scripted);

        Assert.True(encounterId > 0);
        Assert.Single(session.SentMessages);
        Assert.NotNull(_combatManager.GetCombatant(player.Id));
        Assert.NotNull(_combatManager.GetCombatant(enemy.Id));
        Assert.Equal(encounterId, player.EncounterId);
        Assert.Equal(encounterId, enemy.EncounterId);
    }

    [Fact]
    public void StartEncounter_ShouldFail_WhenPlayerIsElementNone()
    {
        var player = CreatePlayer(Element.None);
        var session = new EncounterTestClientSession(player);
        var enemy = CreateEnemy();

        var encounterId = _spawnManager.StartEncounter(session, new List<ServerCharacter> { enemy }, EncounterSource.Scripted);

        Assert.Equal(0, encounterId);
        Assert.Empty(session.SentMessages);
        Assert.Null(_combatManager.GetCombatant(player.Id));
    }

    [Fact]
    public void StartEncounter_ShouldBeIdempotent_AndResendPacket()
    {
        var player = CreatePlayer();
        var session = new EncounterTestClientSession(player);
        var enemy = CreateEnemy();

        // First Start
        var id1 = _spawnManager.StartEncounter(session, new List<ServerCharacter> { enemy }, EncounterSource.Scripted);
        Assert.True(id1 > 0);
        Assert.Single(session.SentMessages);

        // Second Start (Retry)
        var id2 = _spawnManager.StartEncounter(session, new List<ServerCharacter> { enemy }, EncounterSource.Scripted);

        Assert.Equal(id1, id2);
        Assert.Equal(2, session.SentMessages.Count); // Should resend packet
        Assert.Equal(Opcode.EncounterStarted, session.SentMessages[1].Op);
    }

    [Fact]
    public void RandomEncounter_ShouldTrigger_AfterSteps()
    {
        // Setup Map Config
        var config = new ZoneSpawnConfig
        {
            MapId = 1001,
            RandomEncounterEnabled = true,
            StepChance = 1.0f, // 100% chance
            SpawnRegions = new List<SpawnRegion>
            {
                new SpawnRegion { AllowedMonsterIds = new List<int> { 2001 }, X=0, Y=0, Width=100, Height=100 }
            }
        };

        // We need to inject config into SpawnManager.
        // Since SpawnManager.Load reads files, we can either write a temp file or use reflection to inject config.
        // Or create a method to add config manually?
        // Since we can't easily modify private _configs, let's write a temp file and load it.

        var tempFile = Path.GetTempFileName();
        var tempDir = Path.Combine(Path.GetTempPath(), "SpawnTest_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        var configFile = Path.Combine(tempDir, "1001.spawns.json");
        File.WriteAllText(configFile, JsonSerializer.Serialize(config));

        // Mock MonsterManager to return definition
        // MonsterManager loads from file too.
        var monsterFile = Path.Combine(tempDir, "monsters.json");
        var monsters = new List<MonsterDefinition>
        {
            new MonsterDefinition { MonsterId = 2001, Name = "TestMob", Element = Element.Fire, BaseHp = 50 }
        };
        File.WriteAllText(monsterFile, JsonSerializer.Serialize(monsters));

        _monsterManager.Load(monsterFile);
        _spawnManager.Load(tempDir);

        var player = CreatePlayer();
        player.MapId = 1001;
        var session = new EncounterTestClientSession(player);

        // Mock Random to return success for encounter check
        // _randomMock.Setup(r => r.NextDouble()).Returns(0.0); // 0.0 < 1.0
        // Wait, NextDouble is used for step check AND monster selection.
        // We need it to work for both.
        // Using Mock Behavior.Strict might be annoying. Default is Loose.
        // Loose returns default (0.0 for double).

        // Act - Move
        _spawnManager.OnPlayerMoved(session);

        // Assert
        Assert.Single(session.SentMessages);
        Assert.Equal(Opcode.EncounterStarted, session.SentMessages[0].Op);
        Assert.NotNull(_combatManager.GetCombatant(player.Id));

        // Cleanup
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void RoamingMob_ShouldSpawn_WhenBelowMinCount()
    {
        // Setup Config
        var config = new ZoneSpawnConfig
        {
            MapId = 1002,
            MinMobCount = 2,
            SpawnRegions = new List<SpawnRegion>
            {
                new SpawnRegion { AllowedMonsterIds = new List<int> { 2001 }, X=0, Y=0, Width=10, Height=10 }
            }
        };

        var tempDir = Path.Combine(Path.GetTempPath(), "SpawnTest_Roaming_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "1002.spawns.json"), JsonSerializer.Serialize(config));

        var monsters = new List<MonsterDefinition>
        {
            new MonsterDefinition { MonsterId = 2001, Name = "TestMob", Element = Element.Fire, BaseHp = 50, Behavior = new BehaviorProfile() }
        };
        File.WriteAllText(Path.Combine(tempDir, "monsters.json"), JsonSerializer.Serialize(monsters));

        _monsterManager.Load(Path.Combine(tempDir, "monsters.json"));
        _spawnManager.Load(tempDir);

        // Act - Trigger Update to spawn mobs
        // _respawnTimer += dt. RespawnCheckInterval is 5.0f.
        _spawnManager.Update(6.0f);

        // How to verify? _roamingMobs is private.
        // We can check if StartEncounter works with a fake roaming mob collision?
        // Or checking logs?
        // Reflection to check _roamingMobs count.

        var field = typeof(SpawnManager).GetField("_roamingMobs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var list = field.GetValue(_spawnManager) as List<ServerCharacter>;

        Assert.NotNull(list);
        Assert.Equal(2, list.Count);

        // Cleanup
        Directory.Delete(tempDir, true);
    }
}
