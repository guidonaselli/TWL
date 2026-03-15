<<<<<<< HEAD
using Xunit;
using Moq;
using System.IO;
using System.Collections.Generic;
=======
using Moq;
>>>>>>> gsd/M001/S06
using TWL.Server.Persistence;
using TWL.Server.Persistence.Services;
using TWL.Server.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Services;
<<<<<<< HEAD

namespace TWL.Tests.PetTests;

/// <summary>
/// Validates quest-vs-capturable differentiation and 10/8/5 diminishing bonus schedule for pet rebirth.
/// Covers requirements PET-03 and PET-04.
/// </summary>
=======
using Xunit;

namespace TWL.Tests.PetTests;

>>>>>>> gsd/M001/S06
public class PetRebirthPolicyTests : IDisposable
{
    private readonly CombatManager _combatManager;
    private readonly ServerMetrics _metrics;
    private readonly Mock<IRandomService> _mockRandom;
    private readonly Mock<IPlayerRepository> _mockRepo;
    private readonly PetManager _petManager;
    private readonly PetService _petService;
    private readonly PlayerService _playerService;
    private readonly Mock<MonsterManager> _mockMonsterManager;

    public PetRebirthPolicyTests()
    {
        _mockRepo = new Mock<IPlayerRepository>();
        _metrics = new ServerMetrics();
        _playerService = new PlayerService(_mockRepo.Object, _metrics);
        _petManager = new PetManager();

        Directory.CreateDirectory("Content/Data");
        File.WriteAllText("Content/Data/pets_policy_test.json", @"
[
  {
    ""PetTypeId"": 1001,
    ""Name"": ""Quest Slime"",
    ""IsQuestPet"": true,
    ""RebirthEligible"": true,
    ""BaseHp"": 100,
<<<<<<< HEAD
    ""Element"": ""Earth"",
    ""Type"": ""Quest""
=======
    ""Element"": ""Earth""
>>>>>>> gsd/M001/S06
  },
  {
    ""PetTypeId"": 1002,
    ""Name"": ""Wild Wolf"",
    ""IsQuestPet"": false,
    ""RebirthEligible"": true,
    ""BaseHp"": 100,
<<<<<<< HEAD
    ""Element"": ""Wind"",
    ""Type"": ""Capture""
=======
    ""Element"": ""Wind""
>>>>>>> gsd/M001/S06
  }
]");
        _petManager.Load("Content/Data/pets_policy_test.json");

        _mockRandom = new Mock<IRandomService>();
        _mockMonsterManager = new Mock<MonsterManager>();
        
        var mockResolver = new Mock<ICombatResolver>();
        var mockSkills = new Mock<ISkillCatalog>();
        var mockStatus = new Mock<IStatusEngine>();
        
        _combatManager = new CombatManager(mockResolver.Object, _mockRandom.Object, mockSkills.Object, mockStatus.Object);
        _petService = new PetService(_playerService, _petManager, _mockMonsterManager.Object, _combatManager, _mockRandom.Object, new Mock<Microsoft.Extensions.Logging.ILogger<PetService>>().Object);
    }

    public void Dispose()
    {
        if (File.Exists("Content/Data/pets_policy_test.json"))
            File.Delete("Content/Data/pets_policy_test.json");
    }

<<<<<<< HEAD
    private static PetDefinition MakeQuestPetDef(int petTypeId = 1001) => new()
    {
        PetTypeId = petTypeId,
        Name = "Quest Pet",
        Type = PetType.Quest,
        IsQuestPet = true,
        RebirthEligible = true,
        Element = Element.Earth,
        BaseHp = 200,
        BaseStr = 10, BaseCon = 10, BaseInt = 10, BaseWis = 10, BaseAgi = 10,
        GrowthModel = new PetGrowthModel { HpGrowthPerLevel = 5, StrWeight = 20, ConWeight = 20, IntWeight = 20, WisWeight = 20, AgiWeight = 20 }
    };

    private static PetDefinition MakeCapturePetDef(int petTypeId = 2001) => new()
    {
        PetTypeId = petTypeId,
        Name = "Capturable Pet",
        Type = PetType.Capture,
        RebirthEligible = false,
        Element = Element.Water,
        BaseHp = 150,
        BaseStr = 8, BaseCon = 8, BaseInt = 8, BaseWis = 8, BaseAgi = 8,
        CaptureRules = new CaptureRules { IsCapturable = true, BaseChance = 0.5f },
        GrowthModel = new PetGrowthModel { HpGrowthPerLevel = 3, StrWeight = 20, ConWeight = 20, IntWeight = 20, WisWeight = 20, AgiWeight = 20 }
    };

    private static ServerPet MakeQuestPet(int level = 100)
    {
        var pet = new ServerPet(MakeQuestPetDef());
        pet.SetLevel(level);
        return pet;
    }

    private static ServerPet MakeCapturePet(int level = 100)
    {
        var pet = new ServerPet(MakeCapturePetDef());
        pet.SetLevel(level);
        return pet;
    }

    // ─── Eligibility Policy ─────────────────────────────────────────────────────

    [Fact]
    public void TryRebirth_QuestPet_AtLevel100_Succeeds()
    {
        var pet = MakeQuestPet(100);
        var result = pet.TryRebirth();

        Assert.True(result);
        Assert.Equal(1, pet.RebirthGeneration);
        Assert.True(pet.HasRebirthed);
        Assert.Equal(1, pet.Level);
    }

    [Fact]
    public void TryRebirth_CapturePet_IsRejected()
    {
        var pet = MakeCapturePet(100);
        var result = pet.TryRebirth();

        Assert.False(result, "Capturable pets must be blocked from rebirth.");
        Assert.Equal(0, pet.RebirthGeneration);
        Assert.Equal(100, pet.Level);
    }

    // ─── Diminishing Bonus Schedule ─────────────────────────────────────────────

    [Theory]
    [InlineData(1, 10)]
    [InlineData(2, 8)]
    [InlineData(3, 5)]
    [InlineData(4, 5)]
    public void GetRebirthBonusPoints_MatchesSchedule(int generation, int expectedBonus)
    {
        var bonus = ServerPet.GetRebirthBonusPoints(generation);
        Assert.Equal(expectedBonus, bonus);
    }

    [Theory]
    [InlineData(1, 1.10f)]
    [InlineData(2, 1.18f)]
    [InlineData(3, 1.23f)]
    [InlineData(4, 1.28f)]
    public void GetCumulativeStatMultiplier_IsCorrect(int generations, float expectedMultiplier)
    {
        var multiplier = ServerPet.GetCumulativeStatMultiplier(generations);
        Assert.Equal(expectedMultiplier, multiplier, precision: 2);
    }

    [Fact]
    public void RecalculateStats_AfterFirstRebirth_AppliesTenPercentBonus()
    {
        var pet = MakeQuestPet(100);
        var baselineStr = MakeQuestPet(1).Str;
        
        pet.TryRebirth();
        var expectedStr = (int)(baselineStr * 1.10f);

        Assert.Equal(expectedStr, pet.Str);
=======
    [Fact]
    public void QuestPet_CanRebirth()
    {
        var def = _petManager.GetDefinition(1001);
        var pet = new ServerPet(def);
        pet.SetLevel(100);

        var success = pet.TryRebirth();
        Assert.True(success);
        Assert.Equal(1, pet.RebirthCount);
    }

    [Fact]
    public void CapturablePet_CannotRebirth()
    {
        var def = _petManager.GetDefinition(1002);
        var pet = new ServerPet(def);
        pet.SetLevel(100);

        var success = pet.TryRebirth();
        Assert.False(success);
        Assert.Equal(0, pet.RebirthCount);
    }

    [Fact]
    public void DiminishingReturns_10_8_5_Schedule()
    {
        var def = _petManager.GetDefinition(1001);
        var pet = new ServerPet(def);
        int baseHp = pet.MaxHp;

        // 1st Rebirth: +10%
        pet.SetLevel(100);
        pet.TryRebirth();
        Assert.Equal((int)(baseHp * 1.10), pet.MaxHp);

        // 2nd Rebirth: +18% total (10+8)
        pet.SetLevel(100);
        pet.TryRebirth();
        Assert.Equal((int)(baseHp * 1.18), pet.MaxHp);

        // 3rd Rebirth: +23% total (10+8+5)
        pet.SetLevel(100);
        pet.TryRebirth();
        Assert.Equal((int)(baseHp * 1.23), pet.MaxHp);
        
        // 4th Rebirth: Should fail (limit 3)
        pet.SetLevel(100);
        Assert.False(pet.TryRebirth());
>>>>>>> gsd/M001/S06
    }
}
