using System.Text.Json;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Interactions;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;
// Added for QuestState

namespace TWL.Tests.Quests;

public class QuestEngineEventsTests : IDisposable
{
    private readonly InteractionManager _interactionManager;
    private readonly string _interactionsFile = "test_engine_interactions.json";
    private readonly PetManager _petManager;
    private readonly string _petsFile = "test_engine_pets.json";
    private readonly ServerCharacter _player;
    private readonly PlayerQuestComponent _questComponent;
    private readonly ServerQuestManager _questManager;

    private readonly string _questsFile = "test_engine_quests.json";
    private readonly TradeManager _tradeManager;

    public QuestEngineEventsTests()
    {
        // Setup Quests
        var quests = new List<QuestDefinition>
        {
            new()
            {
                QuestId = 1,
                Title = "Pet Friend",
                Description = "Get a pet",
                Objectives = new List<ObjectiveDefinition>
                {
                    new("PetAcquired", "Dog", 1, "Get a Dog") { DataId = 100 }
                },
                Rewards = new RewardDefinition(0, 0, new List<ItemReward>()),
                RequiredLevel = 1,
                Requirements = new List<int>()
            },
            new()
            {
                QuestId = 2,
                Title = "Trader",
                Description = "Trade Iron",
                Objectives = new List<ObjectiveDefinition>
                {
                    new("Trade", "Buyer", 5, "Trade 5 Iron") { DataId = 500 }
                },
                Rewards = new RewardDefinition(0, 0, new List<ItemReward>()),
                RequiredLevel = 1,
                Requirements = new List<int>()
            },
            new()
            {
                QuestId = 3,
                Title = "Compounder",
                Description = "Compound something",
                Objectives = new List<ObjectiveDefinition>
                {
                    new("Compound", "AlchemyTable", 1, "Compound Potion")
                },
                Rewards = new RewardDefinition(0, 0, new List<ItemReward>()),
                RequiredLevel = 1,
                Requirements = new List<int>()
            }
        };
        File.WriteAllText(_questsFile, JsonSerializer.Serialize(quests));

        _questManager = new ServerQuestManager();
        _questManager.Load(_questsFile);
        _petManager = new PetManager();

        _interactionManager = new InteractionManager();
        var interactions = new List<InteractionDefinition>
        {
            new()
            {
                TargetName = "AlchemyTable",
                Type = "Compound",
                RewardItems = new List<ItemReward>()
            }
        };
        File.WriteAllText(_interactionsFile, JsonSerializer.Serialize(interactions));
        _interactionManager.Load(_interactionsFile);

        _tradeManager = new TradeManager();

        _player = new ServerCharacter { Id = 1, Name = "Seller" };
        _questComponent = new PlayerQuestComponent(_questManager, _petManager);
        _questComponent.Character = _player;
    }

    public void Dispose()
    {
        if (File.Exists(_questsFile))
        {
            File.Delete(_questsFile);
        }

        if (File.Exists(_interactionsFile))
        {
            File.Delete(_interactionsFile);
        }
    }

    [Fact]
    public void PetAcquired_ShouldUpdateQuest()
    {
        _questComponent.StartQuest(1);

        // Act
        var pet = new ServerPet { DefinitionId = 100, Name = "Dog" };
        _player.AddPet(pet);

        // Assert
        Assert.Equal(QuestState.Completed, _questComponent.QuestStates[1]);
    }

    [Fact]
    public void Trade_ShouldUpdateQuest()
    {
        _questComponent.StartQuest(2);

        var buyer = new ServerCharacter { Id = 2, Name = "Buyer" };
        _player.AddItem(500, 10); // Give items to sell

        // Act
        var success = _tradeManager.TransferItem(_player, buyer, 500, 5);

        // Assert
        Assert.True(success);
        Assert.Equal(QuestState.Completed, _questComponent.QuestStates[2]);
    }

    [Fact]
    public void Compound_ShouldUpdateQuest()
    {
        _questComponent.StartQuest(3);

        // Act
        // Use InteractHandler logic: interaction returns Type, then passed to QuestComponent
        var type = _interactionManager.ProcessInteraction(_player, _questComponent, "AlchemyTable");
        Assert.Equal("Compound", type);

        // Simulate ServerCharacter event
        _player.NotifyInteract("AlchemyTable", type);

        // Assert
        Assert.Equal(QuestState.Completed, _questComponent.QuestStates[3]);
    }
}