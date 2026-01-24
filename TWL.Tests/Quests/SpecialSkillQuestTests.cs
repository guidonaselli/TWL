using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Domain.Characters;
using Xunit;
using System.Reflection;

namespace TWL.Tests.Quests;

public class TestClientSession : ClientSession
{
    public TestClientSession() : base() { }

    public void SetComponents(ServerCharacter character, PlayerQuestComponent questComponent)
    {
        Character = character;
        QuestComponent = questComponent;
    }

    public void InvokeGrantGoddessSkills()
    {
        var method = typeof(ClientSession).GetMethod("GrantGoddessSkills", BindingFlags.NonPublic | BindingFlags.Instance);
        method?.Invoke(this, null);
    }
}

public class SpecialSkillQuestTests
{
    private readonly ServerQuestManager _questManager;
    private readonly PlayerQuestComponent _playerQuests;
    private readonly ServerCharacter _character;
    private readonly string _testFilePath;

    public SpecialSkillQuestTests()
    {
        _testFilePath = Path.GetTempFileName();

        var testQuests = new List<QuestDefinition>
        {
            new QuestDefinition
            {
                QuestId = 8001,
                Title = "Test Skill Quest",
                Description = "Quest for Skill",
                Type = "SpecialSkill",
                Objectives = new List<ObjectiveDefinition>
                {
                    new ObjectiveDefinition("Kill", "Target", 1, "Kill Target")
                },
                Rewards = new RewardDefinition(0,0, new List<ItemReward>(), null, 8001)
            }
        };

        var json = JsonSerializer.Serialize(testQuests);
        File.WriteAllText(_testFilePath, json);

        _questManager = new ServerQuestManager();
        _questManager.Load(_testFilePath);

        _playerQuests = new PlayerQuestComponent(_questManager);
        _character = new ServerCharacter { Id = 1, Name = "TestPlayer" };
        _playerQuests.Character = _character;
        _playerQuests.Character.Str = 100; // Ensure stats if needed
    }

    [Fact]
    public void GrantGoddessSkills_Should_Grant_Skill_Based_On_Element()
    {
        var session = new TestClientSession();
        _character.CharacterElement = Element.Fire;
        session.SetComponents(_character, _playerQuests);

        session.InvokeGrantGoddessSkills();

        Assert.Contains(2003, _character.KnownSkills); // Hotfire
        Assert.Contains("GS_GRANTED", _playerQuests.Flags);
    }

    [Fact]
    public void GrantGoddessSkills_Should_Not_Grant_Twice()
    {
        var session = new TestClientSession();
        _character.CharacterElement = Element.Water;
        session.SetComponents(_character, _playerQuests);

        session.InvokeGrantGoddessSkills();
        Assert.Contains(2001, _character.KnownSkills); // Shrink

        // Change element (impossible in game, but for test)
        _character.CharacterElement = Element.Wind;

        session.InvokeGrantGoddessSkills();

        // Should NOT have Wind skill (2004) because flag is present
        Assert.DoesNotContain(2004, _character.KnownSkills);
    }

    [Fact]
    public void LearnSkill_Should_Add_Skill_Once()
    {
        Assert.Empty(_character.KnownSkills);
        bool learned = _character.LearnSkill(8001);
        Assert.True(learned);
        Assert.Contains(8001, _character.KnownSkills);

        // Try learning again
        bool learnedAgain = _character.LearnSkill(8001);
        Assert.False(learnedAgain);
    }

    [Fact]
    public void GrantGoddessSkills_Should_Set_Flag_If_Already_Known()
    {
        var session = new TestClientSession();
        _character.CharacterElement = Element.Fire;
        _character.KnownSkills.Add(2003); // Manually add Hotfire
        session.SetComponents(_character, _playerQuests);

        session.InvokeGrantGoddessSkills();

        Assert.Contains("GS_GRANTED", _playerQuests.Flags);
        // Should not have tried to learn it again (no error, just silent success)
        Assert.Contains(2003, _character.KnownSkills);
    }

    [Fact]
    public void AntiAbuse_UniquePerCharacter_Should_Prevent_Replay()
    {
        // Define a quest with UniquePerCharacter
        var def = new QuestDefinition
        {
            QuestId = 9999,
            Title = "Unique Quest",
            Description = "One time only",
            Type = "SpecialSkill",
            Rewards = new RewardDefinition(0,0, new List<ItemReward>()),
            Objectives = new List<ObjectiveDefinition>(),
            AntiAbuseRules = "UniquePerCharacter",
            Repeatable = true // Intentionally conflicting to test AntiAbuse overrides
        };

        // Hacky: Insert into manager via private field or re-load.
        // Since we can't easily modify manager, we'll create a new test setup here or assume 8001 has it (it does in quests.json but not in test definition).
        // I'll create a new definition file for this test instance or just rely on CanStartQuest logic unit test style?
        // Since _questManager is loaded from file, I need to write to file and reload.

        var path = Path.GetTempFileName();
        var list = new List<QuestDefinition> { def };
        File.WriteAllText(path, JsonSerializer.Serialize(list));
        var qm = new ServerQuestManager();
        qm.Load(path);
        var pq = new PlayerQuestComponent(qm);
        pq.Character = new ServerCharacter { Id = 2 };

        // 1. Start Quest
        Assert.True(pq.StartQuest(9999));

        // 2. Complete/Claim
        pq.QuestStates[9999] = QuestState.RewardClaimed; // Simulate completion

        // 3. Try Start Again
        // Even though Repeatable=true, AntiAbuse should block it because it exists in QuestStates
        Assert.False(pq.CanStartQuest(9999));
    }
}
