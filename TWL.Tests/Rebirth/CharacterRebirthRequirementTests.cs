using Microsoft.Extensions.Logging;
using Moq;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Models;
using Xunit;

namespace TWL.Tests.Rebirth;

public class CharacterRebirthRequirementTests
{
    private readonly RebirthManager _rebirthManager;
    private readonly Mock<ILogger<RebirthManager>> _mockLogger;

    public CharacterRebirthRequirementTests()
    {
        _mockLogger = new Mock<ILogger<RebirthManager>>();
        _rebirthManager = new RebirthManager(_mockLogger.Object);
    }

    [Fact]
    public void TryRebirthCharacter_WithMissingOptionalItem_Fails()
    {
        // Arrange
        var character = new ServerCharacter
        {
            Id = 1,
            Level = 100
        };

        var requiredItemId = 9999;

        // Act
        var (success, message, _) = _rebirthManager.TryRebirthCharacter(character, "op-1", requiredItemId: requiredItemId);

        // Assert
        Assert.False(success);
        Assert.Equal("Required item not found.", message);
    }

    [Fact]
    public void TryRebirthCharacter_WithSatisfiedOptionalItem_Succeeds()
    {
        // Arrange
        var character = new ServerCharacter
        {
            Id = 1,
            Level = 100
        };

        var requiredItemId = 9999;
        character.AddItem(requiredItemId, 1);

        // Pre-condition
        Assert.True(character.HasItem(requiredItemId, 1));

        // Act
        var (success, _, statPointsGained) = _rebirthManager.TryRebirthCharacter(character, "op-2", requiredItemId: requiredItemId);

        // Assert
        Assert.True(success);
        Assert.True(statPointsGained > 0);
        Assert.False(character.HasItem(requiredItemId, 1)); // Item was consumed
    }

    [Fact]
    public void TryRebirthCharacter_RetainsSkillsAndEquipment()
    {
        // Arrange
        var character = new ServerCharacter
        {
            Id = 1,
            Level = 100
        };

        // Add Skill
        character.LearnSkill(100);

        // Add Equipment
        character.AddItem(200, 1);
        character.Equip(0); // Equip from inventory slot 0

        var initialSkillCount = character.KnownSkills.Count;
        var initialEquipCount = character.Equipment.Count;

        Assert.True(initialSkillCount > 0);
        Assert.True(initialEquipCount > 0);

        // Act
        var (success, _, _) = _rebirthManager.TryRebirthCharacter(character, "op-3");

        // Assert
        Assert.True(success);
        Assert.Equal(initialSkillCount, character.KnownSkills.Count);
        Assert.Equal(initialEquipCount, character.Equipment.Count);
        Assert.Contains(100, character.KnownSkills);
        Assert.Contains(character.Equipment, e => e.ItemId == 200);
    }
}