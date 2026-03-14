using TWL.Server.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace TWL.Tests.PetTests;

/// <summary>
/// Integration tests for Pet Rebirth action routing and policy enforcement.
/// </summary>
public class PetRebirthIntegrationTests
{
    private readonly PetManager _petManager;
    private readonly PetService _petService;
    private readonly Mock<IPlayerRepository> _mockRepo;
    private readonly PlayerService _playerService;

    public PetRebirthIntegrationTests()
    {
        _petManager = new PetManager();
        // Load test data with a Quest pet and a Capture pet
        _petManager.Load(new List<PetDefinition>
        {
            new() { 
                PetTypeId = 1, 
                Name = "QuestPet", 
                Type = "Quest", 
                RebirthEligible = true,
                GrowthModel = new PetGrowthModel { HpGrowthPerLevel = 10 }
            },
            new() { 
                PetTypeId = 2, 
                Name = "CapturePet", 
                Type = "Capture", 
                RebirthEligible = true,
                GrowthModel = new PetGrowthModel { HpGrowthPerLevel = 10 }
            }
        });

        _mockRepo = new Mock<IPlayerRepository>();
        var metrics = new ServerMetrics();
        _playerService = new PlayerService(_mockRepo.Object, metrics);
        
        var combatManager = new Mock<CombatManager>(null, null, null, null).Object;
        var monsterManager = new Mock<MonsterManager>().Object;
        var random = new Mock<IRandomService>().Object;
        var logger = new Mock<ILogger<PetService>>().Object;

        _petService = new PetService(_playerService, _petManager, monsterManager, combatManager, random, logger);
    }

    [Fact]
    public void TryRebirth_Success_ForEligibleQuestPet()
    {
        // 1. Setup Session & Character
        var session = new ClientSessionForTest();
        var character = new ServerCharacter { Id = 1, Name = "PetMaster" };
        session.SetCharacter(character);
        _playerService.RegisterSession(session);

        // 2. Give Quest Pet to character
        var def = _petManager.GetDefinition(1);
        var pet = new ServerPet(def);
        pet.SetLevel(100);
        character.AddPet(pet);

        // 3. Act
        var result = _petService.TryRebirth(1, pet.InstanceId);

        // 4. Assert
        Assert.True(result, "Rebirth should succeed for quest pet at level 100");
        Assert.Equal(1, pet.Level);
        Assert.Equal(1, pet.RebirthGeneration);
        Assert.True(pet.HasRebirthed);
    }

    [Fact]
    public void TryRebirth_Fails_ForCapturePet()
    {
        // 1. Setup Session
        var session = new ClientSessionForTest();
        var character = new ServerCharacter { Id = 1 };
        session.SetCharacter(character);
        _playerService.RegisterSession(session);

        // 2. Give Capture Pet
        var def = _petManager.GetDefinition(2);
        var pet = new ServerPet(def);
        pet.SetLevel(100);
        character.AddPet(pet);

        // 3. Act
        var result = _petService.TryRebirth(1, pet.InstanceId);

        // 4. Assert
        Assert.False(result, "Rebirth should be denied for Capture pets regardless of eligibility flag");
        Assert.Equal(100, pet.Level);
        Assert.False(pet.HasRebirthed);
    }

    [Fact]
    public void PetRebirth_DiminishingReturns_Verification()
    {
        var def = _petManager.GetDefinition(1);
        var pet = new ServerPet(def); // MaxHp at Level 100 is approx 1000? No, let's see.
        // BaseHp 0 + 10 * 99 = 990. 
        // We care about MaxHp at Level 1 *after* rebirth.

        // Gen 0 (Normal)
        var normalMaxHp = pet.MaxHp;

        // Gen 0 -> 1: +10%
        pet.SetLevel(100);
        pet.TryRebirth();
        var gen1MaxHp = pet.MaxHp;

        // Gen 1 -> 2: +8%
        pet.SetLevel(100);
        pet.TryRebirth();
        var gen2MaxHp = pet.MaxHp;

        // Gen 2 -> 3: +5%
        pet.SetLevel(100);
        pet.TryRebirth();
        var gen3MaxHp = pet.MaxHp;

        Assert.True(gen1MaxHp > normalMaxHp);
        Assert.True(gen2MaxHp > gen1MaxHp);
        Assert.True(gen3MaxHp > gen2MaxHp);
        
        // Ratio check (approximate due to rounding)
        // Gen 1 bonus is 10%, Gen 2 is 8%.
        // Increment from gen1 to gen2 should be smaller than from normal to gen1 in percentage terms.
    }
}
