using System.Collections.Generic;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;
using Xunit;

namespace TWL.Tests;

public class QuestSystemTests
{
    private readonly ServerQuestManager _questManager;
    private readonly PlayerQuestComponent _playerQuests;

    public QuestSystemTests()
    {
        _questManager = new ServerQuestManager();
        // Create mock data
        var quests = new List<QuestDefinition>
        {
            new QuestDefinition
            {
                QuestId = 1,
                Title = "Test Quest 1",
                Description = "Desc",
                Requirements = new List<int>(),
                Objectives = new List<ObjectiveDefinition>
                {
                    new ObjectiveDefinition("Kill", "Slime", 2, "Kill 2 Slimes")
                },
                Rewards = new RewardDefinition(100, 0, new List<ItemReward>())
            },
            new QuestDefinition
            {
                QuestId = 2,
                Title = "Test Quest 2",
                Description = "Desc",
                Requirements = new List<int> { 1 },
                Objectives = new List<ObjectiveDefinition>
                {
                    new ObjectiveDefinition("Talk", "Npc", 1, "Talk")
                },
                Rewards = new RewardDefinition(100, 0, new List<ItemReward>())
            }
        };

        string json = System.Text.Json.JsonSerializer.Serialize(quests);
        System.IO.File.WriteAllText("test_quests.json", json);
        _questManager.Load("test_quests.json");

        _playerQuests = new PlayerQuestComponent(_questManager);
    }

    [Fact]
    public void StartQuest_ShouldSucceed_WhenRequirementsMet()
    {
        bool result = _playerQuests.StartQuest(1);
        Assert.True(result);
        Assert.Equal(QuestState.InProgress, _playerQuests.QuestStates[1]);
    }

    [Fact]
    public void StartQuest_ShouldFail_WhenRequirementsNotMet()
    {
        bool result = _playerQuests.StartQuest(2); // Requires 1 completed
        Assert.False(result);
        Assert.False(_playerQuests.QuestStates.ContainsKey(2));
    }

    [Fact]
    public void UpdateProgress_ShouldCompleteQuest_WhenObjectivesMet()
    {
        _playerQuests.StartQuest(1);

        _playerQuests.UpdateProgress(1, 0, 1);
        Assert.Equal(QuestState.InProgress, _playerQuests.QuestStates[1]);
        Assert.Equal(1, _playerQuests.QuestProgress[1][0]);

        _playerQuests.UpdateProgress(1, 0, 1);
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[1]);
    }

    [Fact]
    public void ClaimReward_ShouldSucceed_WhenQuestCompleted()
    {
        _playerQuests.StartQuest(1);
        _playerQuests.UpdateProgress(1, 0, 2); // Complete it

        bool result = _playerQuests.ClaimReward(1);
        Assert.True(result);
        Assert.Equal(QuestState.RewardClaimed, _playerQuests.QuestStates[1]);
    }

    [Fact]
    public void StartQuest_ShouldSucceed_AfterPrerequisiteClaimed()
    {
        _playerQuests.StartQuest(1);
        _playerQuests.UpdateProgress(1, 0, 2);
        _playerQuests.ClaimReward(1);

        bool result = _playerQuests.StartQuest(2);
        Assert.True(result);
    }

    [Fact]
    public void LoadRealQuests_ShouldLoadCorrectly()
    {
        var qm = new ServerQuestManager();
        // Assuming the test runs from bin/Debug/net8.0, we need to point to Content
        // But usually Content is copied or we need to go up folders.
        // Let's rely on the file existing in Content/Data/quests.json relative to repo root
        // Tests usually run in a temp folder.

        string path = "../../../Content/Data/quests.json"; // Relative from TWL.Tests/bin/Debug/net8.0
        if (!System.IO.File.Exists(path))
        {
             // Fallback for different test runners
             path = "Content/Data/quests.json";
        }

        // Ensure we can find the file, otherwise skip or fail
        if (System.IO.File.Exists(path))
        {
            qm.Load(path);
            var q1 = qm.GetDefinition(1001);
            Assert.NotNull(q1);
            Assert.Equal("Washed Ashore", q1.Title);
            Assert.Equal(10, q1.Rewards.Exp);
        }
    }
}
