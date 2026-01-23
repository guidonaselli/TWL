using System.Collections.Generic;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;
using Xunit;

namespace TWL.Tests;

public class QuestFlagsTests
{
    private readonly ServerQuestManager _questManager;
    private readonly PlayerQuestComponent _playerQuests;

    public QuestFlagsTests()
    {
        _questManager = new ServerQuestManager();
        // Create mock data
        var quests = new List<QuestDefinition>
        {
            // Quest 1: Normal, sets Flag "F1"
            new QuestDefinition
            {
                QuestId = 1,
                Title = "Flag Setter",
                Description = "Desc",
                Objectives = new List<ObjectiveDefinition>
                {
                    new ObjectiveDefinition("Talk", "Npc1", 1, "Talk")
                },
                Rewards = new RewardDefinition(10, 0, new List<ItemReward>()),
                FlagsSet = new List<string> { "F1" }
            },
            // Quest 2: Requires "F1", clears "F1", sets "F2"
            new QuestDefinition
            {
                QuestId = 2,
                Title = "Flag Gated",
                Description = "Desc",
                RequiredFlags = new List<string> { "F1" },
                Objectives = new List<ObjectiveDefinition>
                {
                    new ObjectiveDefinition("Talk", "Npc2", 1, "Talk")
                },
                Rewards = new RewardDefinition(10, 0, new List<ItemReward>()),
                FlagsClear = new List<string> { "F1" },
                FlagsSet = new List<string> { "F2" }
            },
            // Quest 3: Repeatable
            new QuestDefinition
            {
                QuestId = 3,
                Title = "Repeatable Quest",
                Description = "Desc",
                Objectives = new List<ObjectiveDefinition>
                {
                    new ObjectiveDefinition("Kill", "Rat", 1, "Kill")
                },
                Rewards = new RewardDefinition(5, 0, new List<ItemReward>()),
                Repeatable = true
            }
        };

        string json = System.Text.Json.JsonSerializer.Serialize(quests);
        System.IO.File.WriteAllText("test_flags_quests.json", json);
        _questManager.Load("test_flags_quests.json");

        _playerQuests = new PlayerQuestComponent(_questManager);
    }

    [Fact]
    public void StartQuest_ShouldFail_WhenMissingRequiredFlag()
    {
        bool result = _playerQuests.StartQuest(2); // Requires F1
        Assert.False(result);
    }

    [Fact]
    public void FlagFlow_ShouldWork()
    {
        // 1. Complete Quest 1 to get F1
        _playerQuests.StartQuest(1);
        _playerQuests.TryProgress("Talk", "Npc1");
        _playerQuests.ClaimReward(1);

        Assert.Contains("F1", _playerQuests.Flags);
        Assert.Equal(QuestState.RewardClaimed, _playerQuests.QuestStates[1]);

        // 2. Start Quest 2 (Requires F1)
        bool result = _playerQuests.StartQuest(2);
        Assert.True(result);

        // 3. Complete Quest 2
        _playerQuests.TryProgress("Talk", "Npc2");
        _playerQuests.ClaimReward(2);

        // Verify F1 cleared, F2 set
        Assert.DoesNotContain("F1", _playerQuests.Flags);
        Assert.Contains("F2", _playerQuests.Flags);
    }

    [Fact]
    public void RepeatableQuest_ShouldAllowRestart()
    {
        // 1. Start and Complete
        _playerQuests.StartQuest(3);
        _playerQuests.TryProgress("Kill", "Rat");
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[3]);

        // 2. Claim Reward
        _playerQuests.ClaimReward(3);
        Assert.Equal(QuestState.RewardClaimed, _playerQuests.QuestStates[3]);

        // 3. Start Again
        bool result = _playerQuests.StartQuest(3);
        Assert.True(result);
        Assert.Equal(QuestState.InProgress, _playerQuests.QuestStates[3]);
        Assert.Equal(0, _playerQuests.QuestProgress[3][0]); // Progress reset

        // 4. Progress again
        _playerQuests.TryProgress("Kill", "Rat");
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[3]);
    }

    [Fact]
    public void NonRepeatableQuest_ShouldNotAllowRestart()
    {
        _playerQuests.StartQuest(1);
        _playerQuests.TryProgress("Talk", "Npc1");
        _playerQuests.ClaimReward(1);

        bool result = _playerQuests.StartQuest(1);
        Assert.False(result);
    }
}
