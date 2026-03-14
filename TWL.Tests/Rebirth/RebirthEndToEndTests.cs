<<<<<<< HEAD
using Moq;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.DTO;
using Microsoft.Extensions.Logging;
using Xunit;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Models;
using TWL.Shared.Domain.Requests;
using System.Linq;
using System;

namespace TWL.Tests.Rebirth;

/// <summary>
/// End-to-end tests for the Character Rebirth system.
/// Validates the full flow from request handling to state mutation and audit history.
/// </summary>
=======
using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Models;
using TWL.Shared.Domain.Requests;
using Xunit;

namespace TWL.Tests.Rebirth;

>>>>>>> gsd/M001/S06
public class RebirthEndToEndTests
{
    private readonly Mock<ILogger<RebirthManager>> _loggerMock;
    private readonly RebirthManager _rebirthManager;

    public RebirthEndToEndTests()
    {
        _loggerMock = new Mock<ILogger<RebirthManager>>();
        _rebirthManager = new RebirthManager(_loggerMock.Object);
    }

    [Fact]
<<<<<<< HEAD
    public void FullRebirthFlow_Succeeds_WhenRequirementsMet()
    {
        // 1. Setup Requirements
        int requiredQuestId = 5000;
        int requiredItemId = 9007; // Core Shard
=======
    public void Rebirth_FullCycle_Success()
    {
        // 1. Setup Requirements
        int requiredQuestId = 5000;
        int requiredItemId = 9999;
        int requiredItemQty = 5;
>>>>>>> gsd/M001/S06
        _rebirthManager.SetRequirements(new RebirthRequirements
        {
            MinLevel = 100,
            RequiredQuestId = requiredQuestId,
            RequiredItemId = requiredItemId,
<<<<<<< HEAD
            RequiredItemQuantity = 1
=======
            RequiredItemQuantity = requiredItemQty
>>>>>>> gsd/M001/S06
        });

        // 2. Setup Character
        var character = new ServerCharacter
        {
<<<<<<< HEAD
            Id = 1,
=======
            Id = 42,
>>>>>>> gsd/M001/S06
            Name = "EndToEndHero",
            Level = 100,
            RebirthLevel = 0,
            StatPoints = 10
        };
<<<<<<< HEAD
        
        // 3. Setup Quest Component
        var questManager = new ServerQuestManager();
        character.QuestComponent = new PlayerQuestComponent(questManager);
        character.QuestComponent.Character = character;
        
        // Add Rebirth qualified flag
        character.QuestComponent.AddFlag("REBIRTH_QUALIFIED");
        
        // Add Core Resonance Shard (9007)
        character.AddItem(9007, 1);

        // Force complete the required quest
        character.QuestComponent.QuestStates[requiredQuestId] = QuestState.Completed;

        // 4. Execute Rebirth
        var opId = Guid.NewGuid().ToString();
        var (success, message, granted) = _rebirthManager.TryRebirthCharacter(character, character.QuestComponent, opId);

        // 5. Assert Results
        Assert.True(success, $"Rebirth failed: {message}");
        Assert.Equal(1, character.RebirthLevel);
        Assert.Equal(1, character.Level);
        Assert.Equal(0, character.Exp);
        Assert.Equal(10 + 20, character.StatPoints); // 10 initial + 20 bonus for Gen 1
        
        // Ensure item was consumed
        Assert.False(character.HasItem(9007, 1));

        // 6. Verify History Record
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
        character.QuestComponent = new PlayerQuestComponent(new ServerQuestManager());
        character.QuestComponent.Character = character;
        character.QuestComponent.AddFlag("REBIRTH_QUALIFIED");
        character.AddItem(9007, 1);
        
        _rebirthManager.TryRebirthCharacter(character, character.QuestComponent, "audit-log-1");

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
=======
        character.AddItem(requiredItemId, requiredItemQty);

        // 3. Setup Quest Component
        var questManager = new ServerQuestManager();
        var questComponent = new PlayerQuestComponent(questManager);
        // Force complete the quest
        questComponent.QuestStates[requiredQuestId] = QuestState.Completed;

        string opId = "E2E_OP_SUCCESS";

        // 4. Execute Rebirth
        var (success, msg, pointsGained) = _rebirthManager.TryRebirthCharacter(character, questComponent, opId);

        // 5. Verify Outcome
        Assert.True(success);
        Assert.Equal(20, pointsGained); // 1st rebirth bonus
        Assert.Equal(1, character.Level);
        Assert.Equal(1, character.RebirthLevel);
        Assert.Equal(10 + 20, character.StatPoints);
        Assert.Equal(0, character.Exp);
        Assert.False(character.HasItem(requiredItemId, 1)); // Items should be consumed

        // 6. Verify History
        Assert.Single(character.RebirthHistory);
        var record = character.RebirthHistory[0];
        Assert.True(record.Success);
        Assert.Equal(opId, record.OperationId);
        Assert.Equal(0, record.OldRebirthCount);
        Assert.Equal(1, record.NewRebirthCount);
        Assert.Equal(100, record.OldLevel);
        Assert.Equal(1, record.NewLevel);
>>>>>>> gsd/M001/S06
    }

    [Fact]
    public void Rebirth_MultipleGenerations_CorrectScaling()
    {
<<<<<<< HEAD
        var character = new ServerCharacter { Id = 1, Level = 100, RebirthLevel = 0 };
        character.QuestComponent = new PlayerQuestComponent(new ServerQuestManager());
        character.QuestComponent.Character = character;
        character.QuestComponent.AddFlag("REBIRTH_QUALIFIED");
        
        // 1st Rebirth (0 -> 1)
        character.AddItem(9007, 1);
        _rebirthManager.TryRebirthCharacter(character, character.QuestComponent, "gen1");
=======
        // 1st Rebirth (0 -> 1)
        var character = new ServerCharacter { Id = 1, Level = 100, RebirthLevel = 0 };
        _rebirthManager.TryRebirthCharacter(character, "gen1");
>>>>>>> gsd/M001/S06
        Assert.Equal(20, character.RebirthHistory.Last().StatPointsGranted);

        // 2nd Rebirth (1 -> 2)
        character.Level = 100;
<<<<<<< HEAD
        character.AddItem(9007, 1);
=======
>>>>>>> gsd/M001/S06
        _rebirthManager.TryRebirthCharacter(character, "gen2");
        Assert.Equal(15, character.RebirthHistory.Last().StatPointsGranted);

        // 3rd Rebirth (2 -> 3)
        character.Level = 100;
<<<<<<< HEAD
        character.AddItem(9007, 1);
=======
>>>>>>> gsd/M001/S06
        _rebirthManager.TryRebirthCharacter(character, "gen3");
        Assert.Equal(10, character.RebirthHistory.Last().StatPointsGranted);

        // 4th Rebirth (3 -> 4)
        character.Level = 100;
<<<<<<< HEAD
        character.AddItem(9007, 1);
=======
>>>>>>> gsd/M001/S06
        _rebirthManager.TryRebirthCharacter(character, "gen4");
        Assert.Equal(5, character.RebirthHistory.Last().StatPointsGranted);

        Assert.Equal(4, character.RebirthLevel);
        Assert.Equal(4, character.RebirthHistory.Count);
    }
}
