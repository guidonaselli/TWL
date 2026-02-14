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
        // Quest 99502 requires "FestivalStarted" WorldFlag

        // Ensure prereq 99501 is completed
        _playerQuests.QuestStates[99501] = QuestState.RewardClaimed;
        // Also ensure completion time is set for checks
        _playerQuests.QuestCompletionTimes[99501] = DateTime.UtcNow;

        // Try to start 99502 without flag
        Assert.False(_playerQuests.CanStartQuest(99502), "Should not start without world flag");
        Assert.False(_playerQuests.StartQuest(99502), "StartQuest should return false");

        // Set Flag
        _character.NotifyWorldFlagSet("FestivalStarted");
        Assert.Contains("FestivalStarted", _character.WorldFlags);

        // Try again
        Assert.True(_playerQuests.CanStartQuest(99502), "Should start with world flag");
        Assert.True(_playerQuests.StartQuest(99502), "StartQuest should return true");
    }

    [Fact]
    public void NotifyWorldFlagSet_ShouldProgressQuest()
    {
        // Quest 99501 waits for "FestivalStarted"
        _playerQuests.StartQuest(99501);
        Assert.Equal(QuestState.InProgress, _playerQuests.QuestStates[99501]);

        _character.NotifyWorldFlagSet("FestivalStarted");

        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[99501]);
    }

    [Fact]
    public void NotifyInstanceStarted_ShouldProgressQuest()
    {
        // Setup state to start 99502
        _playerQuests.QuestStates[99501] = QuestState.RewardClaimed;
        _playerQuests.QuestCompletionTimes[99501] = DateTime.UtcNow;
        _character.NotifyWorldFlagSet("FestivalStarted");

        Assert.True(_playerQuests.StartQuest(99502));
        Assert.Equal(QuestState.InProgress, _playerQuests.QuestStates[99502]);

        _character.NotifyInstanceStarted("FestivalInstance");

        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[99502]);
    }

    [Fact]
    public void NotifyInstanceCompleted_ShouldProgressQuest()
    {
        // Setup for 99503
        _playerQuests.QuestStates[99501] = QuestState.RewardClaimed;
        _playerQuests.QuestCompletionTimes[99501] = DateTime.UtcNow;
        _character.NotifyWorldFlagSet("FestivalStarted");
        _playerQuests.QuestStates[99502] = QuestState.RewardClaimed;
        _playerQuests.QuestCompletionTimes[99502] = DateTime.UtcNow;

        Assert.True(_playerQuests.StartQuest(99503));

        _character.NotifyInstanceCompleted("FestivalInstance");

        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[99503]);
    }

    [Fact]
    public void NotifyInstanceFailed_ShouldFailQuest()
    {
        // Setup for 99503
        _playerQuests.QuestStates[99501] = QuestState.RewardClaimed;
        _playerQuests.QuestCompletionTimes[99501] = DateTime.UtcNow;
        _character.NotifyWorldFlagSet("FestivalStarted");
        _playerQuests.QuestStates[99502] = QuestState.RewardClaimed;
        _playerQuests.QuestCompletionTimes[99502] = DateTime.UtcNow;

        Assert.True(_playerQuests.StartQuest(99503));

        _character.NotifyInstanceFailed("FestivalInstance");

        Assert.Equal(QuestState.Failed, _playerQuests.QuestStates[99503]);
    }

    [Fact]
    public void NotifyEscortSuccess_ShouldProgressQuest()
    {
        // Setup for 99504
        _playerQuests.QuestStates[99501] = QuestState.RewardClaimed;
        _playerQuests.QuestCompletionTimes[99501] = DateTime.UtcNow;
        _character.NotifyWorldFlagSet("FestivalStarted");
        _playerQuests.QuestStates[99502] = QuestState.RewardClaimed;
        _playerQuests.QuestCompletionTimes[99502] = DateTime.UtcNow;
        _playerQuests.QuestStates[99503] = QuestState.RewardClaimed;
        _playerQuests.QuestCompletionTimes[99503] = DateTime.UtcNow;

        Assert.True(_playerQuests.StartQuest(99504));

        _character.NotifyEscortSuccess("VIP");

        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[99504]);
    }
}
