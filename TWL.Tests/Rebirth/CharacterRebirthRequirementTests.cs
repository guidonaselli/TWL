using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Models;
using TWL.Server.Simulation.Networking.Components;

namespace TWL.Tests.Rebirth;

public class CharacterRebirthRequirementTests
{
    private readonly RebirthManager _rebirthManager;
    private readonly Mock<ILogger<RebirthManager>> _loggerMock;

    private readonly ServerQuestManager _questManager;

    public CharacterRebirthRequirementTests()
    {
        _loggerMock = new Mock<ILogger<RebirthManager>>();
        _rebirthManager = new RebirthManager(_loggerMock.Object);
        _questManager = new ServerQuestManager();
    }

    private ServerCharacter CreateTestCharacter(int level = 100)
    {
        var character = new ServerCharacter { Id = 1, Name = "Test", Level = level };
        character.QuestComponent = new PlayerQuestComponent(_questManager);
        character.QuestComponent.Character = character;
        return character;
    }

    [Fact]
    public void TryRebirthCharacter_RejectsMissingQuestFlag()
    {
        // Arrange
        var character = CreateTestCharacter(100);
        // Add item but NO quest flag
        character.AddItem(9007, 1);
        
        // Act
        var (success, msg, points) = _rebirthManager.TryRebirthCharacter(character, "op123");
 
        // Assert
        Assert.False(success);
        Assert.Contains("quest", msg.ToLower());
        Assert.Equal(100, character.Level);
    }

    [Fact]
    public void TryRebirthCharacter_RejectsMissingItem()
    {
        // Arrange
        var character = CreateTestCharacter(100);
        character.QuestComponent.AddFlag("REBIRTH_QUALIFIED");
        // NO item 9007
        
        // Act
        var (success, msg, points) = _rebirthManager.TryRebirthCharacter(character, "op123");
 
        // Assert
        Assert.False(success);
        Assert.Contains("shard", msg.ToLower());
        Assert.Equal(100, character.Level);
    }

    [Fact]
    public void TryRebirthCharacter_ConsumesItemOnSuccess()
    {
        // Arrange
        var character = CreateTestCharacter(100);
        character.QuestComponent.AddFlag("REBIRTH_QUALIFIED");
        character.AddItem(9007, 1);
        
        // Act
        var (success, msg, points) = _rebirthManager.TryRebirthCharacter(character, "op123");
 
        // Assert
        Assert.True(success);
        Assert.Equal(1, character.Level);
        Assert.Empty(character.Inventory.Where(i => i.ItemId == 9007));
    }

    [Fact]
    public void TryRebirthCharacter_PreservesSkillsAndEquipment()
    {
        // Arrange
        var character = CreateTestCharacter(100);
        character.QuestComponent.AddFlag("REBIRTH_QUALIFIED");
        
        // Mock skills
        character.LearnSkill(1001);
        
        // Mock equipment (Item 5001 - "Dragon Blade")
        character.AddItem(5001, 1); // Add to inventory first
        character.Equip(character.Inventory.Count - 1); // Equip the last added item
        
        character.AddItem(9007, 1); // Add rebirth item AFTER equipping to avoid slot confusion
        
        // Act
        var (success, msg, points) = _rebirthManager.TryRebirthCharacter(character, "op123");
 
        // Assert
        Assert.True(success);
        Assert.Contains(1001, character.KnownSkills);
        Assert.True(character.HasEquippedItem(5001));
        
        // Stats are reset to level 1 baseline
        Assert.Equal(1, character.Level);
    }
}
