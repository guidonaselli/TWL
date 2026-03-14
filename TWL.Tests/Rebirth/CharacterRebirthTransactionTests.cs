using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.DTO;
using TWL.Server.Simulation.Networking.Components;

namespace TWL.Tests.Rebirth;

public class CharacterRebirthTransactionTests
{
    private readonly RebirthManager _rebirthManager;
    private readonly Mock<ILogger<RebirthManager>> _loggerMock;

    private readonly ServerQuestManager _questManager;

    public CharacterRebirthTransactionTests()
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
    public void TryRebirthCharacter_RejectsLevelBelow100()
    {
        // Arrange
        var character = CreateTestCharacter(99);
        string opId = Guid.NewGuid().ToString();

        // Act
        var (success, msg, points) = _rebirthManager.TryRebirthCharacter(character, opId);

        // Assert
        Assert.False(success);
        Assert.Equal(0, points);
        Assert.Equal(99, character.Level);

        // Assert no history is recorded to prevent DoS
        Assert.Empty(character.RebirthHistory);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 15)]
    [InlineData(2, 10)]
    [InlineData(3, 5)]
    [InlineData(10, 5)]
    public void TryRebirthCharacter_SuccessfulRebirth_GrantsCorrectPointsAndResetsLevel(int currentRebirthCount, int expectedPoints)
    {
        // Arrange
        var character = CreateTestCharacter(100);
        character.RebirthLevel = currentRebirthCount;
        character.QuestComponent.AddFlag("REBIRTH_QUALIFIED");
        character.AddItem(9007, 1);
        
        string opId = Guid.NewGuid().ToString();
        int initialStatPoints = character.StatPoints;

        // Act
        var (success, msg, points) = _rebirthManager.TryRebirthCharacter(character, opId);

        // Assert
        Assert.True(success);
        Assert.Equal(expectedPoints, points);
        Assert.Equal(1, character.Level);
        Assert.Equal(currentRebirthCount + 1, character.RebirthLevel);
        Assert.Equal(initialStatPoints + expectedPoints, character.StatPoints);

        var history = character.RebirthHistory.Single();
        Assert.True(history.Success);
        Assert.Equal(expectedPoints, history.StatPointsGranted);
        Assert.Equal(1, history.NewLevel);
        Assert.Equal(currentRebirthCount + 1, history.NewRebirthCount);
    }
}