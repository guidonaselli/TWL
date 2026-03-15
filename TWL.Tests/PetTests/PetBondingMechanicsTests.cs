using Moq;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Services;
using TWL.Server.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Services;
using Xunit;
using Microsoft.Extensions.Logging;

namespace TWL.Tests.PetTests;

public class PetBondingMechanicsTests
{
    private readonly PetManager _petManager;
    private readonly PlayerService _playerService;

    public PetBondingMechanicsTests()
    {
        _playerService = new PlayerService(new Mock<IPlayerRepository>().Object, new ServerMetrics());
        _petManager = new PetManager();
        
        Directory.CreateDirectory("Content/Data");
        File.WriteAllText("Content/Data/pets_bonding_test.json", @"
[
  {
    ""PetTypeId"": 1,
    ""Name"": ""Bond Pet"",
    ""Type"": ""Quest"",
    ""IsQuestPet"": true,
    ""Element"": ""Fire"",
    ""BaseHp"": 100,
    ""BaseStr"": 100,
    ""BaseCon"": 100,
    ""BaseInt"": 100,
    ""BaseWis"": 100,
    ""BaseAgi"": 100,
    ""GrowthModel"": { ""HpGrowthPerLevel"": 10, ""SpGrowthPerLevel"": 5 },
    ""BondTiers"": [
      { ""AmityThreshold"": 0, ""StatMultiplier"": 0.7, ""Name"": ""Hated"" },
      { ""AmityThreshold"": 20, ""StatMultiplier"": 1.0, ""Name"": ""Neutral"" },
      { ""AmityThreshold"": 60, ""StatMultiplier"": 1.05, ""Name"": ""Friendly"" },
      { ""AmityThreshold"": 90, ""StatMultiplier"": 1.15, ""Name"": ""Soulbound"" }
    ]
  }
]");
        _petManager.Load("Content/Data/pets_bonding_test.json");
    }

    [Theory]
    [InlineData(10, 70)]   // 0.7x of 100
    [InlineData(50, 100)]  // 1.0x of 100
    [InlineData(70, 105)]  // 1.05x of 100
    [InlineData(95, 115)]  // 1.15x of 100
    public void BondingTiers_ApplyCorrectMultipliers(int amity, int expectedStr)
    {
        // Setup
        var def = _petManager.GetDefinition(1);
        var pet = new ServerPet(def);
        
        // Act
        pet.Amity = amity;
        pet.RecalculateStats();

        // Assert
        Assert.Equal(expectedStr, pet.Str);
    }

    [Fact]
    public void BondingTiers_FallbackToLegacy_IfEmpty()
    {
        // Setup definition without BondTiers
        var def = new PetDefinition
        {
            PetTypeId = 2,
            Name = "Legacy Pet",
            Element = Element.Fire,
            BaseStr = 10,
            BaseCon = 10,
            BaseInt = 10,
            BaseWis = 10,
            BaseAgi = 10
        };

        var pet = new ServerPet(def);

        // Rebellious (<20) -> 0.8x
        pet.Amity = 10;
        pet.RecalculateStats();
        Assert.Equal(8, pet.Str);

        // Normal -> 1.0x
        pet.Amity = 50;
        pet.RecalculateStats();
        Assert.Equal(10, pet.Str);

        // High Amity (>=90) -> 1.1x
        pet.Amity = 95;
        pet.RecalculateStats();
        Assert.Equal(11, pet.Str);
    }
}
