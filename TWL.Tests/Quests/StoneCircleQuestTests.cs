using System.Collections.Generic;
using System.IO;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;
using Xunit;

namespace TWL.Tests.Quests;

public class StoneCircleQuestTests
{
    private readonly ServerQuestManager _questManager;
    private readonly PlayerQuestComponent _playerQuests;
    private readonly ServerCharacter _character;

    public StoneCircleQuestTests()
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
        _character = new ServerCharacter { Id = 1, Name = "TestPlayer" };
        _playerQuests.Character = _character;
    }

    [Fact]
    public void StoneCircle_Chain_ShouldProgress_EndToEnd()
    {
        // 1. Quest 1050: Rumors in the Cove
        Assert.True(_playerQuests.CanStartQuest(1050), "Should be able to start 1050");
        Assert.True(_playerQuests.StartQuest(1050));

        // Objective: Talk to Pescador Viejo
        var updated = _playerQuests.TryProgress("Talk", "Pescador Viejo");
        Assert.Single(updated);
        Assert.Equal(1050, updated[0]);
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[1050]);

        _playerQuests.ClaimReward(1050);
        Assert.Equal(QuestState.RewardClaimed, _playerQuests.QuestStates[1050]);

        // 2. Quest 1051: The Source of the Noise (Kill 3 Cave Bat)
        Assert.True(_playerQuests.CanStartQuest(1051), "Should be able to start 1051");
        Assert.True(_playerQuests.StartQuest(1051));

        // Simulate killing 1
        updated = _playerQuests.TryProgress("Kill", "Cave Bat");
        Assert.Single(updated);
        Assert.Equal(1, _playerQuests.QuestProgress[1051][0]);
        Assert.Equal(QuestState.InProgress, _playerQuests.QuestStates[1051]);

        // Simulate killing 2 more
        updated = _playerQuests.TryProgress("Kill", "Cave Bat", 2);
        Assert.Single(updated);
        Assert.Equal(3, _playerQuests.QuestProgress[1051][0]);
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[1051]);

        _playerQuests.ClaimReward(1051);

        // 3. Quest 1052: Ancient Inscriptions (Interact 3 Grabado de Pared)
        Assert.True(_playerQuests.StartQuest(1052));
        updated = _playerQuests.TryProgress("Interact", "Grabado de Pared", 3);
        Assert.Single(updated);
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[1052]);
        _playerQuests.ClaimReward(1052);

        // 4. Quest 1053: The Stone Guardian (Kill 1 Stone Golem)
        Assert.True(_playerQuests.StartQuest(1053));
        updated = _playerQuests.TryProgress("Kill", "Stone Golem");
        Assert.Single(updated);
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[1053]);

        // Claim reward to get Item 7501 (Strange Stone Fragment)
        _playerQuests.ClaimReward(1053);
        _character.AddItem(7501, 1);

        // 5. Quest 1054: A Small Friend (Talk to Ancient Altar)
        Assert.True(_playerQuests.CanStartQuest(1054), "Should meet item requirements for 1054");
        Assert.True(_playerQuests.StartQuest(1054));

        updated = _playerQuests.TryProgress("Talk", "Ancient Altar");
        Assert.Single(updated);
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[1054]);

        // Reward: Pet Unlock.
        _playerQuests.ClaimReward(1054);
    }

    [Fact]
    public void Sidequest_LostSupplies_ShouldWork()
    {
        // Quest 1060: Lost Supplies (Interact Caja a la Deriva)
        Assert.True(_playerQuests.CanStartQuest(1060));
        Assert.True(_playerQuests.StartQuest(1060));

        var updated = _playerQuests.TryProgress("Interact", "Caja a la Deriva");
        Assert.Single(updated);
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[1060]);
    }
}
