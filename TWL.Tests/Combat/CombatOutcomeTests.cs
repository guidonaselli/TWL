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

public class CombatOutcomeTests
{
    private readonly CombatManager _manager;
    private readonly MockRandomService _random;
    private readonly MockSkillCatalog _skills;
    private readonly IStatusEngine _statusEngine;

    public CombatOutcomeTests()
    {
        _random = new MockRandomService();
        _skills = new MockSkillCatalog();
        var resolver = new StandardCombatResolver(_random, _skills);
        _statusEngine = new StatusEngine();
        _manager = new CombatManager(resolver, _random, _skills, _statusEngine);
    }

    [Fact]
    public void UseSkill_OutcomeResist_ResistsEffectWithBuff()
    {
        // Setup Skill
        var skillId = 1;
        var skill = new Skill
        {
            SkillId = skillId,
            Name = "Seal",
            SpCost = 0,
            Effects = new List<SkillEffect>
            {
                new() {
                    Tag = SkillEffectTag.Seal,
                    Value = 0,
                    Duration = 2,
                    Chance = 1.0f,
                    Outcome = OutcomeModel.Resist,
                    ResistanceTags = new List<string> { "SealResist" }
                }
            },
            TargetType = SkillTargetType.SingleEnemy
        };
        _skills.AddSkill(skill);

        var attacker = new ServerCharacter { Id = 1, Int = 10, Wis = 10, Hp = 100, Sp = 100, Agi = 10, Team = Team.Player };
        var target = new ServerCharacter { Id = 2, Int = 10, Wis = 10, Hp = 100, Sp = 100, Agi = 5, Team = Team.Enemy };

        // Add 50% Resistance Buff to Target
        var resistBuff = new StatusEffectInstance(SkillEffectTag.BuffStats, 0.5f, 5, "SealResist");
        target.AddStatusEffect(resistBuff, _statusEngine);

        _manager.RegisterCombatant(attacker);
        _manager.RegisterCombatant(target);
        _manager.StartEncounter(1, new[] { attacker, target });

        // Random < Resistance (0.5) -> Resists
        // Set Random = 0.4
        _random.FixedFloat = 0.4f;

        var results = _manager.UseSkill(new UseSkillRequest { PlayerId = 1, SkillId = skillId, TargetId = 2 });

        Assert.Single(results);
        var result = results.First();
        // Should be resisted
        Assert.DoesNotContain(result.AddedEffects, e => e.Tag == SkillEffectTag.Seal);
    }

    [Fact]
    public void UseSkill_OutcomePartial_AppliesReducedEffect()
    {
        // Setup Skill
        var skillId = 1;
        var skill = new Skill
        {
            SkillId = skillId,
            Name = "Slow",
            SpCost = 0,
            Effects = new List<SkillEffect>
            {
                new() {
                    Tag = SkillEffectTag.DebuffStats,
                    Param = "Spd",
                    Value = 50,
                    Duration = 4,
                    Chance = 1.0f,
                    Outcome = OutcomeModel.Partial,
                    ResistanceTags = new List<string> { "SpdResist" }
                }
            },
            TargetType = SkillTargetType.SingleEnemy
        };
        _skills.AddSkill(skill);

        var attacker = new ServerCharacter { Id = 1, Int = 10, Wis = 10, Hp = 100, Sp = 100, Agi = 10, Team = Team.Player };
        var target = new ServerCharacter { Id = 2, Int = 10, Wis = 10, Hp = 100, Sp = 100, Agi = 5, Team = Team.Enemy };

        // Add 50% Resistance Buff to Target
        var resistBuff = new StatusEffectInstance(SkillEffectTag.BuffStats, 0.5f, 5, "SpdResist");
        target.AddStatusEffect(resistBuff, _statusEngine);

        _manager.RegisterCombatant(attacker);
        _manager.RegisterCombatant(target);
        _manager.StartEncounter(1, new[] { attacker, target });

        // Random < Resistance (0.5) -> Partial Resist
        // Set Random = 0.4
        _random.FixedFloat = 0.4f;

        var results = _manager.UseSkill(new UseSkillRequest { PlayerId = 1, SkillId = skillId, TargetId = 2 });

        Assert.Single(results);
        var result = results.First();
        var applied = result.AddedEffects.FirstOrDefault(e => e.Tag == SkillEffectTag.DebuffStats);

        Assert.NotNull(applied);
        // Duration halved: 4 -> 2
        Assert.Equal(2, applied.TurnsRemaining);
        // Value halved: 50 -> 25
        Assert.Equal(25, applied.Value);
    }
}
