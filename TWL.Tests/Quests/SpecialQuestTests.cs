using System.Collections.Generic;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;
using Xunit;
using System.IO;
using System.Text.Json;

namespace TWL.Tests.Quests;

public class SpecialQuestTests
{
    private readonly ServerQuestManager _questManager;
    private readonly PlayerQuestComponent _playerQuests;

    public SpecialQuestTests()
    {
        _questManager = new ServerQuestManager();

        var quests = new List<QuestDefinition>
        {
            // Quest 1: Normal Quest
            new QuestDefinition
            {
                QuestId = 1,
                Title = "Normal Quest",
                Description = "Desc",
                Requirements = new List<int>(),
                Objectives = new List<ObjectiveDefinition> { new ObjectiveDefinition("Talk", "Npc", 1, "Talk") },
                Rewards = new RewardDefinition(100, 0, new List<ItemReward>())
            },
            // Quest 100: Unique Per Character Quest (e.g. Legendary Start)
            new QuestDefinition
            {
                QuestId = 100,
                Title = "Unique Quest",
                Description = "Desc",
                Requirements = new List<int>(),
                Objectives = new List<ObjectiveDefinition> { new ObjectiveDefinition("Talk", "Npc", 1, "Talk") },
                Rewards = new RewardDefinition(100, 0, new List<ItemReward>()),
                AntiAbuseRules = "UniquePerCharacter",
                Repeatability = QuestRepeatability.None
            },
            // Quest 201: Special Category Dragon A
            new QuestDefinition
            {
                QuestId = 201,
                Title = "Dragon Quest A",
                Description = "Desc",
                Requirements = new List<int>(),
                Objectives = new List<ObjectiveDefinition> { new ObjectiveDefinition("Talk", "Dragon", 1, "Talk") },
                Rewards = new RewardDefinition(100, 0, new List<ItemReward>()),
                Type = "SpecialSkill",
                SpecialCategory = "Dragon",
                MutualExclusionGroup = "GlobalSpecial"
            },
            // Quest 202: Special Category Dragon B
            new QuestDefinition
            {
                QuestId = 202,
                Title = "Dragon Quest B",
                Description = "Desc",
                Requirements = new List<int>(),
                Objectives = new List<ObjectiveDefinition> { new ObjectiveDefinition("Talk", "Dragon", 1, "Talk") },
                Rewards = new RewardDefinition(100, 0, new List<ItemReward>()),
                Type = "SpecialSkill",
                SpecialCategory = "Dragon",
                MutualExclusionGroup = "GlobalSpecial"
            },
            // Quest 301: Special Category Rebirth
            new QuestDefinition
            {
                QuestId = 301,
                Title = "Rebirth Quest",
                Description = "Desc",
                Requirements = new List<int>(),
                Objectives = new List<ObjectiveDefinition> { new ObjectiveDefinition("Talk", "Angel", 1, "Talk") },
                Rewards = new RewardDefinition(100, 0, new List<ItemReward>()),
                Type = "SpecialSkill",
                SpecialCategory = "Rebirth",
                MutualExclusionGroup = "GlobalSpecial"
            }
        };

        string json = JsonSerializer.Serialize(quests);
        File.WriteAllText("test_special_quests.json", json);
        _questManager.Load("test_special_quests.json");

        _playerQuests = new PlayerQuestComponent(_questManager);
    }

    [Fact]
    public void UniquePerCharacter_ShouldBlockRestart_EvenIfAbandonedOrCompleted()
    {
        // 1. Start Unique Quest
        Assert.True(_playerQuests.StartQuest(100), "Should start unique quest first time");
        Assert.Equal(QuestState.InProgress, _playerQuests.QuestStates[100]);

        // 2. Try to start again while InProgress (should fail standard logic anyway, but good to check)
        Assert.False(_playerQuests.StartQuest(100), "Should not start while in progress");

        // 3. Complete and Claim
        _playerQuests.UpdateProgress(100, 0, 1);
        _playerQuests.ClaimReward(100);
        Assert.Equal(QuestState.RewardClaimed, _playerQuests.QuestStates[100]);

        // 4. Try to start again (Repeatable is false, AND AntiAbuse is set)
        Assert.False(_playerQuests.StartQuest(100), "Should not start after completion");

        // 5. Simulate data hack or bug where state is removed but log remains
        Assert.True(_playerQuests.QuestCompletionTimes.ContainsKey(100), "Completion log should exist");

        // We manually remove the state key to simulate data loss/hack
        _playerQuests.QuestStates.Remove(100);

        // Even though state is gone, the CompletionLog (QuestCompletionTimes) should remain
        // and UniquePerCharacter rule should still block it.
        Assert.False(_playerQuests.StartQuest(100), "Should not start even if state is hacked/removed, because log remains");
    }

    [Fact]
    public void SpecialCategory_ShouldEnforceExclusivity()
    {
        // 1. Start Dragon Quest A
        Assert.True(_playerQuests.StartQuest(201), "Should start Dragon Quest A");

        // 2. Try to start Dragon Quest B (Same Category) - Should Fail
        Assert.False(_playerQuests.StartQuest(202), "Should fail to start Dragon Quest B while A is active");
        Assert.False(_playerQuests.CanStartQuest(202), "CanStartQuest should also return false");

        // 3. Try to start Rebirth Quest (Different Special Category) - Should Fail (Global Special Exclusivity)
        Assert.False(_playerQuests.StartQuest(301), "Should fail to start Rebirth Quest while Dragon Quest A is active");

        // 4. Complete Dragon Quest A
        _playerQuests.UpdateProgress(201, 0, 1);
        _playerQuests.ClaimReward(201);
        Assert.Equal(QuestState.RewardClaimed, _playerQuests.QuestStates[201]);

        // 5. Now Start Dragon Quest B - Should Succeed (A is no longer InProgress)
        Assert.True(_playerQuests.StartQuest(202), "Should allow Dragon Quest B after A is finished");

        // 6. Try Rebirth Quest again - Should Fail
        Assert.False(_playerQuests.StartQuest(301), "Should fail Rebirth while Dragon B is active");
    }

    [Fact]
    public void NormalQuests_ShouldNotBeAffectedBySpecialExclusivity()
    {
        // 1. Start Special Quest
        _playerQuests.StartQuest(201);

        // 2. Start Normal Quest - Should Succeed
        Assert.True(_playerQuests.StartQuest(1), "Normal quest should start even if Special is active");

        // 3. Start another Normal Quest (simulate via logic, though here we only have one)
        // ...
    }
}
