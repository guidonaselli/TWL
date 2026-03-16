using System;
using Xunit;
using TWL.Server.Services.Combat;
using TWL.Server.Simulation.Networking;

namespace TWL.Tests.Server.Combat;

public class DeathPenaltyServiceTests
{
    private readonly DeathPenaltyService _service;

    public DeathPenaltyServiceTests()
    {
        _service = new DeathPenaltyService();
    }

    [Fact]
    public void ApplyExpPenalty_LosesExactlyOnePercent()
    {
        // Arrange
        var character = new ServerCharacter { Exp = 1000 };
        string deathEventId = Guid.NewGuid().ToString();

        // Act
        var result = _service.ApplyExpPenalty(character, deathEventId);

        // Assert
        Assert.True(result.Applied);
        Assert.False(result.WasDuplicate);
        Assert.Equal(10, result.ExpLost);
        Assert.Equal(1000, result.PreviousExp);
        Assert.Equal(990, result.NewExp);
        Assert.Equal(990, character.Exp);
    }

    [Fact]
    public void ApplyExpPenalty_FloorsAtZero()
    {
        // Arrange
        var character = new ServerCharacter { Exp = 0 };
        string deathEventId = Guid.NewGuid().ToString();

        // Act
        var result = _service.ApplyExpPenalty(character, deathEventId);

        // Assert
        Assert.True(result.Applied);
        Assert.False(result.WasDuplicate);
        Assert.Equal(0, result.ExpLost);
        Assert.Equal(0, result.PreviousExp);
        Assert.Equal(0, result.NewExp);
        Assert.Equal(0, character.Exp);
    }

    [Fact]
    public void ApplyExpPenalty_DuplicateEvent_IsIgnored()
    {
        // Arrange
        var character = new ServerCharacter { Exp = 1000 };
        string deathEventId = Guid.NewGuid().ToString();

        // Act 1
        var result1 = _service.ApplyExpPenalty(character, deathEventId);

        // Act 2 (Duplicate)
        var result2 = _service.ApplyExpPenalty(character, deathEventId);

        // Assert
        Assert.True(result1.Applied);
        Assert.False(result1.WasDuplicate);

        Assert.False(result2.Applied);
        Assert.True(result2.WasDuplicate);
        Assert.Equal(0, result2.ExpLost);
        Assert.Equal(990, result2.PreviousExp);
        Assert.Equal(990, result2.NewExp);

        Assert.Equal(990, character.Exp);
    }

    [Theory]
    [InlineData(1050, 10)] // 10.5 floored to 10
    [InlineData(99, 0)]    // 0.99 floored to 0
    public void ApplyExpPenalty_FractionalPercent_FloorsToNearestInt(int startExp, int expectedLoss)
    {
        // Arrange
        var character = new ServerCharacter { Exp = startExp };
        string deathEventId = Guid.NewGuid().ToString();

        // Act
        var result = _service.ApplyExpPenalty(character, deathEventId);

        // Assert
        Assert.Equal(expectedLoss, result.ExpLost);
        Assert.Equal(startExp - expectedLoss, result.NewExp);
        Assert.Equal(startExp - expectedLoss, character.Exp);
    }
}
