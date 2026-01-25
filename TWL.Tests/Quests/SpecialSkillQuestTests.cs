using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;

namespace TWL.Tests.Quests;

public class TestClientSession : ClientSession
{
    public TestClientSession() : base() { }

    public void SetCharacter(ServerCharacter c) { Character = c; }
    public void SetQuestComponent(PlayerQuestComponent qc) { QuestComponent = qc; }

    public new void GrantGoddessSkills()
    {
        base.GrantGoddessSkills();
    }
}

public class SpecialSkillQuestTests : IDisposable
{
    private ServerQuestManager _questManager;
    private TestClientSession _session;
    private ServerCharacter _character;
    private string _tempQuestFile;

    public SpecialSkillQuestTests()
    {
        _tempQuestFile = Path.GetTempFileName();
        var quests = new List<QuestDefinition>
        {
            // SSQ 1: Dragon
            new QuestDefinition
            {
                QuestId = 8001,
                Title = "Dragon Quest",
                Description = "Kill Dragon",
                Requirements = new List<int>(),
                Objectives = new List<ObjectiveDefinition>
                {
                    new ObjectiveDefinition("Kill", "Dragon", 1, "Kill the Dragon")
                },
                Rewards = new RewardDefinition(1000, 100, new List<ItemReward>(), null, 8001),
                Type = "SpecialSkill",
                SpecialCategory = "Dragon",
                AntiAbuseRules = "UniquePerCharacter",
                RequiredLevel = 10
            },
            // SSQ 2: Fairy (Different Category)
            new QuestDefinition
            {
                QuestId = 8002,
                Title = "Fairy Quest",
                Description = "Instance Clear",
                Requirements = new List<int>(),
                Objectives = new List<ObjectiveDefinition>
                {
                    new ObjectiveDefinition("Instance", "Fairy Woods", 1, "Clear Fairy Woods")
                },
                Rewards = new RewardDefinition(1000, 100, new List<ItemReward>(), null, 8002),
                Type = "SpecialSkill",
                SpecialCategory = "Fairy",
                AntiAbuseRules = "UniquePerCharacter",
                RequiredLevel = 10
            },
            // SSQ 3: Another Dragon (Same Category) - to test exclusivity
            new QuestDefinition
            {
                QuestId = 8003,
                Title = "Another Dragon Quest",
                Description = "Kill Another Dragon",
                Requirements = new List<int>(),
                Objectives = new List<ObjectiveDefinition>
                {
                    new ObjectiveDefinition("Kill", "Wyvern", 1, "Kill Wyvern")
                },
                Rewards = new RewardDefinition(1000, 100, new List<ItemReward>(), null, 8003),
                Type = "SpecialSkill",
                SpecialCategory = "Dragon",
                AntiAbuseRules = "UniquePerCharacter",
                RequiredLevel = 10
            }
        };

        File.WriteAllText(_tempQuestFile, System.Text.Json.JsonSerializer.Serialize(quests));

        _questManager = new ServerQuestManager();
        _questManager.Load(_tempQuestFile);

        _character = new ServerCharacter
        {
            Id = 1,
            Name = "Tester",
            CharacterElement = Element.Fire,
            // Level 20, // Stats are set via LoadSaveData or default, need to check how to set level manually
            // ServerCharacter sets Level to 1 by default. ExpToNextLevel is calculated.
            // I'll cheat by setting Exp enough to level up, or just bypass StartQuest level check if I can?
            // PlayerQuestComponent CheckGating checks Character.Level.
            // I need to set level. ServerCharacter.Level has private setter.
            // But I can use AddExp to level up.
        };
        _character.AddExp(100000); // Level up
        _character.Str = 50;
        _character.Wis = 50;

        var qc = new PlayerQuestComponent(_questManager);
        qc.Character = _character;

        _session = new TestClientSession();
        _session.SetCharacter(_character);
        _session.SetQuestComponent(qc);
    }

    public void Dispose()
    {
        if (File.Exists(_tempQuestFile))
            File.Delete(_tempQuestFile);
    }

    [Fact]
    public void GrantGoddessSkills_FireElement_GrantsHotfire()
    {
        // Act
        _session.GrantGoddessSkills();

        // Assert
        Assert.Contains(2003, _session.Character.KnownSkills);
        Assert.Contains("GS_GRANTED", _session.QuestComponent.Flags);
    }

    [Fact]
    public void GrantGoddessSkills_Idempotent()
    {
        // Arrange
        _session.GrantGoddessSkills();
        Assert.Contains(2003, _session.Character.KnownSkills);

        // Manually remove skill but keep flag to simulate corruption or just verifying flag check
        lock(_session.Character.KnownSkills) { _session.Character.KnownSkills.Clear(); }

        // Act
        _session.GrantGoddessSkills();

        // Assert
        // Should NOT relearn because flag is present
        Assert.DoesNotContain(2003, _session.Character.KnownSkills);
    }

    [Fact]
    public void GrantGoddessSkills_WindElement_GrantsVanish()
    {
        _character.CharacterElement = Element.Wind;
        // Need to clear flags if re-using character, but here we run in isolation per test class instance usually.
        // But xUnit creates new instance per test.
        _session.GrantGoddessSkills();
        Assert.Contains(2004, _session.Character.KnownSkills);
    }

    [Fact]
    public void StartQuest_UniquePerCharacter_CanOnlyStartOnce()
    {
        // 1. Start Quest 8001
        bool started = _session.QuestComponent.StartQuest(8001);
        Assert.True(started, "Should start first time");

        // 2. Try start again while InProgress
        bool startedAgain = _session.QuestComponent.StartQuest(8001);
        Assert.False(startedAgain, "Should not start while InProgress");

        // 3. Complete and Claim
        _session.QuestComponent.UpdateProgress(8001, 0, 1); // Complete
        _session.QuestComponent.ClaimReward(8001);

        // 4. Try start again after Completion
        bool startedAfterComplete = _session.QuestComponent.StartQuest(8001);
        Assert.False(startedAfterComplete, "Should not start again due to UniquePerCharacter rule");
    }

    [Fact]
    public void StartQuest_SpecialCategory_Exclusivity()
    {
        // 1. Start Dragon Quest 8001
        Assert.True(_session.QuestComponent.StartQuest(8001));

        // 2. Try Start Another Dragon Quest 8003
        // Should fail because another "Dragon" category quest is InProgress
        Assert.False(_session.QuestComponent.StartQuest(8003), "Should not start 8003 while 8001 (same category) is active");

        // 3. Try Start Fairy Quest 8002
        // Should succeed because it is "Fairy" category (different)
        Assert.True(_session.QuestComponent.StartQuest(8002), "Should start 8002 (different category)");
    }

    [Fact]
    public void InstanceCompletion_ProgressesQuest()
    {
        // Start Fairy Quest (Requires "Instance: Fairy Woods")
        _session.QuestComponent.StartQuest(8002);

        // Trigger Instance Completion
        _session.HandleInstanceCompletion("Fairy Woods");

        // Check Progress
        var progress = _session.QuestComponent.QuestProgress[8002];
        Assert.Equal(1, progress[0]);

        // Check State
        Assert.Equal(QuestState.Completed, _session.QuestComponent.QuestStates[8002]);
    }
}
