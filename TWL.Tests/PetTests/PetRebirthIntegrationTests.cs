<<<<<<< HEAD
using Xunit;
using Moq;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Services;
using TWL.Server.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Services;

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
                Type = PetType.Quest, 
                IsQuestPet = true,
                RebirthEligible = true,
                Element = Element.Fire,
                GrowthModel = new PetGrowthModel { HpGrowthPerLevel = 10 }
            },
            new() { 
                PetTypeId = 2, 
                Name = "CapturePet", 
                Type = PetType.Capture, 
                RebirthEligible = true,
                Element = Element.Water,
                GrowthModel = new PetGrowthModel { HpGrowthPerLevel = 10 }
            },
            new() { 
                PetTypeId = 1001, 
                Name = "QuestPet1001", 
                Type = PetType.Quest, 
                Element = Element.Earth,
                IsQuestPet = true, 
                RebirthEligible = true,
                BaseHp = 100,
                BaseInt = 10,
                GrowthModel = new PetGrowthModel { CurveType = GrowthCurveType.Standard }
            }
        });

        _mockRepo = new Mock<IPlayerRepository>();
        var metrics = new ServerMetrics();
        _playerService = new PlayerService(_mockRepo.Object, metrics);
        
        var mockResolver = new Mock<ICombatResolver>().Object;
        var mockSkills = new Mock<ISkillCatalog>().Object;
        var mockStatus = new Mock<IStatusEngine>().Object;
        var random = new Mock<IRandomService>().Object;

        var combatManager = new CombatManager(mockResolver, random, mockSkills, mockStatus);
        var monsterManager = new Mock<MonsterManager>().Object;
        var logger = new Mock<ILogger<PetService>>().Object;

        _petService = new PetService(_playerService, _petManager, monsterManager, combatManager, random, logger);
    }

    [Fact]
    public void TryRebirth_Success_ForEligibleQuestPet()
    {
        var session = new ClientSessionForTest();
        var character = new ServerCharacter { Id = 1, Name = "PetMaster" };
        session.SetCharacter(character);
        _playerService.RegisterSession(session);

        var def = _petManager.GetDefinition(1);
        var pet = new ServerPet(def);
        pet.SetLevel(100);
        character.AddPet(pet);

        var result = _petService.TryRebirth(1, pet.InstanceId);

        Assert.True(result, "Rebirth should succeed for quest pet at level 100");
        Assert.Equal(1, pet.Level);
        Assert.Equal(1, pet.RebirthGeneration);
        Assert.True(pet.HasRebirthed);
    }

    [Fact]
    public void TryRebirth_Fails_ForCapturePet()
    {
        var session = new ClientSessionForTest();
        var character = new ServerCharacter { Id = 1 };
        session.SetCharacter(character);
        _playerService.RegisterSession(session);

        var def = _petManager.GetDefinition(2);
        var pet = new ServerPet(def);
        pet.SetLevel(100);
        character.AddPet(pet);

        var result = _petService.TryRebirth(1, pet.InstanceId);

        Assert.False(result, "Rebirth should be denied for Capture pets regardless of eligibility flag");
        Assert.Equal(100, pet.Level);
        Assert.False(pet.HasRebirthed);
    }

    [Fact]
    public void PetRebirth_State_PersistsThroughSaveLoad()
    {
        var def = _petManager.GetDefinition(1001);
        var pet = new ServerPet(def);
        pet.SetLevel(100);
        
        bool success = pet.TryRebirth();
        Assert.True(success);
        Assert.Equal(1, pet.RebirthGeneration);
        Assert.Equal(1, pet.Level);

        var saveData = pet.GetSaveData();
        Assert.Equal(1, saveData.RebirthGeneration);

        var newPet = new ServerPet();
        newPet.LoadSaveData(saveData);
        newPet.Hydrate(def);

        Assert.Equal(1, newPet.RebirthGeneration);
        Assert.Equal(1, newPet.Level);
        
=======
using System;
using System.Collections.Generic;
using System.Linq;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using Xunit;

namespace TWL.Tests.PetTests;

public class PetRebirthIntegrationTests
{
    [Fact]
    public void PetRebirth_State_PersistsThroughSaveLoad()
    {
        // Arrange
        var def = new PetDefinition
        {
            PetTypeId = 1001,
            Name = "QuestPet",
            Element = Element.Fire,
            IsQuestPet = true,
            RebirthEligible = true,
            BaseHp = 100,
            BaseInt = 10,
            GrowthModel = new PetGrowthModel { CurveType = GrowthCurveType.Standard }
        };

        var pet = new ServerPet(def);
        pet.SetLevel(100);
        
        // Act - Rebirth
        bool success = pet.TryRebirth();
        Assert.True(success);
        Assert.Equal(1, pet.RebirthCount);
        Assert.Equal(1, pet.Level);

        // Save
        var saveData = pet.GetSaveData();
        Assert.Equal(1, saveData.RebirthCount);

        // Load into new instance
        var newPet = new ServerPet();
        newPet.LoadSaveData(saveData);
        newPet.Hydrate(def); // Need to re-hydrate to apply stats correctly

        // Assert
        Assert.Equal(1, newPet.RebirthCount);
        Assert.Equal(1, newPet.Level);
        
        // Verify stats are calculated with rebirth bonus
        // Level 1 stats with 10% bonus
>>>>>>> gsd/M001/S06
        PetGrowthCalculator.CalculateStats(def, 1, out var expectedHp, out var _, out var _, out var _, out var _, out var _, out var _);
        int expectedHpWithBonus = (int)(expectedHp * 1.10);
        Assert.Equal(expectedHpWithBonus, newPet.MaxHp);
    }

    [Fact]
    public void Character_SaveLoad_IncludesPetRebirthState()
    {
<<<<<<< HEAD
        var character = new ServerCharacter { Id = 1, Name = "PetOwner" };
        var def = _petManager.GetDefinition(1001);
        var pet = new ServerPet(def);
        pet.SetLevel(100);
        pet.TryRebirth(); // Generation 1
        pet.SetLevel(100);
        pet.TryRebirth(); // Generation 2
        character.AddPet(pet);

        var charSave = character.GetSaveData();
        var petSave = charSave.Pets.Single(p => p.DefinitionId == 1001);
        Assert.Equal(2, petSave.RebirthGeneration);

        var newCharacter = new ServerCharacter();
        newCharacter.LoadSaveData(charSave);
        
        var loadedPet = newCharacter.Pets.Single(p => p.DefinitionId == 1001);
        Assert.Equal(2, loadedPet.RebirthGeneration);
=======
        // Arrange
        var character = new ServerCharacter { Id = 1, Name = "PetOwner" };
        var def = new PetDefinition { PetTypeId = 1001, Name = "QuestPet", Element = Element.Wind, IsQuestPet = true };
        var pet = new ServerPet(def);
        pet.RebirthCount = 2;
        character.AddPet(pet);

        // Save Character
        var charSave = character.GetSaveData();
        var petSave = charSave.Pets.Single(p => p.DefinitionId == 1001);
        Assert.Equal(2, petSave.RebirthCount);

        // Load Character
        var newCharacter = new ServerCharacter();
        newCharacter.LoadSaveData(charSave);
        
        // Assert
        var loadedPet = newCharacter.Pets.Single(p => p.DefinitionId == 1001);
        Assert.Equal(2, loadedPet.RebirthCount);
>>>>>>> gsd/M001/S06
    }
}
