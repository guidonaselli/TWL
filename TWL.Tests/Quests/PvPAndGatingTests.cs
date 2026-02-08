using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Models;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;
using Xunit;

namespace TWL.Tests.Quests;

public class PvPAndGatingTests
{
    private readonly ServerCharacter _character;
    private readonly PlayerQuestComponent _questComponent;
    private readonly ServerQuestManager _questManager;

    public PvPAndGatingTests()
    {
        _questManager = new ServerQuestManager();
        _character = new ServerCharacter { Id = 1, Name = "Duelist" };
        // Level up character to meet requirements if any (none for 3100)
        _character.AddExp(5000); // Level up

        _questComponent = new PlayerQuestComponent(_questManager) { Character = _character };

        // Load Real Data
        var contentPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../Content/Data");
        if (!Directory.Exists(contentPath))
        {
             // Fallback for different test runner contexts
            contentPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "Data");
             if (!Directory.Exists(contentPath))
             {
                 // Try one level up if in bin/Debug/net8.0
                 contentPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../Content/Data");
             }
        }

        // Ensure quests.json exists
        if (!File.Exists(Path.Combine(contentPath, "quests.json")))
        {
             // Fallback to absolute path or relative from repo root if running in sandbox
             contentPath = "Content/Data";
        }

        _questManager.Load(Path.Combine(contentPath, "quests.json"));
    }

    [Fact]
    public void Should_Progress_PvPQuest_On_PlayerKill()
    {
        // 1. Start Quest 3100 (Duel Practice)
        Assert.True(_questComponent.StartQuest(3100), "Should start Duel Practice");

        // 2. Simulate PvP Kill
        // Quest 3100 requires 1 PvPKill on "Player"
        _questComponent.TryProgress("PvPKill", "Player");

        // 3. Verify Completion
        Assert.Equal(QuestState.Completed, _questComponent.QuestStates[3100]);
    }

    [Fact]
    public void Should_FailStartQuest_When_PartyRequired_And_NoParty()
    {
        // Prerequisite: Complete 3100
        _questComponent.QuestStates[3100] = QuestState.RewardClaimed;

        // 1. Ensure No Party
        _character.PartyId = null;

        // 2. Try Start Quest 3101 (Teamwork) - Requires Party
        Assert.False(_questComponent.StartQuest(3101), "Should NOT start Teamwork without party");
    }

    [Fact]
    public void Should_StartQuest_When_PartyRequired_And_InParty()
    {
        // Prerequisite: Complete 3100
        _questComponent.QuestStates[3100] = QuestState.RewardClaimed;

        // 1. Set Party
        _character.PartyId = 123;

        // 2. Try Start Quest 3101 (Teamwork)
        Assert.True(_questComponent.StartQuest(3101), "Should start Teamwork with party");
    }

    [Fact]
    public void Should_FailStartQuest_When_GuildRequired_And_NoGuild()
    {
        // Prerequisite: Complete 3101
        _questComponent.QuestStates[3100] = QuestState.RewardClaimed;
        _questComponent.QuestStates[3101] = QuestState.RewardClaimed;

        // 1. Ensure No Guild
        _character.GuildId = null;

        // 2. Try Start Quest 3102 (Guild Service) - Requires Guild
        Assert.False(_questComponent.StartQuest(3102), "Should NOT start Guild Service without guild");
    }

    [Fact]
    public void Should_StartQuest_When_GuildRequired_And_InGuild()
    {
        // Prerequisite: Complete 3101
        _questComponent.QuestStates[3100] = QuestState.RewardClaimed;
        _questComponent.QuestStates[3101] = QuestState.RewardClaimed;

        // 1. Set Guild
        _character.GuildId = 999;

        // 2. Try Start Quest 3102
        Assert.True(_questComponent.StartQuest(3102), "Should start Guild Service with guild");
    }
}
