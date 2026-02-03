using System.Text.Json;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;

namespace TWL.Tests;

public class QuestSystemTests
{
    private readonly PlayerQuestComponent _playerQuests;
    private readonly ServerQuestManager _questManager;

    public QuestSystemTests()
    {
        _questManager = new ServerQuestManager();
        // Create mock data
        var quests = new List<QuestDefinition>
        {
            new()
            {
                QuestId = 1,
                Title = "Test Quest 1",
                Description = "Desc",
                Requirements = new List<int>(),
                Objectives = new List<ObjectiveDefinition>
                {
                    new("Kill", "Slime", 2, "Kill 2 Slimes")
                },
                Rewards = new RewardDefinition(100, 0, new List<ItemReward>())
            },
            new()
            {
                QuestId = 2,
                Title = "Test Quest 2",
                Description = "Desc",
                Requirements = new List<int> { 1 },
                Objectives = new List<ObjectiveDefinition>
                {
                    new("Talk", "Npc", 1, "Talk")
                },
                Rewards = new RewardDefinition(100, 0, new List<ItemReward>())
            }
        };

        var json = JsonSerializer.Serialize(quests);
        File.WriteAllText("test_quests.json", json);
        _questManager.Load("test_quests.json");

        _playerQuests = new PlayerQuestComponent(_questManager);
    }

    [Fact]
    public void StartQuest_ShouldSucceed_WhenRequirementsMet()
    {
        var result = _playerQuests.StartQuest(1);
        Assert.True(result);
        Assert.Equal(QuestState.InProgress, _playerQuests.QuestStates[1]);
    }

    [Fact]
    public void StartQuest_ShouldFail_WhenRequirementsNotMet()
    {
        var result = _playerQuests.StartQuest(2); // Requires 1 completed
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

        var result = _playerQuests.ClaimReward(1);
        Assert.True(result);
        Assert.Equal(QuestState.RewardClaimed, _playerQuests.QuestStates[1]);
    }

    [Fact]
    public void StartQuest_ShouldSucceed_AfterPrerequisiteClaimed()
    {
        _playerQuests.StartQuest(1);
        _playerQuests.UpdateProgress(1, 0, 2);
        _playerQuests.ClaimReward(1);

        var result = _playerQuests.StartQuest(2);
        Assert.True(result);
    }

    [Fact]
    public void LoadRealQuests_ShouldLoadCorrectly()
    {
        var qm = new ServerQuestManager();
        var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../Content/Data");
        if (!Directory.Exists(dir))
        {
            dir = Path.Combine(Directory.GetCurrentDirectory(), "Content/Data");
        }
        var path = Path.Combine(dir, "quests.json");

        if (File.Exists(path))
        {
            qm.Load(path);
            var q1 = qm.GetDefinition(1001);
            Assert.NotNull(q1);
            Assert.Equal("Despertar en la Playa", q1.Title);
            Assert.Equal(10, q1.Rewards.Exp);
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
        var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../Content/Data");
        if (!Directory.Exists(dir))
        {
            dir = Path.Combine(Directory.GetCurrentDirectory(), "Content/Data");
        }
        var path = Path.Combine(dir, "quests.json");

        if (!File.Exists(path))
        {
            return; // Skip if file not found in test env
        }

        qm.Load(path);
        var pq = new PlayerQuestComponent(qm);
        // Need to attach character to use item logic
        pq.Character = new TWL.Server.Simulation.Networking.ServerCharacter { Id = 1, Name = "Test" };

        // --- Quest 1001 ---
        Assert.True(pq.CanStartQuest(1001), "Should be able to start 1001");
        Assert.True(pq.StartQuest(1001));

        var updated = pq.TryProgress("Talk", "Capitana Maren");
        Assert.Single(updated);

        Assert.Equal(QuestState.Completed, pq.QuestStates[1001]);
        Assert.True(pq.ClaimReward(1001));

        // --- Quest 1002 ---
        Assert.True(pq.CanStartQuest(1002), "Should be able to start 1002");
        Assert.True(pq.StartQuest(1002));

        // Collect 3 Coconuts
        pq.Character.AddItem(7330, 3);
        // Note: AddItem handles update internally if connected, otherwise we need to manual update?
        // PlayerQuestComponent subscribes to character events. So AddItem works.

        Assert.Equal(QuestState.Completed, pq.QuestStates[1002]);
        Assert.True(pq.ClaimReward(1002));
    }
}