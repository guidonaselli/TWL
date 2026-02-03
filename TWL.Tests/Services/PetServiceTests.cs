using System.Text.Json;
using Moq;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Services;
using TWL.Server.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Services;

namespace TWL.Tests.Services;

// Helper to expose setting Character for tests
public class ClientSessionForTest : ClientSession
{
    public void SetUserId(int id) => UserId = id;
    public void SetCharacter(ServerCharacter c) => Character = c;
}

public class PetServiceTests : IDisposable
{
    private const string TestFile = "Content/Data/pets_service_test.json";
    private readonly CombatManager _combatManager;
    private readonly ServerMetrics _metrics;
    private readonly Mock<IRandomService> _mockRandom;
    private readonly Mock<IPlayerRepository> _mockRepo;
    private readonly Mock<ICombatResolver> _mockResolver;
    private readonly Mock<ISkillCatalog> _mockSkills;
    private readonly Mock<IStatusEngine> _mockStatusEngine;
    private readonly Mock<MonsterManager> _mockMonsterManager;
    private readonly PetManager _petManager;
    private readonly PetService _petService;

    private readonly PlayerService _playerService;

    public PetServiceTests()
    {
        _mockRepo = new Mock<IPlayerRepository>();
        _metrics = new ServerMetrics();
        _playerService = new PlayerService(_mockRepo.Object, _metrics);

        _petManager = new PetManager();
        // Create mock definitions
        var def = new PetDefinition
        {
            PetTypeId = 100,
            Name = "Test Pet",
            Element = Element.Earth,
            BaseHp = 100,
            CaptureRules = new CaptureRules
            {
                IsCapturable = true,
                LevelLimit = 5,
                BaseChance = 0.5f,
                RequiredItemId = 999
            },
            GrowthModel = new PetGrowthModel()
        };
        var tempDef = new PetDefinition
        {
            PetTypeId = 101,
            Name = "Temp Pet",
            Element = Element.Wind,
            IsTemporary = true,
            DurationSeconds = 1,
            GrowthModel = new PetGrowthModel()
        };

        var options = new JsonSerializerOptions { IncludeFields = true };
        Directory.CreateDirectory("Content/Data");
        File.WriteAllText(TestFile, JsonSerializer.Serialize(new List<PetDefinition> { def, tempDef }, options));
        _petManager.Load(TestFile);

        _mockStatusEngine = new Mock<IStatusEngine>();
        _mockResolver = new Mock<ICombatResolver>();
        _mockSkills = new Mock<ISkillCatalog>();
        _mockRandom = new Mock<IRandomService>();
        _mockMonsterManager = new Mock<MonsterManager>();

        _combatManager = new CombatManager(_mockResolver.Object, _mockRandom.Object, _mockSkills.Object,
            _mockStatusEngine.Object);
        _petService = new PetService(_playerService, _petManager, _mockMonsterManager.Object, _combatManager, _mockRandom.Object);
    }

    public void Dispose()
    {
        if (File.Exists(TestFile))
        {
            File.Delete(TestFile);
        }
    }

    [Fact]
    public void CaptureEnemy_Success()
    {
        // Arrange
        var chara = new ServerCharacter { Id = 1, Name = "Trainer" };
        while (chara.Level < 5)
        {
            chara.AddExp(1000); // Level up to 5
        }

        chara.AddItem(999, 1); // Required Item

        var session = new ClientSessionForTest();
        session.SetUserId(1);
        session.SetCharacter(chara);
        _playerService.RegisterSession(session);

        // Define Enemy Monster
        var enemyMonsterDef = new MonsterDefinition
        {
            MonsterId = 200,
            Name = "Test Enemy",
            Element = Element.Earth,
            IsCapturable = true,
            PetTypeId = 100,
            CaptureThreshold = 0.5f
        };
        _mockMonsterManager.Setup(m => m.GetDefinition(200)).Returns(enemyMonsterDef);

        var enemy = new ServerCharacter
        {
            Id = 200, // Runtime ID
            MonsterId = 200, // Definition ID
            Name = "Test Enemy",
            Hp = 10,
            Con = 10, // Just to have stats
            Team = Team.Enemy // CRITICAL: Must be enemy
        };
        // Mock MaxHp manually or ensure stats calc
        // Since ServerCharacter calculates MaxHp from Con, it should be fine if initialized.
        // But ServerCharacter defaults are 8. Let's set it explicitly if possible or rely on base.
        // Actually ServerCharacter.MaxHp comes from ServerCombatant which is virtual.
        // ServerCharacter.MaxHp uses _maxHp which is recalculated.
        // We need to simulate init.
        // Or we can just trust it has > 10 HP.
        // Let's force stats.

        _combatManager.RegisterCombatant(enemy);

        _mockRandom.Setup(r => r.NextFloat()).Returns(0.0f); // Always succeed

        // Debug assertions
        Assert.NotNull(_playerService.GetSession(1));
        Assert.NotNull(_combatManager.GetCombatant(200));
        Assert.NotNull(_petManager.GetDefinition(100));

        // Act
        var result = _petService.CaptureEnemy(1, 200);

        // Assert
        Assert.NotNull(result);
        Assert.Single(chara.Pets);
        Assert.Equal(100, chara.Pets[0].DefinitionId);
        Assert.Equal(40, chara.Pets[0].Amity);
        Assert.False(chara.HasItem(999, 1)); // Consumed
        Assert.Equal(0, enemy.Hp); // Killed
    }

    [Fact]
    public void CaptureEnemy_Fails_LevelTooLow()
    {
        // Arrange
        var chara = new ServerCharacter { Id = 1 }; // Level 1
        chara.AddItem(999, 1);
        var session = new ClientSessionForTest();
        session.SetUserId(1);
        session.SetCharacter(chara);
        _playerService.RegisterSession(session);

        var enemyMonsterDef = new MonsterDefinition
        {
            MonsterId = 200,
            Name = "Test Enemy",
            Element = Element.Earth,
            IsCapturable = true,
            PetTypeId = 100,
            CaptureThreshold = 0.5f
        };
        _mockMonsterManager.Setup(m => m.GetDefinition(200)).Returns(enemyMonsterDef);

        var enemy = new ServerCharacter
        {
            Id = 200,
            MonsterId = 200,
            Hp = 10,
            Team = Team.Enemy
        };
        _combatManager.RegisterCombatant(enemy);

        // Act
        var result = _petService.CaptureEnemy(1, 200);

        // Assert
        Assert.Null(result);
        Assert.Empty(chara.Pets);
        Assert.True(chara.HasItem(999, 1)); // Not consumed on validation fail
    }

    [Fact]
    public void SwitchPet_Cooldown()
    {
        // Arrange
        var chara = new ServerCharacter { Id = 1 };
        var pet1 = new ServerPet(new PetDefinition { PetTypeId = 100, Element = Element.Earth }) { InstanceId = "pet1" };
        var pet2 = new ServerPet(new PetDefinition { PetTypeId = 100, Element = Element.Earth }) { InstanceId = "pet2" };
        chara.AddPet(pet1);
        chara.AddPet(pet2);

        var session = new ClientSessionForTest();
        session.SetUserId(1);
        session.SetCharacter(chara);
        _playerService.RegisterSession(session);

        // Act 1: Switch to Pet 1 (Success)
        var result1 = _petService.SwitchPet(1, "pet1");
        Assert.True(result1);
        Assert.Equal("pet1", chara.ActivePetInstanceId);
        Assert.True(chara.LastPetSwitchTime > DateTime.MinValue);

        // Act 2: Switch to Pet 2 immediately (Fail - Cooldown)
        var result2 = _petService.SwitchPet(1, "pet2");
        Assert.False(result2);
        Assert.Equal("pet1", chara.ActivePetInstanceId); // Still pet1

        // Act 3: Force time travel (Simulate generic wait)
        chara.LastPetSwitchTime = DateTime.UtcNow.AddMinutes(-2);
        var result3 = _petService.SwitchPet(1, "pet2");
        Assert.True(result3);
        Assert.Equal("pet2", chara.ActivePetInstanceId);
    }

    [Fact]
    public void RevivePet_ConsumesItem()
    {
        // Arrange
        var chara = new ServerCharacter { Id = 1 };
        chara.AddGold(1000);
        chara.AddItem(PetService.ItemRevive1Hp, 5); // Add 5 revive potions
        var pet = new ServerPet(new PetDefinition { PetTypeId = 100, Element = Element.Earth, BaseHp = 100 })
            { InstanceId = "pet1", Level = 10, IsDead = true };
        chara.AddPet(pet);

        var session = new ClientSessionForTest();
        session.SetUserId(1);
        session.SetCharacter(chara);
        _playerService.RegisterSession(session);

        // Act
        var result = _petService.RevivePet(1, "pet1");

        // Assert
        Assert.True(result);
        Assert.False(pet.IsDead);
        Assert.Equal(1, pet.Hp); // Revived with 1 HP
        Assert.Equal(1000, chara.Gold); // No gold consumed
        Assert.Equal(4, chara.GetItems(PetService.ItemRevive1Hp).Sum(i => i.Quantity)); // 1 consumed
    }

    [Fact]
    public void RevivePet_Fails_NoItem()
    {
        // Arrange
        var chara = new ServerCharacter { Id = 1 };
        chara.AddGold(1000); // Has gold but no items
        var pet = new ServerPet(new PetDefinition { PetTypeId = 100, Element = Element.Earth, BaseHp = 100 })
            { InstanceId = "pet1", IsDead = true };
        chara.AddPet(pet);

        var session = new ClientSessionForTest();
        session.SetUserId(1);
        session.SetCharacter(chara);
        _playerService.RegisterSession(session);

        // Act
        var result = _petService.RevivePet(1, "pet1");

        // Assert
        Assert.False(result);
        Assert.True(pet.IsDead);
        Assert.Equal(1000, chara.Gold);
    }

    [Fact]
    public void Amity_Effects_Stats()
    {
        var def = new PetDefinition { PetTypeId = 100, BaseStr = 100, Element = Element.Earth };
        var pet = new ServerPet(def);
        pet.Amity = 50;
        pet.RecalculateStats();
        var baseStr = pet.Str;

        // Rebellious (< 20)
        pet.ChangeAmity(-40); // 10
        Assert.True(pet.IsRebellious);
        Assert.True(pet.Str < baseStr); // Penalty applied

        // Loyal (>= 90)
        pet.ChangeAmity(85); // 95
        Assert.True(pet.Str > baseStr); // Bonus applied
    }

    [Fact]
    public void TemporaryPet_Expiry()
    {
        var def = new PetDefinition
        {
            PetTypeId = 101,
            Element = Element.Wind,
            IsTemporary = true,
            DurationSeconds = 1 // 1 second duration
        };
        var pet = new ServerPet(def);

        Assert.NotNull(pet.ExpirationTime);
        Assert.False(pet.IsExpired);

        // Wait slightly (in real test we shouldn't sleep ideally but for 1s it's okay-ish)
        // Since ServerPet uses DateTime.UtcNow directly, we can't mock time easily.
        // We'll manually set ExpirationTime for test stability.

        pet.ExpirationTime = DateTime.UtcNow.AddSeconds(-1);
        Assert.True(pet.IsExpired);
    }

    [Fact]
    public void SwitchPet_Expired_Fails()
    {
        var chara = new ServerCharacter { Id = 1 };
        var pet = new ServerPet(new PetDefinition { PetTypeId = 101, Element = Element.Wind, IsTemporary = true, DurationSeconds = 1 })
        {
            InstanceId = "pet1"
        };
        pet.ExpirationTime = DateTime.UtcNow.AddSeconds(-1); // Expired
        chara.AddPet(pet);

        var session = new ClientSessionForTest();
        session.SetUserId(1);
        session.SetCharacter(chara);
        _playerService.RegisterSession(session);

        var result = _petService.SwitchPet(1, "pet1");

        Assert.False(result);
        Assert.Null(chara.ActivePetInstanceId);
    }

    [Fact]
    public void RevivePet_Expired_Fails()
    {
        var chara = new ServerCharacter { Id = 1 };
        chara.AddGold(1000);
        var pet = new ServerPet(new PetDefinition { PetTypeId = 101, Element = Element.Wind, IsTemporary = true })
        {
            InstanceId = "pet1",
            IsDead = true
        };
        pet.ExpirationTime = DateTime.UtcNow.AddSeconds(-1); // Expired
        chara.AddPet(pet);

        var session = new ClientSessionForTest();
        session.SetUserId(1);
        session.SetCharacter(chara);
        _playerService.RegisterSession(session);

        var result = _petService.RevivePet(1, "pet1");

        Assert.False(result);
        Assert.True(pet.IsDead);
        Assert.Equal(1000, chara.Gold); // No gold consumed
    }

    [Fact]
    public void PetDeath_ReducesAmity()
    {
        // Arrange
        var chara = new ServerCharacter { Id = 1 };
        var pet = new ServerPet(new PetDefinition { PetTypeId = 100, Element = Element.Earth, BaseHp = 100 })
        {
            InstanceId = "pet1",
            Amity = 50,
            Hp = 10
        };
        chara.AddPet(pet);

        // We need to trigger HandlePetDeath via CombatManager event or call pet.Die() directly.
        // Since HandlePetDeath is private and triggered by event, let's simulate the event.
        // We can't easily trigger the event on the real CombatManager without a real combat context.
        // However, we can trust pet.Die() logic if we tested ServerPet logic, OR we can test that PetService hooked it up.
        // Since we are testing PetService integration, let's just call pet.Die() manually to verify the effect on Amity,
        // as testing the event wiring might require more complex CombatManager mocking setup.

        // Actually, let's just test pet.Die() logic here as part of the system test if not covered elsewhere.

        var initialAmity = pet.Amity;
        pet.Die();

        Assert.True(pet.IsDead);
        Assert.Equal(0, pet.Hp);
        Assert.Equal(initialAmity - 1, pet.Amity);
    }
}