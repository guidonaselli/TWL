using TWL.Server.Features.Combat;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Services;
using TWL.Tests.Mocks;
using Xunit;

namespace TWL.Tests.Combat;

public class CombatHitRulesTests
{
    private readonly CombatManager _manager;
    private readonly MockRandomService _random;
    private readonly MockSkillCatalog _skills;
    private readonly IStatusEngine _statusEngine;

    public CombatHitRulesTests()
    {
        _random = new MockRandomService();
        _skills = new MockSkillCatalog();
        var resolver = new StandardCombatResolver(_random, _skills);
        _statusEngine = new StatusEngine();
        _manager = new CombatManager(resolver, _random, _skills, _statusEngine);
    }

    [Fact]
    public void UseSkill_WithHitRules_CalculatesChanceBasedOnStats()
    {
        // Setup Skill
        var skillId = 1;
        var skill = new Skill
        {
            SkillId = skillId,
            Name = "Seal",
            SpCost = 0,
            HitRules = new SkillHitRules
            {
                BaseChance = 0.5f,
                StatDependence = "Int-Wis",
                MinChance = 0.1f,
                MaxChance = 0.9f
            },
            Effects = new List<SkillEffect>
            {
                new() { Tag = SkillEffectTag.Seal, Value = 0, Duration = 2, Chance = 1.0f }
            },
            TargetType = SkillTargetType.SingleEnemy
        };
        _skills.AddSkill(skill);

        // Attacker: High Int (100)
        var attacker = new ServerCharacter { Id = 1, Int = 100, Wis = 10, Hp = 100, Sp = 100, Agi = 10, Team = Team.Player };
        // Target: Low Wis (10)
        var target = new ServerCharacter { Id = 2, Int = 10, Wis = 10, Hp = 100, Sp = 100, Agi = 5, Team = Team.Enemy };

        _manager.RegisterCombatant(attacker);
        _manager.RegisterCombatant(target);

        // Start encounter to initialize turn engine
        _manager.StartEncounter(1, new[] { attacker, target });

        // Expected Chance: 0.5 + (100 - 10) * 0.01 = 0.5 + 0.9 = 1.4 -> Max 0.9.
        // We set Random to 0.85 (below 0.9) -> Should Hit
        _random.FixedFloat = 0.85f;

        var results = _manager.UseSkill(new UseSkillRequest { PlayerId = 1, SkillId = skillId, TargetId = 2 });

        Assert.Single(results);
        var result = results.First();
        Assert.Contains(result.AddedEffects, e => e.Tag == SkillEffectTag.Seal);
    }

    [Fact]
    public void UseSkill_WithHitRules_FailsWhenChanceIsLow()
    {
        // Setup Skill
        var skillId = 1;
        var skill = new Skill
        {
            SkillId = skillId,
            Name = "Seal",
            SpCost = 0,
            HitRules = new SkillHitRules
            {
                BaseChance = 0.5f,
                StatDependence = "Int-Wis",
                MinChance = 0.1f,
                MaxChance = 0.9f
            },
            Effects = new List<SkillEffect>
            {
                new() { Tag = SkillEffectTag.Seal, Value = 0, Duration = 2, Chance = 1.0f }
            },
            TargetType = SkillTargetType.SingleEnemy
        };
        _skills.AddSkill(skill);

        // Attacker: Low Int (10)
        var attacker = new ServerCharacter { Id = 1, Int = 10, Wis = 10, Hp = 100, Sp = 100, Agi = 10, Team = Team.Player };
        // Target: High Wis (100)
        var target = new ServerCharacter { Id = 2, Int = 10, Wis = 100, Hp = 100, Sp = 100, Agi = 5, Team = Team.Enemy };

        _manager.RegisterCombatant(attacker);
        _manager.RegisterCombatant(target);
        _manager.StartEncounter(1, new[] { attacker, target });

        // Expected Chance: 0.5 + (10 - 100) * 0.01 = 0.5 - 0.9 = -0.4 -> Min 0.1.
        // We set Random to 0.15 (above 0.1) -> Should Miss
        _random.FixedFloat = 0.15f;

        var results = _manager.UseSkill(new UseSkillRequest { PlayerId = 1, SkillId = skillId, TargetId = 2 });

        Assert.Single(results);
        var result = results.First();
        Assert.DoesNotContain(result.AddedEffects, e => e.Tag == SkillEffectTag.Seal);
    }
}
