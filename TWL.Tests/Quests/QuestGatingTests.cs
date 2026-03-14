using System;
using System.Collections.Generic;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Quests;
using Xunit;

namespace TWL.Tests.Quests;

public class QuestGatingTests
{
    private readonly ServerQuestManager _questManager;
    private readonly PlayerQuestComponent _questComponent;
    private readonly ServerCharacter _character;

    public QuestGatingTests()
    {
        _questManager = new ServerQuestManager();
        _questComponent = new PlayerQuestComponent(_questManager);
        _character = new ServerCharacter { Id = 1, Level = 10, RebirthLevel = 0 };
        _questComponent.Character = _character;
    }

    [Fact]
    public void CanStartQuest_Rejects_WhenRebirthLevelTooLow()
    {
        // Arrange
        var quest = new QuestDefinition
        {
            QuestId = 9001,
            Title = "Rebirth Only Quest",
            Description = "Only for reborn heroes.",
            RequiredRebirthLevel = 1,
            Objectives = new List<ObjectiveDefinition>
            {
                new ObjectiveDefinition("Talk", "Elder", 1, "Talk to the Elder")
            },
            Rewards = new RewardDefinition(0, 0, new List<ItemReward>())
        };
        _questManager.AddQuest(quest);

        // Act
        bool canStart0 = _questComponent.CanStartQuest(9001);
        
        _character.RebirthLevel = 1;
        bool canStart1 = _questComponent.CanStartQuest(9001);

        // Assert
        Assert.False(canStart0);
        Assert.True(canStart1);
    }

    [Fact]
    public void CanStartQuest_Accepts_WhenNoRebirthRequired()
    {
        // Arrange
        var quest = new QuestDefinition
        {
            QuestId = 9002,
            Title = "Normal Quest",
            Description = "For everyone.",
            RequiredRebirthLevel = 0,
            Objectives = new List<ObjectiveDefinition>
            {
                new ObjectiveDefinition("Talk", "Elder", 1, "Talk to the Elder")
            },
            Rewards = new RewardDefinition(0, 0, new List<ItemReward>())
        };
        _questManager.AddQuest(quest);

        // Act
        bool canStart = _questComponent.CanStartQuest(9002);

        // Assert
        Assert.True(canStart);
    }
}
