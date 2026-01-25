using System.Collections.Generic;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;
using TWL.Tests.Mocks;
using Xunit;

namespace TWL.Tests.Server;

public class QuestCombatIntegrationTests
{
    private readonly ServerQuestManager _questManager;
    private readonly CombatManager _combatManager;
    private readonly PlayerQuestComponent _playerQuests;

    public QuestCombatIntegrationTests()
    {
        // 1. Setup Quest Manager with a Kill Quest
        _questManager = new ServerQuestManager();
        var quests = new List<QuestDefinition>
        {
            new QuestDefinition
            {
                QuestId = 1,
                Title = "Kill Crabs",
                Description = "Kill 2 Crabs",
                Requirements = new List<int>(),
                Objectives = new List<ObjectiveDefinition>
                {
                    new ObjectiveDefinition("Kill", "WeakCrab", 2, "Kill 2 WeakCrabs")
                },
                Rewards = new RewardDefinition(100, 0, new List<ItemReward>())
            }
        };
        // Hack: Save and Load because ServerQuestManager doesn't expose AddQuest directly
        string json = System.Text.Json.JsonSerializer.Serialize(quests);
        System.IO.File.WriteAllText("test_combat_quests.json", json);
        _questManager.Load("test_combat_quests.json");

        // Load Skills
        var skillsJson = @"
[
  {
    ""SkillId"": 999,
    ""Name"": ""Basic Attack"",
    ""Element"": ""Earth"",
    ""Branch"": ""Physical"",
    ""Tier"": 1,
    ""TargetType"": ""SingleEnemy"",
    ""SpCost"": 0,
    ""Scaling"": [ { ""Stat"": ""Str"", ""Coefficient"": 2.0 } ],
    ""Effects"": [ { ""Tag"": ""Damage"" } ]
  }
]";
        TWL.Shared.Domain.Skills.SkillRegistry.Instance.LoadSkills(skillsJson);

        // 2. Setup Combat Manager
        var random = new MockRandomService();
        var resolver = new StandardCombatResolver(random, TWL.Shared.Domain.Skills.SkillRegistry.Instance);
        _combatManager = new CombatManager(resolver, random, TWL.Shared.Domain.Skills.SkillRegistry.Instance);

        // 3. Setup Player Component
        _playerQuests = new PlayerQuestComponent(_questManager);
        _playerQuests.StartQuest(1);
    }

    [Fact]
    public void CombatKill_ShouldProgressQuest()
    {
        // Setup Characters
        var player = new ServerCharacter { Id = 1, Name = "Hero", Hp = 100, Str = 50 }; // High STR to kill
        // Atk = 100. Skill = 200. Target Def = 16. Dmg = 184.

        var mob1 = new ServerCharacter { Id = 2, Name = "WeakCrab", Hp = 10 };
        var mob2 = new ServerCharacter { Id = 3, Name = "WeakCrab", Hp = 10 };

        _combatManager.AddCharacter(player);
        _combatManager.AddCharacter(mob1);
        _combatManager.AddCharacter(mob2);

        // --- Simulate Attack 1 (Kill Mob 1) ---
        var request1 = new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = 999 };
        var result1 = _combatManager.UseSkill(request1);

        Assert.NotNull(result1);
        Assert.True(result1.NewTargetHp <= 0, "Mob should be dead");

        // Simulate ClientSession logic
        if (result1.NewTargetHp <= 0)
        {
            var target = _combatManager.GetCharacter(result1.TargetId);
            Assert.NotNull(target);
            Assert.Equal("WeakCrab", target.Name);

            var updated = _playerQuests.TryProgress("Kill", target.Name);
            Assert.Single(updated);
            Assert.Equal(1, updated[0]); // Quest 1 updated
        }

        Assert.Equal(1, _playerQuests.QuestProgress[1][0]);

        // --- Simulate Attack 2 (Kill Mob 2) ---
        var request2 = new UseSkillRequest { PlayerId = 1, TargetId = 3, SkillId = 999 };
        var result2 = _combatManager.UseSkill(request2);

        Assert.True(result2.NewTargetHp <= 0);

        if (result2.NewTargetHp <= 0)
        {
            var target = _combatManager.GetCharacter(result2.TargetId);
            var updated = _playerQuests.TryProgress("Kill", target.Name);
            Assert.Single(updated);
        }

        // Quest should be completed
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[1]);
    }

    [Fact]
    public void CombatDamage_ShouldNotProgressQuest_IfTargetAlive()
    {
        var player = new ServerCharacter { Id = 1, Name = "Hero", Hp = 100, Str = 5 }; // Low STR
        // Atk = 10. Skill = 20. Target Def = 16. Dmg = 4.

        var mob = new ServerCharacter { Id = 2, Name = "WeakCrab", Hp = 100 };

        _combatManager.AddCharacter(player);
        _combatManager.AddCharacter(mob);

        var request = new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = 999 };
        var result = _combatManager.UseSkill(request);

        Assert.True(result.NewTargetHp > 0);

        // Simulate ClientSession Logic
        List<int> updated = new List<int>();
        if (result.NewTargetHp <= 0)
        {
            // Should not reach here
            var target = _combatManager.GetCharacter(result.TargetId);
            updated = _playerQuests.TryProgress("Kill", target.Name);
        }

        Assert.Empty(updated);
        Assert.Equal(0, _playerQuests.QuestProgress[1][0]);
    }
}
