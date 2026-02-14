using System.Text.Json;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;
using Xunit;

namespace TWL.Tests.Quests;

public class EventDrivenQuestTests
{
    private readonly PlayerQuestComponent _playerQuests;
    private readonly ServerQuestManager _questManager;
    private readonly ServerCharacter _character;

    public EventDrivenQuestTests()
    {
        _questManager = new ServerQuestManager();
        // Ensure the file exists before loading, or use the one we created
        if (!File.Exists("Content/Data/quests_events_demo.json"))
        {
            throw new FileNotFoundException("quests_events_demo.json not found");
        }
        _questManager.Load("Content/Data/quests_events_demo.json");

        _playerQuests = new PlayerQuestComponent(_questManager);
        _character = new ServerCharacter { Id = 1, Name = "TestPlayer" };
        _playerQuests.Character = _character;
    }

    [Fact]
    public void WorldFlag_ShouldGateQuest()
    {
        // Quest 5002 requires "FestivalStarted" WorldFlag

        // Ensure prereq 5001 is completed
        _playerQuests.QuestStates[5001] = QuestState.RewardClaimed;
        // Also ensure completion time is set for checks
        _playerQuests.QuestCompletionTimes[5001] = DateTime.UtcNow;

        // Try to start 5002 without flag
        Assert.False(_playerQuests.CanStartQuest(5002), "Should not start without world flag");
        Assert.False(_playerQuests.StartQuest(5002), "StartQuest should return false");

        // Set Flag
        _character.NotifyWorldFlagSet("FestivalStarted");
        Assert.Contains("FestivalStarted", _character.WorldFlags);

        // Try again
        Assert.True(_playerQuests.CanStartQuest(5002), "Should start with world flag");
        Assert.True(_playerQuests.StartQuest(5002), "StartQuest should return true");
    }

    [Fact]
    public void NotifyWorldFlagSet_ShouldProgressQuest()
    {
        // Quest 5001 waits for "FestivalStarted"
        _playerQuests.StartQuest(5001);
        Assert.Equal(QuestState.InProgress, _playerQuests.QuestStates[5001]);

        _character.NotifyWorldFlagSet("FestivalStarted");

        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[5001]);
    }

    [Fact]
    public void NotifyInstanceStarted_ShouldProgressQuest()
    {
        // Setup state to start 5002
        _playerQuests.QuestStates[5001] = QuestState.RewardClaimed;
        _playerQuests.QuestCompletionTimes[5001] = DateTime.UtcNow;
        _character.NotifyWorldFlagSet("FestivalStarted");

        Assert.True(_playerQuests.StartQuest(5002));
        Assert.Equal(QuestState.InProgress, _playerQuests.QuestStates[5002]);

        _character.NotifyInstanceStarted("FestivalInstance");

        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[5002]);
    }

    [Fact]
    public void NotifyInstanceCompleted_ShouldProgressQuest()
    {
        // Setup for 5003
        _playerQuests.QuestStates[5001] = QuestState.RewardClaimed;
        _playerQuests.QuestCompletionTimes[5001] = DateTime.UtcNow;
        _character.NotifyWorldFlagSet("FestivalStarted");
        _playerQuests.QuestStates[5002] = QuestState.RewardClaimed;
        _playerQuests.QuestCompletionTimes[5002] = DateTime.UtcNow;

        Assert.True(_playerQuests.StartQuest(5003));

        _character.NotifyInstanceCompleted("FestivalInstance");

        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[5003]);
    }

    [Fact]
    public void NotifyInstanceFailed_ShouldFailQuest()
    {
        // Setup for 5003
        _playerQuests.QuestStates[5001] = QuestState.RewardClaimed;
        _playerQuests.QuestCompletionTimes[5001] = DateTime.UtcNow;
        _character.NotifyWorldFlagSet("FestivalStarted");
        _playerQuests.QuestStates[5002] = QuestState.RewardClaimed;
        _playerQuests.QuestCompletionTimes[5002] = DateTime.UtcNow;

        Assert.True(_playerQuests.StartQuest(5003));

        _character.NotifyInstanceFailed("FestivalInstance");

        Assert.Equal(QuestState.Failed, _playerQuests.QuestStates[5003]);
    }

    [Fact]
    public void NotifyEscortSuccess_ShouldProgressQuest()
    {
        // Setup for 5004
        _playerQuests.QuestStates[5001] = QuestState.RewardClaimed;
        _playerQuests.QuestCompletionTimes[5001] = DateTime.UtcNow;
        _character.NotifyWorldFlagSet("FestivalStarted");
        _playerQuests.QuestStates[5002] = QuestState.RewardClaimed;
        _playerQuests.QuestCompletionTimes[5002] = DateTime.UtcNow;
        _playerQuests.QuestStates[5003] = QuestState.RewardClaimed;
        _playerQuests.QuestCompletionTimes[5003] = DateTime.UtcNow;

        Assert.True(_playerQuests.StartQuest(5004));

        _character.NotifyEscortSuccess("VIP");

        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[5004]);
    }
}
