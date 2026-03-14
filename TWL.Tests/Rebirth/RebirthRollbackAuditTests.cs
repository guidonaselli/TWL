using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace TWL.Tests.Rebirth;

/// <summary>
/// Failure-path tests for Rebirth rollback and audit behavior.
/// </summary>
public class RebirthRollbackAuditTests
{
    private class FaultyCharacter : ServerCharacter
    {
        public bool ShouldFailReset { get; set; }

        public override void ResetStatsToBaseline()
        {
            if (ShouldFailReset)
            {
                throw new InvalidOperationException("Simulated database/logic failure during stat reset.");
            }
            base.ResetStatsToBaseline();
        }
    }

    private readonly RebirthManager _rebirthManager;
    private readonly Mock<ILogger<RebirthManager>> _mockLogger;

    public RebirthRollbackAuditTests()
    {
        _mockLogger = new Mock<ILogger<RebirthManager>>();
        _rebirthManager = new RebirthManager(_mockLogger.Object);
    }

    [Fact]
    public void Rebirth_ShouldRollback_OnException()
    {
        // Setup
        var character = new FaultyCharacter
        {
            Id = 1,
            Level = 100,
            RebirthLevel = 0,
            StatPoints = 10,
            ShouldFailReset = true
        };
        character.QuestComponent.AddFlag("REBIRTH_QUALIFIED");
        character.AddItem(9007, 1);

        // Act
        var (success, message, _) = _rebirthManager.TryRebirthCharacter(character, "fail-op-1");

        // Assert
        Assert.False(success);
        Assert.Contains("Internal error", message);

        // Verify Rollback
        Assert.Equal(100, character.Level);
        Assert.Equal(0, character.RebirthLevel);
        Assert.Equal(10, character.StatPoints);
        
        // Item should NOT be consumed if transaction failed
        Assert.True(character.HasItem(9007, 1));
    }


    [Fact]
    public void Audit_ShouldLogFailure_WhenRequirementsMissing()
    {
        var character = new ServerCharacter { Id = 1, Level = 99 }; // Too low
        
        var (success, message, _) = _rebirthManager.TryRebirthCharacter(character, "audit-fail-1");
        
        Assert.False(success);
        Assert.Equal("Level 100 required.", message);

        // Verify Logger was called (Task 2.2)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Rebirth failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}
