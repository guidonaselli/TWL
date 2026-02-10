using System.Text.Json;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Models;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;
using Xunit;

namespace TWL.Tests.Quests;

public class AdvancedObjectivesTests : IDisposable
{
    private readonly PlayerQuestComponent _playerQuests;
    private readonly ServerQuestManager _questManager;
    private readonly ServerCharacter _character;
    private const string TestFile = "test_quests_advanced.json";

    public AdvancedObjectivesTests()
    {
        _questManager = new ServerQuestManager();
        _character = new ServerCharacter
        {
            Id = 1,
            Name = "TestPlayer",
            MaxInventorySlots = 10
        };

        var quests = new List<QuestDefinition>
        {
            new()
            {
                QuestId = 4001,
                Title = "Show Item Quest",
                Description = "Show a specific item to an NPC",
                Objectives = new List<ObjectiveDefinition>
                {
                    new("ShowItem", "VillageElder", 1, "Show the Ancient Relic", 9999)
                },
                Rewards = new RewardDefinition(100, 0, new List<ItemReward>())
            },
            new()
            {
                QuestId = 4002,
                Title = "Use Item Quest",
                Description = "Use a specific item",
                Objectives = new List<ObjectiveDefinition>
                {
                    new("UseItem", "HealingPotion", 1, "Use a Healing Potion", 8888)
                },
                Rewards = new RewardDefinition(100, 0, new List<ItemReward>())
            },
            new()
            {
                QuestId = 4003,
                Title = "Fishing Quest",
                Description = "Catch a fish",
                Objectives = new List<ObjectiveDefinition>
                {
                    new("Fish", "FishingSpot", 1, "Catch a fish at the spot")
                },
                Rewards = new RewardDefinition(100, 0, new List<ItemReward>())
            }
        };

        var json = JsonSerializer.Serialize(quests);
        File.WriteAllText(TestFile, json);
        _questManager.Load(TestFile);

        _playerQuests = new PlayerQuestComponent(_questManager);
        _playerQuests.Character = _character;
    }

    public void Dispose()
    {
        if (File.Exists(TestFile))
        {
            File.Delete(TestFile);
        }
    }

    [Fact]
    public void ShowItem_ShouldNotProgress_IfItemMissing()
    {
        _playerQuests.StartQuest(4001);

        // Try to interact without the item
        var uniqueUpdates = new HashSet<int>();
        _playerQuests.TryProgress(uniqueUpdates, "VillageElder", "Talk", "Interact");

        Assert.Empty(uniqueUpdates);
        Assert.Equal(QuestState.InProgress, _playerQuests.QuestStates[4001]);
    }

    [Fact]
    public void ShowItem_ShouldProgress_IfItemPresent()
    {
        _playerQuests.StartQuest(4001);

        // Add the required item
        _character.AddItem(9999, 1);

        // Try to interact with the item
        var uniqueUpdates = new HashSet<int>();
        _playerQuests.TryProgress(uniqueUpdates, "VillageElder", "Talk", "Interact");

        Assert.Single(uniqueUpdates);
        Assert.Equal(4001, uniqueUpdates.First());
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[4001]);
    }

    [Fact]
    public void UseItem_ShouldProgress_WhenItemUsed()
    {
        _playerQuests.StartQuest(4002);

        // Simulate using the item
        var updated = _playerQuests.HandleUseItem(8888, "HealingPotion");

        Assert.Single(updated);
        Assert.Equal(4002, updated[0]);
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[4002]);
    }

    [Fact]
    public void Fish_ShouldProgress_WhenFishingEventOccurs()
    {
        _playerQuests.StartQuest(4003);

        // Simulate fishing event (which passes "Fish" as type)
        var uniqueUpdates = new HashSet<int>();
        _playerQuests.TryProgress(uniqueUpdates, "FishingSpot", "Fish");

        Assert.Single(uniqueUpdates);
        Assert.Equal(4003, uniqueUpdates.First());
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[4003]);
    }
}
