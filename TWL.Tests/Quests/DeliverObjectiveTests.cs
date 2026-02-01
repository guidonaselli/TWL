using System.Text.Json;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;

namespace TWL.Tests.Quests;

public class DeliverObjectiveTests
{
    private readonly ServerCharacter _character;
    private readonly PlayerQuestComponent _playerQuests;
    private readonly ServerQuestManager _questManager;

    public DeliverObjectiveTests()
    {
        _questManager = new ServerQuestManager();

        // Define a custom quest for testing
        var definitions = new List<QuestDefinition>
        {
            new()
            {
                QuestId = 9999,
                Title = "Test Deliver",
                Description = "Deliver wood",
                Objectives = new List<ObjectiveDefinition>
                {
                    new("Deliver", "TestNPC", 3, "Deliver 3 Wood") { DataId = 7316 }
                },
                Rewards = new RewardDefinition(10, 0, new List<ItemReward>())
            }
        };

        var json = JsonSerializer.Serialize(definitions);
        var tempPath = Path.GetTempFileName();
        File.WriteAllText(tempPath, json);
        _questManager.Load(tempPath);
        File.Delete(tempPath);

        _playerQuests = new PlayerQuestComponent(_questManager);
        _character = new ServerCharacter { Id = 1, Name = "TestPlayer" };
        _playerQuests.Character = _character;
    }

    [Fact]
    public void TryDeliver_ShouldRemoveItems_AndProgressQuest()
    {
        // Start Quest
        Assert.True(_playerQuests.StartQuest(9999));

        // Add 5 Wood to inventory
        _character.AddItem(7316, 5);
        Assert.True(_character.HasItem(7316, 5));

        // Attempt Deliver
        var updated = _playerQuests.TryDeliver("TestNPC");

        // Verify result
        Assert.Single(updated);
        Assert.Equal(9999, updated[0]);

        // Check Progress: Should be 3/3 (Completed)
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[9999]);
        Assert.Equal(3, _playerQuests.QuestProgress[9999][0]);

        // Check Inventory: Should have 2 left (5 - 3)
        var items = _character.GetItems(7316);
        Assert.Single(items);
        Assert.Equal(2, items[0].Quantity);
    }

    [Fact]
    public void TryDeliver_ShouldNotProgress_IfItemMissing()
    {
        Assert.True(_playerQuests.StartQuest(9999));

        // No items
        var updated = _playerQuests.TryDeliver("TestNPC");

        Assert.Empty(updated);
        Assert.Equal(0, _playerQuests.QuestProgress[9999][0]);
    }

    [Fact]
    public void TryDeliver_ShouldPartialProgress_IfNotEnoughItems()
    {
        Assert.True(_playerQuests.StartQuest(9999));

        // Add 1 Wood (Need 3)
        _character.AddItem(7316, 1);

        var updated = _playerQuests.TryDeliver("TestNPC");

        Assert.Single(updated);
        Assert.Equal(1, _playerQuests.QuestProgress[9999][0]); // 1/3
        Assert.Equal(QuestState.InProgress, _playerQuests.QuestStates[9999]);

        // Inventory should be empty
        Assert.False(_character.HasItem(7316, 1));
    }
}