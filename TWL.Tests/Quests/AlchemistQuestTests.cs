using System.Collections.Generic;
using System.IO;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;
using Xunit;

namespace TWL.Tests.Quests;

public class AlchemistQuestTests
{
    private readonly ServerQuestManager _questManager;
    private readonly PlayerQuestComponent _playerQuests;

    public AlchemistQuestTests()
    {
        _questManager = new ServerQuestManager();

        // Locate quests.json
        string path = "../../../../TWL.Server/Content/Data/quests.json";
        if (!File.Exists(path))
        {
             // Try valid fallback if running from root
             path = "TWL.Server/Content/Data/quests.json";
        }

        Assert.True(File.Exists(path), $"Quest file not found at {Path.GetFullPath(path)}");
        _questManager.Load(path);

        _playerQuests = new PlayerQuestComponent(_questManager);
    }

    [Fact]
    public void AlchemistChain_ShouldProgress_Correctly()
    {
        // 1. Quest 2001: Strange Herbs
        Assert.True(_playerQuests.CanStartQuest(2001), "Should be able to start 2001");
        Assert.True(_playerQuests.StartQuest(2001));

        // Objective: Talk to Alchemist Elara
        var updated = _playerQuests.TryProgress("Talk", "Alchemist Elara");
        Assert.Single(updated);
        Assert.Equal(2001, updated[0]);
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[2001]);

        _playerQuests.ClaimReward(2001);
        Assert.Equal(QuestState.RewardClaimed, _playerQuests.QuestStates[2001]);

        // 2. Quest 2002: Gathering Ingredients (Requires 2001)
        Assert.True(_playerQuests.CanStartQuest(2002), "Should be able to start 2002");
        Assert.True(_playerQuests.StartQuest(2002));

        // Objective: Collect 5 Green Herbs
        // Test amount progression
        updated = _playerQuests.TryProgress("Collect", "Green Herb", 2);
        Assert.Single(updated);
        Assert.Equal(2, _playerQuests.QuestProgress[2002][0]);
        Assert.Equal(QuestState.InProgress, _playerQuests.QuestStates[2002]);

        updated = _playerQuests.TryProgress("Collect", "Green Herb", 3);
        Assert.Single(updated);
        Assert.Equal(5, _playerQuests.QuestProgress[2002][0]);
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[2002]);

        _playerQuests.ClaimReward(2002);

        // 3. Quest 2003: Basic Brew (Requires 2002)
        Assert.True(_playerQuests.CanStartQuest(2003));
        Assert.True(_playerQuests.StartQuest(2003));

        // Objective: Craft 1 Minor Potion
        updated = _playerQuests.TryProgress("Craft", "Minor Potion", 1);
        Assert.Single(updated);
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[2003]);

        _playerQuests.ClaimReward(2003);

        // 4. Quest 2004: Delivery (Requires 2003)
        Assert.True(_playerQuests.CanStartQuest(2004));
        Assert.True(_playerQuests.StartQuest(2004));

        // Objective: Talk to Guard Captain
        updated = _playerQuests.TryProgress("Talk", "Guard Captain");
        Assert.Single(updated);
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[2004]);
    }

    [Fact]
    public void TryProgress_ShouldUseAmount()
    {
        // Manually start gathering quest
        // Mock state if needed, but easier to just start it properly
        _playerQuests.QuestStates[2001] = QuestState.RewardClaimed; // Prereq

        _playerQuests.StartQuest(2002); // 5 Green Herbs

        // Add 10 at once
        var updated = _playerQuests.TryProgress("Collect", "Green Herb", 10);

        Assert.Single(updated);
        Assert.Equal(5, _playerQuests.QuestProgress[2002][0]); // Should be capped at 5
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[2002]);
    }
}
