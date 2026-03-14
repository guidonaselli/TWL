using Moq;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Models;
using Microsoft.Extensions.Logging;
using Xunit;
using TWL.Server.Simulation.Networking.Components;
using System.Linq;
using System;

namespace TWL.Tests.Rebirth;

public class CharacterRebirthRequirementTests
{
    private readonly Mock<ILogger<RebirthManager>> _loggerMock;
    private readonly RebirthManager _rebirthManager;
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
    public void TryRebirthCharacter_Fails_WhenLevelBelow100()
    {
        // Arrange
        var character = CreateTestCharacter(99);
        
        // Act
        var result = _rebirthManager.TryRebirthCharacter(character, "op1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Level 100 required", result.Message);
    }

    [Fact]
    public void TryRebirthCharacter_Fails_WhenQuestNotCompleted()
    {
        // Arrange
        var character = CreateTestCharacter(100);
        _rebirthManager.SetRequirements(new RebirthRequirements { RequiredQuestId = 5000 });

        var questComponent = new PlayerQuestComponent(new ServerQuestManager());
        // Quest 5000 not started

        // Act
        var result = _rebirthManager.TryRebirthCharacter(character, questComponent, "op2");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("quest", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryRebirthCharacter_Fails_WhenItemMissing_DataDriven()
    {
        // Arrange
        var character = CreateTestCharacter(100);
        _rebirthManager.SetRequirements(new RebirthRequirements { RequiredItemId = 9999, RequiredItemQuantity = 1 });
        
        // Act
        var result = _rebirthManager.TryRebirthCharacter(character, null, "op3");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("item", result.Message, StringComparison.OrdinalIgnoreCase);
    }
}
