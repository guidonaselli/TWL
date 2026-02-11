using Moq;
using TWL.Server.Services.World;
using TWL.Server.Services.World.Handlers;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Models;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;
using TWL.Server.Persistence.Services;

namespace TWL.Tests.Quests;

public class QuestSystemExtendedTests
{
    private readonly PlayerQuestComponent _playerQuests;
    private readonly ServerQuestManager _questManager;
    private readonly ServerCharacter _character;

    public QuestSystemExtendedTests()
    {
        _questManager = new ServerQuestManager();
        _character = new ServerCharacter { Id = 1, Name = "TestPlayer" };

        var quests = new List<QuestDefinition>
        {
            new()
            {
                QuestId = 100,
                Title = "Pet Quest",
                Description = "Requires Pet",
                RequiredPetId = 999,
                Objectives = new List<ObjectiveDefinition>
                {
                    new("Talk", "Trainer", 1, "Talk")
                },
                Rewards = new RewardDefinition(0, 0, new List<ItemReward>())
            },
            new()
            {
                QuestId = 101,
                Title = "Death Quest",
                Description = "Don't die",
                Objectives = new List<ObjectiveDefinition>
                {
                    new("Kill", "Wolf", 1, "Kill Wolf")
                },
                Rewards = new RewardDefinition(0, 0, new List<ItemReward>()),
                FailConditions = new List<QuestFailCondition>
                {
                    new("PlayerDeath", "", "Die")
                }
            }
        };

        // Hack to load quests into manager without file
        var json = System.Text.Json.JsonSerializer.Serialize(quests);
        File.WriteAllText("extended_quests.json", json);
        _questManager.Load("extended_quests.json");

        _playerQuests = new PlayerQuestComponent(_questManager);
        _playerQuests.Character = _character;
    }

    [Fact]
    public void StartQuest_ShouldFail_WhenRequiredPetMissing()
    {
        var result = _playerQuests.StartQuest(100);
        Assert.False(result);
    }

    [Fact]
    public void StartQuest_ShouldSucceed_WhenRequiredPetPresent()
    {
        _character.AddPet(new ServerPet { DefinitionId = 999, InstanceId = "1" });
        var result = _playerQuests.StartQuest(100);
        Assert.True(result);
    }

    [Fact]
    public void Quest_ShouldFail_OnPlayerDeath()
    {
        _playerQuests.StartQuest(101);
        Assert.Equal(QuestState.InProgress, _playerQuests.QuestStates[101]);

        // Simulate Player Death
        _playerQuests.HandleCombatantDeath("TestPlayer");

        Assert.Equal(QuestState.Failed, _playerQuests.QuestStates[101]);
    }

    [Fact]
    public void Quest_ShouldNotFail_OnOtherDeath()
    {
        _playerQuests.StartQuest(101);

        // Simulate Other Death
        _playerQuests.HandleCombatantDeath("AnotherPlayer");

        Assert.Equal(QuestState.InProgress, _playerQuests.QuestStates[101]);
    }

    [Fact]
    public void Validation_ShouldDetect_CircularDependency()
    {
        var quests = new List<QuestDefinition>
        {
            new() { QuestId = 1, Title = "A", Description="A", Objectives = [new("T","T",1,"D")], Rewards = new(0,0,[]), Requirements = [2] },
            new() { QuestId = 2, Title = "B", Description="B", Objectives = [new("T","T",1,"D")], Rewards = new(0,0,[]), Requirements = [1] }
        };

        var errors = QuestValidator.Validate(quests);
        Assert.Contains(errors, e => e.Contains("Circular dependency"));
    }

    [Fact]
    public void Validation_ShouldDetect_MutualExclusionConflict()
    {
        var quests = new List<QuestDefinition>
        {
            new() { QuestId = 1, Title = "A", Description="A", Objectives = [new("T","T",1,"D")], Rewards = new(0,0,[]), MutualExclusionGroup = "G1" },
            new() { QuestId = 2, Title = "B", Description="B", Objectives = [new("T","T",1,"D")], Rewards = new(0,0,[]), MutualExclusionGroup = "G1", Requirements = [1] }
        };

        var errors = QuestValidator.Validate(quests);
        Assert.Contains(errors, e => e.Contains("logical deadlock"));
    }
}
