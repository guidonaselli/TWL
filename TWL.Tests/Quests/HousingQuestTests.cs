using System.Collections.Generic;
using System.IO;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;
using Xunit;

namespace TWL.Tests.Quests;

public class HousingQuestTests
{
    private readonly ServerQuestManager _questManager;
    private readonly PlayerQuestComponent _playerQuests;
    private readonly ServerCharacter _character;

    public HousingQuestTests()
    {
        _questManager = new ServerQuestManager();

        // Locate quests.json
        string path = System.IO.Path.Combine(System.AppContext.BaseDirectory, "Content/Data/quests.json");
        if (!File.Exists(path))
        {
             // Try valid fallback if running from root
             path = System.IO.Path.Combine(System.AppContext.BaseDirectory, "Content/Data/quests.json");
        }

        Assert.True(File.Exists(path), $"Quest file not found at {Path.GetFullPath(path)}");
        _questManager.Load(path);

        _playerQuests = new PlayerQuestComponent(_questManager);
        _character = new ServerCharacter { Id = 1, Name = "TestHomeowner" };

        // Level up to 10 (Required Level)
        _character.AddExp(100000);

        _playerQuests.Character = _character;
    }

    [Fact]
    public void HousingChain_Should_Progress_Correctly()
    {
        // --- QUEST 9001: Housing Dreams ---
        int q1 = 9001;
        Assert.True(_playerQuests.CanStartQuest(q1), "Should start 9001");
        Assert.True(_playerQuests.StartQuest(q1));

        // Talk to Estate Agent
        var updated = _playerQuests.TryProgress("Talk", "Estate Agent");
        Assert.Contains(q1, updated);
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[q1]);
        Assert.True(_playerQuests.ClaimReward(q1));

        // --- QUEST 9002: Building Blocks ---
        int q2 = 9002;
        Assert.True(_playerQuests.CanStartQuest(q2), "Should start 9002 after 9001");
        Assert.True(_playerQuests.StartQuest(q2));

        // Collect Wood (7316) x 10
        Assert.True(_character.AddItem(7316, 10));
        // Collect Iron Ore (7301) x 5
        Assert.True(_character.AddItem(7301, 5));

        // Verify progress
        Assert.Equal(10, _playerQuests.QuestProgress[q2][0]);
        Assert.Equal(5, _playerQuests.QuestProgress[q2][1]);

        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[q2]);
        Assert.True(_playerQuests.ClaimReward(q2));

        // --- QUEST 9003: The Price of Land ---
        int q3 = 9003;
        Assert.True(_playerQuests.CanStartQuest(q3), "Should start 9003 after 9002");
        Assert.True(_playerQuests.StartQuest(q3));

        // Pay Gold
        _character.AddGold(1000);
        updated = _playerQuests.TryDeliver("Estate Agent");
        Assert.Contains(q3, updated);

        Assert.Equal(500, _character.Gold); // 1000 - 500
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[q3]);
        Assert.True(_playerQuests.ClaimReward(q3));

        // --- QUEST 9004: Landowner ---
        int q4 = 9004;
        Assert.True(_playerQuests.CanStartQuest(q4), "Should start 9004 after 9003");
        Assert.True(_playerQuests.StartQuest(q4));

        // Talk to finalize
        updated = _playerQuests.TryProgress("Talk", "Estate Agent");
        Assert.Contains(q4, updated);

        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[q4]);
        Assert.True(_playerQuests.ClaimReward(q4));

        // Verify Final Rewards
        Assert.Contains("HOUSING_UNLOCKED", _playerQuests.Flags);
        Assert.True(_character.HasItem(800, 1));
    }
}
