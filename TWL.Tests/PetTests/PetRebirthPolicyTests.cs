using Xunit;
using Moq;
using System.IO;
using System.Collections.Generic;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Services;
using TWL.Server.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Services;

namespace TWL.Tests.PetTests;

/// <summary>
/// Validates quest-vs-capturable differentiation and 10/8/5 diminishing bonus schedule for pet rebirth.
/// Covers requirements PET-03 and PET-04.
/// </summary>
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
    ""Element"": ""Earth"",
    ""Type"": ""Quest""
  },
  {
    ""PetTypeId"": 1002,
    ""Name"": ""Wild Wolf"",
    ""IsQuestPet"": false,
    ""RebirthEligible"": true,
    ""BaseHp"": 100,
    ""Element"": ""Wind"",
    ""Type"": ""Capture""
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
    }
}
