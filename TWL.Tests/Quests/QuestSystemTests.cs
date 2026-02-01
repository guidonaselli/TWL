using System.Text.Json;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;

namespace TWL.Tests.Quests;

public class QuestSystemTests
{
    private readonly PlayerQuestComponent _playerQuests;
    private readonly ServerQuestManager _questManager;

    public QuestSystemTests()
    {
        _questManager = new ServerQuestManager();
        // Setup definitions manually
        var definitions = new List<QuestDefinition>
        {
            // Quest 1: Mutual Exclusion Group A - Quest 1
            new()
            {
                QuestId = 1,
                Title = "Group A - Q1",
                Description = "...",
                Objectives = new List<ObjectiveDefinition> { new("Talk", "Npc", 1, "Talk") },
                Rewards = new RewardDefinition(10, 0, new List<ItemReward>()),
                MutualExclusionGroup = "GroupA"
            },
            // Quest 2: Mutual Exclusion Group A - Quest 2
            new()
            {
                QuestId = 2,
                Title = "Group A - Q2",
                Description = "...",
                Objectives = new List<ObjectiveDefinition> { new("Talk", "Npc", 1, "Talk") },
                Rewards = new RewardDefinition(10, 0, new List<ItemReward>()),
                MutualExclusionGroup = "GroupA"
            },
            // Quest 3: Daily Quest
            new()
            {
                QuestId = 3,
                Title = "Daily Quest",
                Description = "...",
                Objectives = new List<ObjectiveDefinition> { new("Talk", "Npc", 1, "Talk") },
                Rewards = new RewardDefinition(10, 0, new List<ItemReward>()),
                Repeatability = QuestRepeatability.Daily
            },
            // Quest 4: Cooldown Quest (1 second)
            new()
            {
                QuestId = 4,
                Title = "Cooldown Quest",
                Description = "...",
                Objectives = new List<ObjectiveDefinition> { new("Talk", "Npc", 1, "Talk") },
                Rewards = new RewardDefinition(10, 0, new List<ItemReward>()),
                Repeatability = QuestRepeatability.Cooldown,
                RepeatCooldown = TimeSpan.FromSeconds(1)
            }
        };

        var json = JsonSerializer.Serialize(definitions);
        File.WriteAllText("test_quest_system.json", json);
        _questManager.Load("test_quest_system.json");

        _playerQuests = new PlayerQuestComponent(_questManager);
    }

    [Fact]
    public void DataLoading_ShouldBeCorrect()
    {
        var q1 = _questManager.GetDefinition(1);
        Assert.NotNull(q1);
        Assert.Equal("GroupA", q1.MutualExclusionGroup);

        var q3 = _questManager.GetDefinition(3);
        Assert.Equal(QuestRepeatability.Daily, q3.Repeatability);
    }

    [Fact]
    public void MutualExclusion_ShouldPreventConcurrentQuests()
    {
        // Start Q1
        Assert.True(_playerQuests.StartQuest(1));
        Assert.Equal(QuestState.InProgress, _playerQuests.QuestStates[1]);

        // Try Start Q2 (Same Group) -> Fail
        Assert.False(_playerQuests.StartQuest(2));

        // Complete Q1
        _playerQuests.UpdateProgress(1, 0, 1);
        _playerQuests.ClaimReward(1);
        Assert.Equal(QuestState.RewardClaimed, _playerQuests.QuestStates[1]);

        // Now Start Q2 -> Success
        Assert.True(_playerQuests.StartQuest(2));
    }

    [Fact]
    public void DailyQuest_ShouldRespectCooldown()
    {
        // 1. Start and Complete
        _playerQuests.StartQuest(3);
        _playerQuests.UpdateProgress(3, 0, 1);
        _playerQuests.ClaimReward(3);

        // 2. Try start again immediately -> Fail
        Assert.False(_playerQuests.StartQuest(3));
    }

    [Fact]
    public void CooldownQuest_ShouldWait()
    {
        // 1. Start and Complete
        _playerQuests.StartQuest(4);
        _playerQuests.UpdateProgress(4, 0, 1);
        _playerQuests.ClaimReward(4);

        // 2. Try start again immediately -> Fail
        Assert.False(_playerQuests.StartQuest(4));

        // 3. Wait 1.1s
        Thread.Sleep(1100);

        // 4. Try start again -> Success
        Assert.True(_playerQuests.StartQuest(4));
    }

    [Fact]
    public void FailQuest_ShouldSetStateToFailed()
    {
        _playerQuests.StartQuest(1);
        Assert.Equal(QuestState.InProgress, _playerQuests.QuestStates[1]);

        _playerQuests.FailQuest(1);
        Assert.Equal(QuestState.Failed, _playerQuests.QuestStates[1]);

        // Should be able to restart if failed (based on current logic)
        Assert.True(_playerQuests.StartQuest(1));
        Assert.Equal(QuestState.InProgress, _playerQuests.QuestStates[1]);
    }
}