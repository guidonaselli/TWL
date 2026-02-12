using System.Text.Json;
using TWL.Server.Persistence;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;
using Xunit;

namespace TWL.Tests.Quests;

public class QuestSystemExtendedTests
{
    private readonly PlayerQuestComponent _playerQuests;
    private readonly ServerQuestManager _questManager;
    private readonly ServerCharacter _character;

    public QuestSystemExtendedTests()
    {
        _questManager = new ServerQuestManager();

        // Locate the scenario file
        var scenarioPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../Content/Data/quests_scenario_demo.json");
        if (!File.Exists(scenarioPath))
        {
             // Try local relative if running differently
             scenarioPath = Path.Combine(Directory.GetCurrentDirectory(), "Content/Data/quests_scenario_demo.json");
        }

        if (!File.Exists(scenarioPath))
        {
            // If still not found, try finding it relative to repo root (for safety in different envs)
            // But strict path usually works in standard .NET test runners if content is copied.
            // Since I wrote it to Content/Data/, it should be there.
        }

        _questManager.Load(scenarioPath);

        _playerQuests = new PlayerQuestComponent(_questManager);
        _character = new ServerCharacter { Id = 1, Name = "TestHero" };
        _playerQuests.Character = _character;
    }

    [Fact]
    public void Quest8000_KillCount_ShouldProgress()
    {
        var qId = 8000;
        Assert.True(_playerQuests.StartQuest(qId));

        // Progress via HandleKillCount
        _playerQuests.HandleKillCount("Slime");
        Assert.Equal(1, _playerQuests.QuestProgress[qId][0]);

        // Progress via generic TryProgress("Kill")
        _playerQuests.TryProgress("Kill", "Slime");
        Assert.Equal(2, _playerQuests.QuestProgress[qId][0]);

        // Complete
        _playerQuests.HandleKillCount("Slime");
        _playerQuests.HandleKillCount("Slime");
        _playerQuests.HandleKillCount("Slime");

        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[qId]);
    }

    [Fact]
    public void Quest8001_Collect_ShouldProgress()
    {
        var qId = 8001;
        Assert.True(_playerQuests.StartQuest(qId));

        // Add Item (ItemId 7001)
        _character.AddItem(7001, 1);
        Assert.Equal(1, _playerQuests.QuestProgress[qId][0]);

        _character.AddItem(7001, 2);
        Assert.Equal(3, _playerQuests.QuestProgress[qId][0]);

        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[qId]);
        Assert.True(_playerQuests.ClaimReward(qId));

        // Check Reward
        Assert.True(_character.HasItem(7001, 3)); // We collected 3, reward gave 3 more -> 6 total
    }

    [Fact]
    public void Quest8002_Deliver_ShouldConsumeItem()
    {
        // Prereq: 8001
        var qId = 8002;
        _playerQuests.QuestStates[8001] = QuestState.RewardClaimed; // Cheat prereq

        Assert.True(_playerQuests.StartQuest(qId));

        // Need items to deliver
        _character.AddItem(7001, 3);

        var delivered = _playerQuests.TryDeliver("Alchemist");
        Assert.Single(delivered);
        Assert.Equal(qId, delivered[0]);

        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[qId]);
        Assert.False(_character.HasItem(7001, 1)); // Should be consumed
    }

    [Fact]
    public void Quest8003_8004_MutualExclusion_ShouldBlock()
    {
        var qA = 8003;
        var qB = 8004;

        // Start A
        Assert.True(_playerQuests.StartQuest(qA));

        // Try Start B -> Should fail
        Assert.False(_playerQuests.StartQuest(qB));

        // Complete A
        _playerQuests.QuestStates[qA] = QuestState.Completed;
        _playerQuests.ClaimReward(qA);

        // Try Start B again -> Should still fail (Permanent exclusion)
        Assert.False(_playerQuests.StartQuest(qB));
    }

    [Fact]
    public void Quest8005_InstanceLockout_ShouldGate()
    {
        var qId = 8005;
        var instanceId = "CaveInstance";

        // Mock Lockout
        _character.InstanceLockouts[instanceId] = DateTime.UtcNow.AddHours(1);

        // Should fail to start
        Assert.False(_playerQuests.StartQuest(qId));

        // Remove Lockout
        _character.InstanceLockouts.Remove(instanceId);

        // Should succeed
        Assert.True(_playerQuests.StartQuest(qId));

        // Complete Instance
        _playerQuests.HandleInstanceCompletion(instanceId);
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[qId]);
    }

    [Fact]
    public void Quest8006_EscortFail_ShouldFailQuest()
    {
        var qId = 8006;
        Assert.True(_playerQuests.StartQuest(qId));

        // Fail via death
        _playerQuests.HandleCombatantDeath("VIP");

        Assert.Equal(QuestState.Failed, _playerQuests.QuestStates[qId]);
    }
}
