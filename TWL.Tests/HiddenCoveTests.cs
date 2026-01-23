using System.Collections.Generic;
using System.IO;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;
using Xunit;

namespace TWL.Tests;

public class HiddenCoveTests
{
    private ServerQuestManager _questManager;
    private InteractionManager _interactionManager;
    private ServerCharacter _character;
    private PlayerQuestComponent _questComponent;

    public HiddenCoveTests()
    {
        _questManager = new ServerQuestManager();
        _interactionManager = new InteractionManager();
        _character = new ServerCharacter { Id = 1, Name = "Survivor" };
        _questComponent = new PlayerQuestComponent(_questManager);

        // Load Real Data
        string contentPath = Path.Combine("..", "..", "..", "..", "Content", "Data");
        if (!Directory.Exists(contentPath)) contentPath = Path.Combine("Content", "Data");

        _questManager.Load(Path.Combine(contentPath, "quests.json"));
        _interactionManager.Load(Path.Combine(contentPath, "interactions.json"));
    }

    [Fact]
    public void HiddenCove_Chain_Progression()
    {
        // Setup: Complete 1305
        SetQuestCompleted(1305);

        // 1. Start Quest 1401 (Echoes from the Sea)
        Assert.True(_questComponent.StartQuest(1401), "Should start 1401");

        // Obj 1: Interact RepairedRadioTower
        _questComponent.TryProgress("Interact", "RepairedRadioTower");

        // Obj 2: Interact NorthEastCliff
        _questComponent.TryProgress("Interact", "NorthEastCliff");

        Assert.Equal(QuestState.Completed, _questComponent.QuestStates[1401]);
        Assert.True(_questComponent.ClaimReward(1401));

        // 2. Start Quest 1402 (Blocked Passage)
        Assert.True(_questComponent.StartQuest(1402));

        // Obj 1: Interact HeavyRocks
        _questComponent.TryProgress("Interact", "HeavyRocks");

        // Obj 2: Collect Sulfur (Simulate gathering)
        bool gathered = _interactionManager.ProcessInteraction(_character, _questComponent, "SulfurVent");
        Assert.True(gathered, "Should gather Sulfur");
        Assert.True(_character.HasItem(9001, 1));
        _questComponent.TryProgress("Collect", "SulfurVent");

        // Obj 3: Collect Charcoal
        gathered = _interactionManager.ProcessInteraction(_character, _questComponent, "BurntTree");
        Assert.True(gathered, "Should gather Charcoal");
        Assert.True(_character.HasItem(9002, 1));
        _questComponent.TryProgress("Collect", "BurntTree");

        // Obj 4: Craft Black Powder
        // Verify we have items
        Assert.True(_character.HasItem(9001, 1));
        Assert.True(_character.HasItem(9002, 1));

        bool crafted = _interactionManager.ProcessInteraction(_character, _questComponent, "AlchemyTable");
        Assert.True(crafted, "Should craft Black Powder");
        Assert.True(_character.HasItem(9003, 1));

        _questComponent.TryProgress("Craft", "AlchemyTable");

        Assert.Equal(QuestState.Completed, _questComponent.QuestStates[1402]);
        Assert.True(_questComponent.ClaimReward(1402));

        // 3. Start Quest 1403 (Clearing the Way)
        Assert.True(_questComponent.StartQuest(1403));

        // Obj 1: Interact HeavyRocks (with Black Powder)
        // Ensure character has item.
        // Interaction AlchemyTable gives 9003 x1.
        // HeavyRocks consumes 1.

        bool blasted = _interactionManager.ProcessInteraction(_character, _questComponent, "HeavyRocks");
        Assert.True(blasted, "Should blast rocks");

        _questComponent.TryProgress("Interact", "HeavyRocks");
        Assert.Equal(QuestState.Completed, _questComponent.QuestStates[1403]);
        Assert.True(_questComponent.ClaimReward(1403));

        // 4. Start Quest 1404 (Hidden Dock)
        Assert.True(_questComponent.StartQuest(1404));

        _questComponent.TryProgress("Interact", "HiddenDock");
        Assert.Equal(QuestState.Completed, _questComponent.QuestStates[1404]);
        Assert.True(_questComponent.ClaimReward(1404));
    }

    private void SetQuestCompleted(int questId)
    {
        _questComponent.QuestStates[questId] = QuestState.Completed;
    }

    [Fact]
    public void HiddenCove_Sidequest_Fisherman()
    {
        // Prerequisites
        SetQuestCompleted(2011); // First Catch
        SetQuestCompleted(1404); // Hidden Dock

        Assert.True(_questComponent.StartQuest(2401), "Should start 2401");

        // Interaction
        // FishingSpot requires Item 303 (Rod).
        _character.AddItem(303, 1);

        bool fished = _interactionManager.ProcessInteraction(_character, _questComponent, "FishingSpot");
        Assert.True(fished, "Should catch rare fish");
        Assert.True(_character.HasItem(9005, 1));

        _questComponent.TryProgress("Collect", "FishingSpot");
        Assert.Equal(QuestState.Completed, _questComponent.QuestStates[2401]);
        Assert.True(_questComponent.ClaimReward(2401));
    }
}
