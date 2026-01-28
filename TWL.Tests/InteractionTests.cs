using System.Collections.Generic;
using System.IO;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Interactions;
using TWL.Shared.Domain.Quests;
using Xunit;

namespace TWL.Tests;

public class InteractionTests
{
    private InteractionManager _manager;
    private ServerCharacter _character;
    private PlayerQuestComponent _questComponent;
    private ServerQuestManager _questManager;

    public InteractionTests()
    {
        _manager = new InteractionManager();
        _character = new ServerCharacter { Id = 1, Name = "TestPlayer" };
        _questManager = new ServerQuestManager();
        // Setup Quest Manager with dummy data
        File.WriteAllText("test_quests.json", "[{\"QuestId\": 1016, \"Title\": \"Test\", \"Description\": \"Test\", \"Objectives\": [{\"Type\":\"Talk\",\"TargetName\":\"Dummy\",\"RequiredCount\":1,\"Description\":\"Dummy\"}], \"Rewards\": {\"Exp\":0,\"Gold\":0,\"Items\":[]}}]");
        _questManager.Load("test_quests.json");
        _questComponent = new PlayerQuestComponent(_questManager);
    }

    [Fact]
    public void ProcessInteraction_ShouldGiveReward_WhenGatherType()
    {
        // Setup Interaction
        var interactions = new List<InteractionDefinition>
        {
            new InteractionDefinition
            {
                TargetName = "Rock",
                Type = "Gather",
                RewardItems = new List<ItemReward> { new ItemReward(201, 1) }
            }
        };
        SaveInteractions(interactions);
        _manager.Load("test_interactions.json");

        bool result = _manager.ProcessInteraction(_character, _questComponent, "Rock");

        Assert.True(result);
        Assert.True(_character.HasItem(201, 1));
    }

    [Fact]
    public void ProcessInteraction_ShouldFail_WhenQuestRequiredButNotActive()
    {
        // Setup Interaction with Requirement
        var interactions = new List<InteractionDefinition>
        {
            new InteractionDefinition
            {
                TargetName = "QuestRock",
                Type = "Gather",
                RewardItems = new List<ItemReward> { new ItemReward(201, 1) },
                RequiredQuestId = 1016
            }
        };
        SaveInteractions(interactions);
        _manager.Load("test_interactions.json");

        // Quest not started
        bool result = _manager.ProcessInteraction(_character, _questComponent, "QuestRock");

        Assert.False(result);
        Assert.False(_character.HasItem(201, 1));
    }

    [Fact]
    public void ProcessInteraction_ShouldSucceed_WhenQuestRequiredAndActive()
    {
        // Setup Interaction with Requirement
        var interactions = new List<InteractionDefinition>
        {
            new InteractionDefinition
            {
                TargetName = "QuestRock",
                Type = "Gather",
                RewardItems = new List<ItemReward> { new ItemReward(201, 1) },
                RequiredQuestId = 1016
            }
        };
        SaveInteractions(interactions);
        _manager.Load("test_interactions.json");

        // Start Quest
        _questComponent.StartQuest(1016);

        bool result = _manager.ProcessInteraction(_character, _questComponent, "QuestRock");

        Assert.True(result);
        Assert.True(_character.HasItem(201, 1));
    }

    [Fact]
    public void ProcessInteraction_ShouldCraft_WhenItemsPresent()
    {
        var interactions = new List<InteractionDefinition>
        {
            new InteractionDefinition
            {
                TargetName = "Bench",
                Type = "Craft",
                RequiredItems = new List<ItemReward> { new ItemReward(201, 1) },
                RewardItems = new List<ItemReward> { new ItemReward(203, 1) }
            }
        };
        SaveInteractions(interactions);
        _manager.Load("test_interactions.json");

        _character.AddItem(201, 1);

        bool result = _manager.ProcessInteraction(_character, _questComponent, "Bench");

        Assert.True(result);
        Assert.False(_character.HasItem(201, 1)); // Consumed
        Assert.True(_character.HasItem(203, 1)); // Rewarded
    }

    [Fact]
    public void ProcessInteraction_ShouldFailCraft_WhenItemsMissing()
    {
        var interactions = new List<InteractionDefinition>
        {
            new InteractionDefinition
            {
                TargetName = "Bench",
                Type = "Craft",
                RequiredItems = new List<ItemReward> { new ItemReward(201, 1) },
                RewardItems = new List<ItemReward> { new ItemReward(203, 1) }
            }
        };
        SaveInteractions(interactions);
        _manager.Load("test_interactions.json");

        bool result = _manager.ProcessInteraction(_character, _questComponent, "Bench");

        Assert.False(result);
        Assert.False(_character.HasItem(203, 1));
    }

    private void SaveInteractions(List<InteractionDefinition> list)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(list);
        File.WriteAllText("test_interactions.json", json);
    }
}
