using Moq;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Services;
using TWL.Server.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Services;

namespace TWL.Tests.PetTests;

public class PetSystemTests : IDisposable
{
    private readonly CombatManager _combatManager;
    private readonly ServerMetrics _metrics;
    private readonly Mock<IRandomService> _mockRandom;
    private readonly Mock<IPlayerRepository> _mockRepo;
    private readonly Mock<ICombatResolver> _mockResolver;
    private readonly Mock<ISkillCatalog> _mockSkills;
    private readonly Mock<IStatusEngine> _mockStatusEngine;
    private readonly PetManager _petManager;
    private readonly PetService _petService;

    private readonly PlayerService _playerService;

    public PetSystemTests()
    {
        _mockRepo = new Mock<IPlayerRepository>();
        _metrics = new ServerMetrics();
        _playerService = new PlayerService(_mockRepo.Object, _metrics);

        _petManager = new PetManager();
        Directory.CreateDirectory("Content/Data");
        File.WriteAllText("Content/Data/pets_test.json", @"
[
  {
    ""PetTypeId"": 9999,
    ""Name"": ""Test Wolf"",
    ""Type"": ""Capture"",
    ""Element"": ""Earth"",
    ""BaseHp"": 100,
    ""BaseStr"": 10,
    ""BaseCon"": 10,
    ""BaseInt"": 5,
    ""BaseWis"": 5,
    ""BaseAgi"": 10,
    ""CaptureRules"": {
        ""IsCapturable"": true,
        ""LevelLimit"": 1,
        ""BaseChance"": 0.5
    },
    ""GrowthModel"": {
      ""HpGrowthPerLevel"": 10.0,
      ""SpGrowthPerLevel"": 5.0
    }
  }
]");
        _petManager.Load("Content/Data/pets_test.json");

        _mockStatusEngine = new Mock<IStatusEngine>();
        _mockResolver = new Mock<ICombatResolver>();
        _mockSkills = new Mock<ISkillCatalog>();
        _mockRandom = new Mock<IRandomService>();

        _combatManager = new CombatManager(_mockResolver.Object, _mockRandom.Object, _mockSkills.Object,
            _mockStatusEngine.Object);
        _petService = new PetService(_playerService, _petManager, _combatManager, _mockRandom.Object);
    }

    public void Dispose()
    {
        if (File.Exists("Content/Data/pets_test.json"))
        {
            File.Delete("Content/Data/pets_test.json");
        }
    }

    [Fact]
    public void CapturePet_Success()
    {
        // Setup Session
        var session = new ClientSessionForTest();
        session.SetCharacter(new ServerCharacter { Id = 1, Name = "Trainer" });
        _playerService.RegisterSession(session);

        // Setup Enemy
        var enemyDef = new EnemyCharacter("Wild Wolf", Element.Earth, true)
        {
            PetTypeId = 9999,
            CaptureThreshold = 0.5f,
            Health = 10,
            MaxHealth = 100
        };
        var enemy = new ServerEnemy(enemyDef) { Id = 200, Hp = 10 };
        _combatManager.RegisterCombatant(enemy);

        // Mock RNG
        _mockRandom.Setup(r => r.NextFloat()).Returns(0.1f);

        // Act
        var petInstanceId = _petService.CaptureEnemy(1, 200);

        // Assert
        Assert.NotNull(petInstanceId);
        var pet = session.Character.Pets[0];
        Assert.Equal(9999, pet.DefinitionId);
        Assert.Equal(40, pet.Amity); // Default capture amity
    }

    [Fact]
    public void PetGrowth_RecalculateStats()
    {
        // Act
        var def = _petManager.GetDefinition(9999);
        var pet = new ServerPet(def);

        var hpLvl1 = pet.MaxHp;

        pet.AddExp(200); // Level up -> 2

        // Assert
        Assert.Equal(2, pet.Level);
        Assert.True(pet.MaxHp > hpLvl1);
    }

    [Fact]
    public void Combat_PetCanBeTarget()
    {
        // Setup
        var petDef = _petManager.GetDefinition(9999);
        var pet = new ServerPet(petDef) { Id = -100 }; // Runtime ID
        _combatManager.RegisterCombatant(pet);

        var attacker = new ServerCharacter { Id = 1, Str = 50 };
        _combatManager.RegisterCombatant(attacker);

        _mockSkills.Setup(s => s.GetSkillById(1))
            .Returns(new Skill { SkillId = 1, SpCost = 0, Effects = new List<SkillEffect>() });
        _mockResolver.Setup(r =>
                r.CalculateDamage(It.IsAny<ServerCombatant>(), It.IsAny<ServerCombatant>(),
                    It.IsAny<UseSkillRequest>()))
            .Returns(50);

        // Act
        var result = _combatManager.UseSkill(new UseSkillRequest { PlayerId = 1, TargetId = -100, SkillId = 1 });

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(-100, result[0].TargetId);
        Assert.Equal(50, result[0].Damage);
        // Verify pet took damage
        Assert.True(pet.Hp < pet.MaxHp);
    }
}

// Helper to expose setting Character
public class ClientSessionForTest : ClientSession
{
    public ClientSessionForTest()
    {
        UserId = 1;
    }

    public void SetCharacter(ServerCharacter c) => Character = c;
}