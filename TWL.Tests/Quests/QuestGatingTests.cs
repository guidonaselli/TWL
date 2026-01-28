using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Quests;
using Xunit;

namespace TWL.Tests.Quests;

public class QuestGatingTests
{
    private readonly ServerQuestManager _questManager;
    private readonly PlayerQuestComponent _playerQuests;
    private readonly ServerCharacter _character;
    private readonly string _testFilePath;

    public QuestGatingTests()
    {
        _testFilePath = Path.GetTempFileName();

        var testQuests = new List<QuestDefinition>
        {
            new QuestDefinition
            {
                QuestId = 9001,
                Title = "Level Gate",
                Description = "Req Level 5",
                RequiredLevel = 5,
                Objectives = new List<ObjectiveDefinition> { new ObjectiveDefinition("Talk", "T", 1, "D") },
                Rewards = new RewardDefinition(0,0, new List<ItemReward>())
            },
            new QuestDefinition
            {
                QuestId = 9002,
                Title = "Stat Gate",
                Description = "Req Str 10",
                RequiredStats = new Dictionary<string, int> { { "Str", 10 } },
                Objectives = new List<ObjectiveDefinition> { new ObjectiveDefinition("Talk", "T", 1, "D") },
                Rewards = new RewardDefinition(0,0, new List<ItemReward>())
            },
            new QuestDefinition
            {
                QuestId = 9003,
                Title = "Item Gate",
                Description = "Req Item 100 x 1",
                RequiredItems = new List<ItemRequirement> { new ItemRequirement(100, 1) },
                Objectives = new List<ObjectiveDefinition> { new ObjectiveDefinition("Talk", "T", 1, "D") },
                Rewards = new RewardDefinition(0,0, new List<ItemReward>())
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
    public void Should_Fail_If_Level_Too_Low()
    {
        // Level 1 vs Req 5
        Assert.Equal(1, _character.Level);
        Assert.False(_playerQuests.CanStartQuest(9001));

        // Level Up to 5
        _character.AddExp(10000); // Enough to level up
        Assert.True(_character.Level >= 5);

        Assert.True(_playerQuests.CanStartQuest(9001));
    }

    [Fact]
    public void Should_Fail_If_Stats_Too_Low()
    {
        // Str 8 vs Req 10
        _character.Str = 8;
        Assert.False(_playerQuests.CanStartQuest(9002));

        // Increase Str
        _character.Str = 10;
        Assert.True(_playerQuests.CanStartQuest(9002));
    }

    [Fact]
    public void Should_Fail_If_Item_Missing()
    {
        // No item
        Assert.False(_playerQuests.CanStartQuest(9003));

        // Add Item
        _character.AddItem(100, 1);
        Assert.True(_playerQuests.CanStartQuest(9003));
    }
}
