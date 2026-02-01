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
        // Assuming the test runs from bin/Debug/net10.0, we need to point to Content
        // But usually Content is copied or we need to go up folders.
        // Let's rely on the file existing in Content/Data/quests.json relative to repo root
        // Tests usually run in a temp folder.

        string path = System.IO.Path.Combine(System.AppContext.BaseDirectory, "Content/Data/quests.json"); // Relative from TWL.Tests/bin/Debug/net10.0
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
            Assert.Equal("El Despertar", q1.Title);
            Assert.Equal(50, q1.Rewards.Exp);
        }
    }

    [Fact]
    public void TryProgress_ShouldUpdateCorrectly()
    {
        _playerQuests.StartQuest(1); // Kill Slime (2)

        // Correct type and target
        var updated = _playerQuests.TryProgress("Kill", "Slime");
        Assert.Single(updated);
        Assert.Equal(1, updated[0]);
        Assert.Equal(1, _playerQuests.QuestProgress[1][0]);

        // Wrong target
        updated = _playerQuests.TryProgress("Kill", "Goblin");
        Assert.Empty(updated);

        // Wrong type
        updated = _playerQuests.TryProgress("Talk", "Slime");
        Assert.Empty(updated);

        // Complete it
        updated = _playerQuests.TryProgress("Kill", "Slime");
        Assert.Single(updated);
        Assert.Equal(2, _playerQuests.QuestProgress[1][0]);
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[1]);

        // Should not update if already complete
        updated = _playerQuests.TryProgress("Kill", "Slime");
        Assert.Empty(updated);
    }

    [Fact]
    public void BasicChain_ShouldWork_EndToEnd()
    {
        var qm = new ServerQuestManager();
        string path = System.IO.Path.Combine(System.AppContext.BaseDirectory, "Content/Data/quests.json");
        if (!System.IO.File.Exists(path)) path = "Content/Data/quests.json";

        if (!System.IO.File.Exists(path)) return; // Skip if file not found in test env

        qm.Load(path);
        var pq = new PlayerQuestComponent(qm);

        // --- Quest 1001: El Despertar ---
        Assert.True(pq.CanStartQuest(1001), "Should be able to start 1001");
        Assert.True(pq.StartQuest(1001));

        // Talk to Anciano Varado
        var updated = pq.TryProgress("Talk", "Anciano Varado");
        Assert.Single(updated);

        Assert.Equal(QuestState.Completed, pq.QuestStates[1001]);
        Assert.True(pq.ClaimReward(1001));

        // --- Quest 1002: Supervivencia Básica ---
        Assert.True(pq.CanStartQuest(1002), "Should be able to start 1002");
        Assert.True(pq.StartQuest(1002));

        // Collect 5 Madera
        for (int i = 0; i < 5; i++)
        {
            updated = pq.TryProgress("Collect", "Madera");
        }
        Assert.Single(updated); // Last one implies update

        Assert.Equal(QuestState.Completed, pq.QuestStates[1002]);
        Assert.True(pq.ClaimReward(1002));

        // --- Quest 1003: El Cangrejo Molesto ---
        Assert.True(pq.CanStartQuest(1003), "Should be able to start 1003");
        Assert.True(pq.StartQuest(1003));

        // Kill Cangrejo de Playa
        updated = pq.TryProgress("Kill", "Cangrejo de Playa");
        Assert.Single(updated);

        Assert.Equal(QuestState.Completed, pq.QuestStates[1003]);
        Assert.True(pq.ClaimReward(1003));
    }
}
