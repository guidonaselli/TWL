using System.Text.Json;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;

namespace TWL.Tests.Quests;

public class TimeLimitedQuestTests
{
    private readonly PlayerQuestComponent _playerQuests;
    private readonly ServerQuestManager _questManager;

    public TimeLimitedQuestTests()
    {
        _questManager = new ServerQuestManager();
        // Create mock data
        var quests = new List<QuestDefinition>
        {
            new()
            {
                QuestId = 1,
                Title = "Timed Quest",
                Description = "Run!",
                Objectives = new List<ObjectiveDefinition>
                {
                    new("Talk", "Target", 1, "Talk")
                },
                Rewards = new RewardDefinition(100, 0, new List<ItemReward>()),
                TimeLimitSeconds = 1 // Very short limit
            }
        };

        var json = JsonSerializer.Serialize(quests);
        File.WriteAllText("test_quests_timed.json", json);
        _questManager.Load("test_quests_timed.json");

        _playerQuests = new PlayerQuestComponent(_questManager);
    }

    [Fact]
    public void StartQuest_ShouldRecordStartTime()
    {
        _playerQuests.StartQuest(1);
        Assert.True(_playerQuests.QuestStartTimes.ContainsKey(1));
        Assert.True((DateTime.UtcNow - _playerQuests.QuestStartTimes[1]).TotalSeconds < 5);
    }

    [Fact]
    public void UpdateProgress_ShouldFail_IfTimeExpired()
    {
        _playerQuests.StartQuest(1);

        // Simulate time passing (wait 1.1s)
        Thread.Sleep(1100);

        // Try progress triggers CheckFailures
        _playerQuests.TryProgress("Talk", "Target");

        Assert.Equal(QuestState.Failed, _playerQuests.QuestStates[1]);
    }

    [Fact]
    public void UpdateProgress_ShouldSucceed_IfWithinTime()
    {
        _playerQuests.StartQuest(1);

        // Immediate progress
        _playerQuests.TryProgress("Talk", "Target");

        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[1]);
    }
}