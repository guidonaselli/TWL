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
        // 1. Start Quest 1201
        // Prereq 1103 must be completed if we were enforcing it strictly,
        // but CanStartQuest checks QuestStates of prerequisites.
        // For this test, let's force-set prereq 1103 as completed if needed.
        // Actually, let's just cheat and assume we can start it if we satisfy prereqs manually.
        SetQuestCompleted(1103);

        Assert.True(_questComponent.StartQuest(1201), "Should be able to start 1201");
        Assert.Equal(QuestState.InProgress, _questComponent.QuestStates[1201]);

        // 2. Interact with StrangeStoneDebris
        var interactionType = _interactionManager.ProcessInteraction(_character, _questComponent, "StrangeStoneDebris");
        Assert.NotNull(interactionType);
        Assert.True(_character.HasItem(8101, 1), "Should have Strange Stone");

        // 3. Update Progress (simulated client/server event)
        _questComponent.TryProgress("Collect", "StrangeStoneDebris");
        Assert.Equal(QuestState.Completed, _questComponent.QuestStates[1201]);

        // 4. Claim Reward
        Assert.True(_questComponent.ClaimReward(1201));

        // 5. Start Quest 1202
        Assert.True(_questComponent.StartQuest(1202));

        // 6. Interact AncientTablet
        interactionType = _interactionManager.ProcessInteraction(_character, _questComponent, "AncientTablet");
        Assert.NotNull(interactionType);
        Assert.True(_character.HasItem(8102, 1), "Should have Ancient Rubbing");

        // 7. Update Progress
        _questComponent.TryProgress("Collect", "AncientTablet");
        Assert.Equal(QuestState.Completed, _questComponent.QuestStates[1202]);

        // 8. Claim Reward (Gets Key 8103)
        // Note: ClaimReward in component updates state, but ClientSession applies rewards.
        // In this unit test, we must manually apply rewards if we want to check them, or just trust the state change.
        // ProcessInteraction gives items, but Quest Rewards are handled by ClientSession logic usually.
        // Let's manually give the key to simulate reward
        _character.AddItem(8103, 1);
        Assert.True(_questComponent.ClaimReward(1202));

        // 9. Start Quest 1203
        Assert.True(_questComponent.StartQuest(1203));

        // 10. Kill RuinsGuardian
        // No interaction for kill, just direct progress update from CombatManager
        _questComponent.TryProgress("Kill", "RuinsGuardian");
        Assert.Equal(QuestState.Completed, _questComponent.QuestStates[1203]);
        Assert.True(_questComponent.ClaimReward(1203));

        // 11. Start Quest 1204
        Assert.True(_questComponent.StartQuest(1204));

        // 12. Interact RuinsEntrance
        // This interaction gives no items, but requires Quest 1204 to be InProgress (checked by InteractionManager)
        interactionType = _interactionManager.ProcessInteraction(_character, _questComponent, "RuinsEntrance");
        Assert.NotNull(interactionType);

        _questComponent.TryProgress("Interact", "RuinsEntrance");
        Assert.Equal(QuestState.Completed, _questComponent.QuestStates[1204]);
    }

    private void SetQuestCompleted(int questId)
    {
        // Reflection or public access needed?
        // PlayerQuestComponent.QuestStates is public get, private set.
        // But the dictionary it returns is the actual reference?
        // "public Dictionary<int, QuestState> QuestStates { get; private set; } = new();"
        // Yes, likely returning the reference.
        _questComponent.QuestStates[questId] = QuestState.Completed;
    }
}