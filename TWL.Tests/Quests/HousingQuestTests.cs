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
        string path = "../../../../TWL.Server/Content/Data/quests.json";
        if (!File.Exists(path))
        {
             // Try valid fallback if running from root
             path = "TWL.Server/Content/Data/quests.json";
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
    public void HousingAuthorization_Quest_Should_Complete_With_PayGold()
    {
        int questId = 9000;

        // 1. Start Quest
        Assert.True(_playerQuests.CanStartQuest(questId), "Should be able to start 9000 at level 10+");
        Assert.True(_playerQuests.StartQuest(questId));
        Assert.Equal(QuestState.InProgress, _playerQuests.QuestStates[questId]);

        // 2. Talk to Estate Agent
        var updated = _playerQuests.TryProgress("Talk", "Estate Agent");
        Assert.Contains(questId, updated);

        // 3. Collect Materials
        // Add Wood (7316) x 10
        // We simulate Item Added event by calling AddItem
        Assert.True(_character.AddItem(7316, 10)); // Should trigger OnItemAdded -> UpdateProgress

        // Add Iron Ore (7301) x 5
        Assert.True(_character.AddItem(7301, 5));

        // Verify progress for Items
        // Indexes: 0=Talk, 1=Collect Wood, 2=Collect Iron, 3=PayGold
        Assert.Equal(10, _playerQuests.QuestProgress[questId][1]);
        Assert.Equal(5, _playerQuests.QuestProgress[questId][2]);

        // 4. Pay Gold
        // Give player Gold
        _character.AddGold(1000); // Has 1000
        Assert.Equal(1000, _character.Gold);

        // Interact with Estate Agent to deliver/pay
        // TryDeliver checks for "Deliver" AND "PayGold" objectives
        updated = _playerQuests.TryDeliver("Estate Agent");
        Assert.Contains(questId, updated);

        // Verify Gold Deducted (500 required)
        Assert.Equal(500, _character.Gold);
        Assert.Equal(500, _playerQuests.QuestProgress[questId][3]);

        // Verify Completion
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[questId]);

        // 5. Claim Reward
        Assert.True(_playerQuests.ClaimReward(questId));
        Assert.Equal(QuestState.RewardClaimed, _playerQuests.QuestStates[questId]);

        // Check Flag
        Assert.Contains("HOUSING_UNLOCKED", _playerQuests.Flags);

        // Check Item Reward (Tent ID 800)
        Assert.True(_character.HasItem(800, 1));
    }
}
