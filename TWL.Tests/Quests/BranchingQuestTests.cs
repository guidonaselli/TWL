using System.Collections.Generic;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;
using Xunit;

namespace TWL.Tests.Quests;

public class BranchingQuestTests
{
    private readonly ServerQuestManager _questManager;
    private readonly PlayerQuestComponent _playerQuests;

    public BranchingQuestTests()
    {
        _questManager = new ServerQuestManager();
        // Create mock data
        var quests = new List<QuestDefinition>
        {
            new QuestDefinition
            {
                QuestId = 1,
                Title = "Path A",
                Description = "A",
                Objectives = new List<ObjectiveDefinition> { new ObjectiveDefinition("Talk", "A", 1, "Talk") },
                Rewards = new RewardDefinition(100, 0, new List<ItemReward>()),
                MutualExclusionGroup = "Path",
                FlagsSet = new List<string> { "Done_A" },
                BlockedByFlags = new List<string> { "Done_B" }
            },
            new QuestDefinition
            {
                QuestId = 2,
                Title = "Path B",
                Description = "B",
                Objectives = new List<ObjectiveDefinition> { new ObjectiveDefinition("Talk", "B", 1, "Talk") },
                Rewards = new RewardDefinition(100, 0, new List<ItemReward>()),
                MutualExclusionGroup = "Path",
                FlagsSet = new List<string> { "Done_B" },
                BlockedByFlags = new List<string> { "Done_A" }
            }
        };

        string json = System.Text.Json.JsonSerializer.Serialize(quests);
        System.IO.File.WriteAllText("test_quests_branching.json", json);
        _questManager.Load("test_quests_branching.json");

        _playerQuests = new PlayerQuestComponent(_questManager);
    }

    [Fact]
    public void StartQuest_ShouldBlockMutualExclusion()
    {
        Assert.True(_playerQuests.StartQuest(1));
        Assert.False(_playerQuests.StartQuest(2)); // Blocked by MutualExclusionGroup (A is InProgress)
    }

    [Fact]
    public void CompletedQuest_ShouldBlockOtherBranch_ViaFlags()
    {
        // Complete A
        Assert.True(_playerQuests.StartQuest(1));
        _playerQuests.TryProgress("Talk", "A");
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[1]);
        Assert.True(_playerQuests.ClaimReward(1)); // Sets flag "Done_A"

        // Try B
        Assert.False(_playerQuests.StartQuest(2)); // Blocked by Flags "Done_A"
    }
}
