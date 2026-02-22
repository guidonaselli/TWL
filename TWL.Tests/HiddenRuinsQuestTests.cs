using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Requests;

namespace TWL.Tests;

public class HiddenRuinsQuestTests
{
    private readonly ServerCharacter _character;
    private readonly InteractionManager _interactionManager;
    private readonly PlayerQuestComponent _questComponent;
    private readonly ServerQuestManager _questManager;

    public HiddenRuinsQuestTests()
    {
        _questManager = new ServerQuestManager();
        _interactionManager = new InteractionManager();
        _character = new ServerCharacter { Id = 1, Name = "Explorer" };
        _questComponent = new PlayerQuestComponent(_questManager);

        // Load Real Data
        var contentPath = Path.Combine("..", "..", "..", "..", "Content", "Data");
        if (!Directory.Exists(contentPath))
        {
            contentPath = Path.Combine("Content", "Data");
        }

        _questManager.Load(Path.Combine(contentPath, "quests.json"));
        _interactionManager.Load(Path.Combine(contentPath, "interactions.json"));
    }

    [Fact]
    public void HiddenRuins_FullChain_Progression()
    {
        // 1. Start Quest 1301
        // Simulate prerequisites using actual game logic
        SimulateQuestCompletion(1103);

        Assert.True(_questComponent.StartQuest(1301), "Should be able to start 1301");
        Assert.Equal(QuestState.InProgress, _questComponent.QuestStates[1301]);

        // 2. Interact with StrangeStoneDebris
        var interactionType = _interactionManager.ProcessInteraction(_character, _questComponent, "StrangeStoneDebris");
        Assert.NotNull(interactionType);
        Assert.True(_character.HasItem(8101, 1), "Should have Strange Stone");

        // 3. Update Progress (simulated client/server event)
        _questComponent.TryProgress("Collect", "StrangeStoneDebris");
        Assert.Equal(QuestState.Completed, _questComponent.QuestStates[1301]);

        // 4. Claim Reward
        Assert.True(_questComponent.ClaimReward(1301));

        // 5. Start Quest 1302
        Assert.True(_questComponent.StartQuest(1302));

        // 6. Interact AncientTablet
        interactionType = _interactionManager.ProcessInteraction(_character, _questComponent, "AncientTablet");
        Assert.NotNull(interactionType);
        Assert.True(_character.HasItem(8102, 1), "Should have Ancient Rubbing");

        // 7. Update Progress
        _questComponent.TryProgress("Collect", "AncientTablet");
        Assert.Equal(QuestState.Completed, _questComponent.QuestStates[1302]);

        // 8. Claim Reward (Gets Key 8103)
        // Note: ClaimReward in component updates state, but ClientSession applies rewards.
        // In this unit test, we must manually apply rewards if we want to check them, or just trust the state change.
        // ProcessInteraction gives items, but Quest Rewards are handled by ClientSession logic usually.
        // Let's manually give the key to simulate reward
        _character.AddItem(8103, 1);
        Assert.True(_questComponent.ClaimReward(1302));

        // 9. Start Quest 1303
        Assert.True(_questComponent.StartQuest(1303));

        // 10. Kill RuinsGuardian
        // No interaction for kill, just direct progress update from CombatManager
        _questComponent.TryProgress("Kill", "RuinsGuardian");
        Assert.Equal(QuestState.Completed, _questComponent.QuestStates[1303]);
        Assert.True(_questComponent.ClaimReward(1303));

        // 11. Start Quest 1304
        Assert.True(_questComponent.StartQuest(1304));

        // 12. Interact RuinsEntrance
        // This interaction gives no items, but requires Quest 1304 to be InProgress (checked by InteractionManager)
        interactionType = _interactionManager.ProcessInteraction(_character, _questComponent, "RuinsEntrance");
        Assert.NotNull(interactionType);

        _questComponent.TryProgress("Interact", "RuinsEntrance");
        Assert.Equal(QuestState.Completed, _questComponent.QuestStates[1304]);
    }

    private void SimulateQuestCompletion(int questId)
    {
        if (_questComponent.QuestStates.TryGetValue(questId, out var state) && (state == QuestState.Completed || state == QuestState.RewardClaimed))
        {
            return;
        }

        var def = _questManager.GetDefinition(questId);
        Assert.NotNull(def);

        // Fulfill prerequisites recursively
        foreach (var reqId in def.Requirements)
        {
            SimulateQuestCompletion(reqId);
        }

        // Fulfill gating conditions
        _character.SetLevel(Math.Max(_character.Level, def.RequiredLevel > 0 ? def.RequiredLevel : 100));
        if (def.RequiredItems != null)
        {
            foreach (var reqItem in def.RequiredItems)
            {
                if (!_character.HasItem(reqItem.ItemId, reqItem.Quantity))
                {
                    _character.AddItem(reqItem.ItemId, reqItem.Quantity);
                }
            }
        }

        Assert.True(_questComponent.StartQuest(questId), $"Failed to start prerequisite quest {questId}");

        foreach (var obj in def.Objectives)
        {
            if (obj.Type == "Talk") 
                _questComponent.TryProgress("Talk", obj.TargetName);
            else if (obj.Type == "Kill") 
                _questComponent.TryProgress("Kill", obj.TargetName, obj.RequiredCount);
            else if (obj.Type == "Collect" || obj.Type == "Interact" || obj.Type == "Gather" || obj.Type == "Craft" || obj.Type == "Compound" || obj.Type == "Explore")
            {
                 // For collect objectives, we might need to actually inject the item
                 if (obj.DataId.HasValue && obj.Type == "Collect")
                 {
                     _character.AddItem(obj.DataId.Value, obj.RequiredCount);
                 }
                 _questComponent.TryProgress(obj.Type, obj.TargetName, obj.RequiredCount);
            }
            else
            {
                _questComponent.TryProgress(obj.Type, obj.TargetName, obj.RequiredCount);
            }
        }

        Assert.Equal(QuestState.Completed, _questComponent.QuestStates[questId]);
        _questComponent.ClaimReward(questId);
    }
}