using Moq;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Models;
using Microsoft.Extensions.Logging;
using Xunit;
using TWL.Server.Simulation.Networking.Components;
<<<<<<< HEAD
using System.Linq;
using System;
=======
>>>>>>> gsd/M001/S06

namespace TWL.Tests.Rebirth;

public class CharacterRebirthRequirementTests
{
    private readonly Mock<ILogger<RebirthManager>> _loggerMock;
    private readonly RebirthManager _rebirthManager;
<<<<<<< HEAD
    private readonly ServerQuestManager _questManager;
=======
>>>>>>> gsd/M001/S06

    public CharacterRebirthRequirementTests()
    {
        _loggerMock = new Mock<ILogger<RebirthManager>>();
        _rebirthManager = new RebirthManager(_loggerMock.Object);
<<<<<<< HEAD
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
=======
>>>>>>> gsd/M001/S06
    }

    [Fact]
    public void TryRebirthCharacter_Fails_WhenLevelBelow100()
    {
        // Arrange
<<<<<<< HEAD
        var character = CreateTestCharacter(99);
=======
        var character = new ServerCharacter { Level = 99 };
>>>>>>> gsd/M001/S06
        
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
<<<<<<< HEAD
        var character = CreateTestCharacter(100);
=======
        var character = new ServerCharacter { Level = 100 };
        // We need to inject Quest prerequisites into RebirthManager.
        // For this test, let's assume quest 5000 is required.
>>>>>>> gsd/M001/S06
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
<<<<<<< HEAD
    public void TryRebirthCharacter_Fails_WhenItemMissing_DataDriven()
    {
        // Arrange
        var character = CreateTestCharacter(100);
=======
    public void TryRebirthCharacter_Fails_WhenItemMissing()
    {
        // Arrange
        var character = new ServerCharacter { Level = 100 };
>>>>>>> gsd/M001/S06
        _rebirthManager.SetRequirements(new RebirthRequirements { RequiredItemId = 9999, RequiredItemQuantity = 1 });
        
        // Act
        var result = _rebirthManager.TryRebirthCharacter(character, null, "op3");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("item", result.Message, StringComparison.OrdinalIgnoreCase);
    }
<<<<<<< HEAD
=======

    [Fact]
    public void TryRebirthCharacter_RecordsFailureInHistory_WhenLevelTooLow()
    {
        // Arrange
        var character = new ServerCharacter { Level = 99 };
        
        // Act
        _rebirthManager.TryRebirthCharacter(character, "op_fail_level");

        // Assert
        Assert.Single(character.RebirthHistory);
        var record = character.RebirthHistory[0];
        Assert.False(record.Success);
        Assert.Contains("level", record.Reason, StringComparison.OrdinalIgnoreCase);
    }
>>>>>>> gsd/M001/S06
}
