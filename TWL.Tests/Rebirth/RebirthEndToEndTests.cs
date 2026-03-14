using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.DTO;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace TWL.Tests.Rebirth;

/// <summary>
/// End-to-end tests for the Character Rebirth system.
/// Validates the full flow from request handling to state mutation and audit history.
/// </summary>
public class RebirthEndToEndTests
{
    private readonly RebirthManager _rebirthManager;
    private readonly Mock<ILogger<RebirthManager>> _mockLogger;

    public RebirthEndToEndTests()
    {
        _mockLogger = new Mock<ILogger<RebirthManager>>();
        _rebirthManager = new RebirthManager(_mockLogger.Object);
    }

    [Fact]
    public void FullRebirthFlow_Succeeds_WhenRequirementsMet()
    {
        // 1. Setup Character with all requirements
        var character = new ServerCharacter
        {
            Id = 1,
            Name = "EndToEndHero",
            Level = 100,
            RebirthLevel = 0,
            StatPoints = 10
        };
        
        // Add Rebirth qualified flag
        character.QuestComponent.AddFlag("REBIRTH_QUALIFIED");
        
        // Add Core Resonance Shard (9007)
        character.AddItem(9007, 1);

        // 2. Execute Rebirth via Manager
        var opId = Guid.NewGuid().ToString();
        var (success, message, granted) = _rebirthManager.TryRebirthCharacter(character, opId);

        // 3. Assert Results
        Assert.True(success, $"Rebirth failed: {message}");
        Assert.Equal(1, character.RebirthLevel);
        Assert.Equal(1, character.Level);
        Assert.Equal(0, character.Exp);
        Assert.Equal(10 + 20, character.StatPoints); // 10 initial + 20 bonus for Gen 1
        
        // Ensure item was consumed
        Assert.False(character.HasItem(9007, 1));

        // 4. Verify History Record
        Assert.Single(character.RebirthHistory);
        var record = character.RebirthHistory[0];
        Assert.Equal(opId, record.OperationId);
        Assert.Equal(100, record.OldLevel);
        Assert.Equal(1, record.NewLevel);
        Assert.Equal(0, record.OldRebirthCount);
        Assert.Equal(1, record.NewRebirthCount);
        Assert.Equal(20, record.StatPointsGranted);
        Assert.True(record.Success);
    }

    [Fact]
    public void Rebirth_Visibility_PersistsThroughSaveLoad()
    {
        var character = new ServerCharacter { Id = 1, Level = 100 };
        character.QuestComponent.AddFlag("REBIRTH_QUALIFIED");
        character.AddItem(9007, 1);
        
        _rebirthManager.TryRebirthCharacter(character, "audit-log-1");

        // Save
        var saveData = character.GetSaveData();
        Assert.Equal(1, saveData.RebirthLevel);
        Assert.Equal(character.Id, saveData.Id);

        // Load into new instance
        var restored = new ServerCharacter();
        restored.LoadSaveData(saveData);

        Assert.Equal(1, restored.RebirthLevel);
        Assert.Equal(1, restored.Level);
        Assert.Equal("audit-log-1", restored.RebirthHistory[0].OperationId);
    }

    [Fact]
    public void Rebirth_DiminishingReturns_Progression()
    {
        var character = new ServerCharacter { Id = 1, Level = 100 };
        character.QuestComponent.AddFlag("REBIRTH_QUALIFIED");
        
        // Round 1 (Gen 0 -> 1): +20
        character.AddItem(9007, 1);
        var res1 = _rebirthManager.TryRebirthCharacter(character, "op1");
        Assert.Equal(20, res1.StatPointsGained);

        // Round 2 (Gen 1 -> 2): +15
        character.Level = 100;
        character.AddItem(9007, 1);
        var res2 = _rebirthManager.TryRebirthCharacter(character, "op2");
        Assert.Equal(15, res2.StatPointsGained);

        // Round 3 (Gen 2 -> 3): +10
        character.Level = 100;
        character.AddItem(9007, 1);
        var res3 = _rebirthManager.TryRebirthCharacter(character, "op3");
        Assert.Equal(10, res3.StatPointsGained);

        // Round 4 (Gen 3 -> 4): +5
        character.Level = 100;
        character.AddItem(9007, 1);
        var res4 = _rebirthManager.TryRebirthCharacter(character, "op4");
        Assert.Equal(5, res4.StatPointsGained);
    }
}
