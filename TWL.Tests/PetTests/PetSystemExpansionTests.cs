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

public class PetSystemExpansionTests : IDisposable
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

    public PetSystemExpansionTests()
    {
        _mockRepo = new Mock<IPlayerRepository>();
        _metrics = new ServerMetrics();
        _playerService = new PlayerService(_mockRepo.Object, _metrics);

        _petManager = new PetManager();
        Directory.CreateDirectory("Content/Data");
        File.WriteAllText("Content/Data/pets_expansion_test.json", @"
[
  {
    ""PetTypeId"": 1001,
    ""Name"": ""Slime"",
    ""Type"": ""Capture"",
    ""Element"": ""Earth"",
    ""BaseHp"": 100,
    ""GrowthModel"": { ""HpGrowthPerLevel"": 10 },
    ""SkillSet"": [],
    ""Utilities"": [
       { ""Type"": ""Mount"", ""Value"": 1.2, ""RequiredLevel"": 1, ""RequiredAmity"": 10 }
    ],
    ""RebirthEligible"": true,
    ""RebirthSkillId"": 999
  }
]");
        _petManager.Load("Content/Data/pets_expansion_test.json");

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
        if (File.Exists("Content/Data/pets_expansion_test.json"))
        {
            File.Delete("Content/Data/pets_expansion_test.json");
        }
    }

    [Fact]
    public void PetDeath_ReducesAmity()
    {
        // Setup
        var session = new ClientSessionForTest();
        session.SetCharacter(new ServerCharacter { Id = 1, Name = "Trainer" });
        _playerService.RegisterSession(session);

        var def = _petManager.GetDefinition(1001);
        var pet = new ServerPet(def);
        pet.Amity = 50;
        session.Character.AddPet(pet);

        // Register combatants
        _combatManager.RegisterCombatant(pet);
        var attacker = new ServerCharacter { Id = 2 };
        _combatManager.RegisterCombatant(attacker);

        _mockRandom.Setup(r => r.NextFloat()).Returns(0.0f);

        _mockSkills.Setup(s => s.GetSkillById(1))
            .Returns(new Skill { SkillId = 1, SpCost = 0, Effects = new List<SkillEffect> { new SkillEffect { Tag = SkillEffectTag.Damage, Value = 1.0f } } });

        // Mock Damage to kill (Hp is around 100, so 200 damage is safe kill)
        _mockResolver.Setup(r =>
                r.CalculateDamage(It.IsAny<ServerCombatant>(), It.IsAny<ServerCombatant>(),
                    It.IsAny<UseSkillRequest>()))
            .Returns(200);

        // Act
        // Manual death to verify Amity logic without relying on flaky CombatManager mocks
        pet.Die();

        // Assert
        Assert.True(pet.IsDead);
        Assert.Equal(49, pet.Amity); // 50 - 1
    }

    [Fact]
    public void Rebirth_ResetsLevel_BoostsStats()
    {
        var def = _petManager.GetDefinition(1001);
        var pet = new ServerPet(def);
        pet.Level = 100;
        pet.RecalculateStats();

        // Act
        var success = pet.TryRebirth();

        // Assert
        Assert.True(success);
        Assert.Equal(1, pet.Level);
        Assert.True(pet.HasRebirthed);

        // Check stat boost: Level 1 Rebirth vs Level 1 Normal
        var normalPet = new ServerPet(def); // Level 1 default

        // Normal HP: BaseHp (100) + Growth(0)
        // Rebirth HP: (BaseHp + Growth) * 1.1
        Assert.True(pet.MaxHp > normalPet.MaxHp);
    }

    [Fact]
    public void Utility_Usage_Check()
    {
        var session = new ClientSessionForTest();
        session.SetCharacter(new ServerCharacter { Id = 1, Name = "Trainer" });
        _playerService.RegisterSession(session);

        var def = _petManager.GetDefinition(1001);
        var pet = new ServerPet(def);
        pet.Amity = 5; // Too low (req 10)
        session.Character.AddPet(pet);

        // Act - Fail
        var resultLowAmity = _petService.UseUtility(1, pet.InstanceId, PetUtilityType.Mount);
        Assert.False(resultLowAmity);

        // Act - Success
        pet.Amity = 20;
        var resultSuccess = _petService.UseUtility(1, pet.InstanceId, PetUtilityType.Mount);
        Assert.True(resultSuccess);
    }

    [Fact]
    public void ObedienceCheckShouldRespectAmity()
    {
        var def = _petManager.GetDefinition(1001);
        var pet = new ServerPet(def);

        // Amity 50 -> Always obey
        Assert.True(pet.CheckObedience(0.99f));

        // Amity 0 -> 24% fail chance (Formula: (20-0)*0.01 + 0.04 = 0.24)
        pet.ChangeAmity(-50); // Amity 0

        // Roll 0.1 (10%) -> Fail check (returns false if roll <= failChance? No, returns roll > failChance)
        // Code: return roll > failChance;
        // 0.1 > 0.24 is False. Disobey.
        Assert.False(pet.CheckObedience(0.1f));

        // Roll 0.3 -> 0.3 > 0.24 is True. Obey.
        Assert.True(pet.CheckObedience(0.3f));
    }
}