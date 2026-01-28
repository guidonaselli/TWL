using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;
using Xunit;

namespace TWL.Tests.Quests;

public class QuestLogicTests
{
    private readonly ServerQuestManager _questManager;
    private readonly PlayerQuestComponent _playerQuests;
    private readonly ServerCharacter _character;
    private readonly string _testFilePath;

    public QuestLogicTests()
    {
        _testFilePath = Path.GetTempFileName();

        var testQuests = new List<QuestDefinition>
        {
            // Quest A: Sets flag "A_DONE"
            new QuestDefinition
            {
                QuestId = 101,
                Title = "Quest A",
                Description = "Quest A Description",
                Objectives = new List<ObjectiveDefinition>
                {
                    new ObjectiveDefinition("Talk", "TargetA", 1, "Talk to A")
                },
                Rewards = new RewardDefinition(0, 0, new List<ItemReward>()),
                FlagsSet = new List<string> { "A_DONE" }
            },
            // Quest B: Blocked by "A_DONE"
            new QuestDefinition
            {
                QuestId = 102,
                Title = "Quest B",
                Description = "Blocked by A",
                Objectives = new List<ObjectiveDefinition> { new ObjectiveDefinition("Talk", "T", 1, "D") },
                Rewards = new RewardDefinition(0, 0, new List<ItemReward>()),
                BlockedByFlags = new List<string> { "A_DONE" }
            },
            // Quest C: Expiry Test
            new QuestDefinition
            {
                QuestId = 103,
                Title = "Quest C",
                Description = "Expired Quest",
                Objectives = new List<ObjectiveDefinition> { new ObjectiveDefinition("Talk", "T", 1, "D") },
                Rewards = new RewardDefinition(0, 0, new List<ItemReward>()),
                Expiry = DateTime.UtcNow.AddMinutes(-10) // Expired 10 mins ago
            },
            // Quest D: Instance Rules (just definition check for now)
            new QuestDefinition
            {
                QuestId = 104,
                Title = "Quest D",
                Description = "Instance Quest",
                Objectives = new List<ObjectiveDefinition> { new ObjectiveDefinition("Talk", "T", 1, "D") },
                Rewards = new RewardDefinition(0, 0, new List<ItemReward>()),
                InstanceRules = new InstanceRules("Inst01", 1, 300, "KillBoss")
            }
        };

        var json = JsonSerializer.Serialize(testQuests);
        File.WriteAllText(_testFilePath, json);

        _questManager = new ServerQuestManager();
        _questManager.Load(_testFilePath);

        _playerQuests = new PlayerQuestComponent(_questManager);
        _character = new ServerCharacter { Id = 1, Name = "TestPlayer" };
        _playerQuests.Character = _character;
    }

    [Fact]
    public void Should_Block_Quest_Start_If_BlockedByFlags_Present()
    {
        // Initially can start Quest B (Flag A_DONE not present)
        Assert.True(_playerQuests.CanStartQuest(102));

        // Start and Complete Quest A
        _playerQuests.StartQuest(101);
        _playerQuests.QuestStates[101] = QuestState.Completed;
        _playerQuests.ClaimReward(101);

        // Verify Flag Set
        Assert.Contains("A_DONE", _playerQuests.Flags);

        // Now Quest B should be blocked
        Assert.False(_playerQuests.CanStartQuest(102));
    }

    [Fact]
    public void Should_Fail_Quest_If_Expired()
    {
        // Start Quest C (Expired) - actually CanStartQuest might not check Expiry for starting,
        // but StartQuest -> CheckFailures -> should mark it failed if it was InProgress.

        // However, if we try to start an expired quest:
        // Current logic: CanStartQuest -> CheckFailures.
        // But CheckFailures only checks active (InProgress) quests.
        // If the quest has Expiry, and we try to start it...
        // Logic in CanStartQuest doesn't explicitly check Expiry for *Starting*, only for active.
        // Usually Expiry implies "Cannot start after date" OR "Fails if active after date".
        // Let's assume the prompt meant "Fails if active".

        // Manually set it to InProgress to simulate it was started before expiry
        _playerQuests.QuestStates[103] = QuestState.InProgress;

        // Trigger CheckFailures via TryProgress or CanStartQuest
        _playerQuests.TryProgress("Talk", "AnyTarget");

        // Should be Failed
        Assert.Equal(QuestState.Failed, _playerQuests.QuestStates[103]);
    }

    [Fact]
    public void Should_Load_InstanceRules()
    {
        var def = _questManager.GetDefinition(104);
        Assert.NotNull(def.InstanceRules);
        Assert.Equal("Inst01", def.InstanceRules.InstanceId);
        Assert.Equal(1, def.InstanceRules.DifficultyLevel);
    }
}
