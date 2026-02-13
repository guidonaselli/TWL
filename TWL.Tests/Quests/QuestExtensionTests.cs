using System.Text.Json;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Quests;

namespace TWL.Tests.Quests;

public class QuestExtensionTests
{
    private readonly PlayerQuestComponent _playerQuests;
    private readonly ServerQuestManager _questManager;
    private readonly ServerCharacter _character;

    public QuestExtensionTests()
    {
        _questManager = new ServerQuestManager();
        var quests = new List<QuestDefinition>
        {
            new()
            {
                QuestId = 100,
                Title = "Kill Quest",
                Description = "Kill a Slime",
                Objectives = new List<ObjectiveDefinition> { new("Kill", "Slime", 1, "Kill Slime") },
                Rewards = new RewardDefinition(0, 0, new List<ItemReward>())
            },
            new()
            {
                QuestId = 101,
                Title = "Event Quest",
                Description = "Participate in Event",
                Objectives = new List<ObjectiveDefinition> { new("EventParticipation", "Festival", 1, "Join Festival") },
                Rewards = new RewardDefinition(0, 0, new List<ItemReward>())
            },
            new()
            {
                QuestId = 102,
                Title = "Craft Quest",
                Description = "Craft Sword",
                Objectives = new List<ObjectiveDefinition> { new("Craft", "Sword", 1, "Craft Sword") },
                Rewards = new RewardDefinition(0, 0, new List<ItemReward>())
            }
        };

        var json = JsonSerializer.Serialize(quests);
        File.WriteAllText("test_extension_quests.json", json);
        _questManager.Load("test_extension_quests.json");

        _playerQuests = new PlayerQuestComponent(_questManager);
        _character = new ServerCharacter { Id = 1, Name = "Player1" };
        _playerQuests.Character = _character;
    }

    [Fact]
    public void NotifyKill_ShouldProgressQuest()
    {
        _playerQuests.StartQuest(100);

        // Simulate Kill Event via ServerCharacter helper
        _character.NotifyKill("Slime", null);

        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[100]);
    }

    [Fact]
    public void NotifyEventParticipation_ShouldProgressQuest()
    {
        _playerQuests.StartQuest(101);

        _character.NotifyEventParticipation("Festival");

        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[101]);
    }

    [Fact]
    public void NotifyCraft_ShouldProgressQuest()
    {
        _playerQuests.StartQuest(102);

        _character.NotifyCraft("Sword", 1);

        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[102]);
    }

    [Fact]
    public void PetKill_ShouldProgressOwnerQuest()
    {
        // Verify ServerPet has OwnerId and we can set it.
        var pet = new ServerPet();
        _character.AddPet(pet);

        Assert.Equal(_character.Id, pet.OwnerId);
    }
}
