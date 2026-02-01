using System.Text.Json;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;

namespace TWL.Tests.Quests;

public class PlayerQuestComponentTests
{
    private readonly PlayerQuestComponent _playerQuests;
    private readonly ServerQuestManager _questManager;

    public PlayerQuestComponentTests()
    {
        _questManager = new ServerQuestManager();
        // Create mock data
        var quests = new List<QuestDefinition>
        {
            new()
            {
                QuestId = 1,
                Title = "Crafting Quest",
                Description = "Craft something",
                Requirements = new List<int>(),
                Objectives = new List<ObjectiveDefinition>
                {
                    new("Craft", "Sword", 1, "Craft a Sword")
                },
                Rewards = new RewardDefinition(100, 0, new List<ItemReward>())
            },
            new()
            {
                QuestId = 2,
                Title = "Compound Quest",
                Description = "Compound something",
                Requirements = new List<int>(),
                Objectives = new List<ObjectiveDefinition>
                {
                    new("Compound", "Potion", 2, "Compound 2 Potions")
                },
                Rewards = new RewardDefinition(100, 0, new List<ItemReward>())
            },
            new()
            {
                QuestId = 3,
                Title = "Escort Quest",
                Description = "Escort someone",
                Requirements = new List<int>(),
                Objectives = new List<ObjectiveDefinition>
                {
                    new("Escort", "Princess", 1, "Escort Princess")
                },
                Rewards = new RewardDefinition(100, 0, new List<ItemReward>())
            }
        };

        var json = JsonSerializer.Serialize(quests);
        File.WriteAllText("test_quests_component.json", json);
        _questManager.Load("test_quests_component.json");

        _playerQuests = new PlayerQuestComponent(_questManager);
    }

    [Fact]
    public void HandleCraft_ShouldUpdateProgress()
    {
        _playerQuests.StartQuest(1);

        var updated = _playerQuests.HandleCraft("Sword");
        Assert.Single(updated);
        Assert.Equal(1, updated[0]);
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[1]);
    }

    [Fact]
    public void HandleCompound_ShouldUpdateProgress()
    {
        _playerQuests.StartQuest(2);

        var updated = _playerQuests.HandleCompound("Potion");
        Assert.Single(updated);
        Assert.Equal(QuestState.InProgress, _playerQuests.QuestStates[2]);
        Assert.Equal(1, _playerQuests.QuestProgress[2][0]);

        updated = _playerQuests.HandleCompound("Potion");
        Assert.Single(updated);
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[2]);
    }

    [Fact]
    public void HandleEscort_ShouldUpdateProgress_OnSuccess()
    {
        _playerQuests.StartQuest(3);

        var updated = _playerQuests.HandleEscort("Princess", true);
        Assert.Single(updated);
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[3]);
    }

    [Fact]
    public void HandleEscort_ShouldNotUpdate_OnFailure()
    {
        _playerQuests.StartQuest(3);

        var updated = _playerQuests.HandleEscort("Princess", false);
        Assert.Empty(updated);
        Assert.Equal(QuestState.InProgress, _playerQuests.QuestStates[3]);
    }
}