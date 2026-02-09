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

public class CombatStackingTests
{
    private readonly CombatManager _manager;
    private readonly MockRandomService _random;
    private readonly MockSkillCatalog _skills;
    private readonly IStatusEngine _statusEngine;

    public CombatStackingTests()
    {
        _random = new MockRandomService();
        _skills = new MockSkillCatalog();
        var resolver = new StandardCombatResolver(_random, _skills);
        _statusEngine = new StatusEngine();
        _manager = new CombatManager(resolver, _random, _skills, _statusEngine);
    }

    [Fact]
    public void UseSkill_StackingPolicyStackUpToN_StacksValues()
    {
        var skillId = 1;
        var skill = new Skill
        {
            SkillId = skillId,
            Name = "StackableBuff",
            SpCost = 0,
            Effects = new List<SkillEffect>
            {
                new() {
                    Tag = SkillEffectTag.BuffStats,
                    Param = "Atk",
                    Value = 10,
                    Duration = 5,
                    StackingPolicy = StackingPolicy.StackUpToN,
                    MaxStacks = 3
                }
            },
            TargetType = SkillTargetType.SingleAlly
        };
        _skills.AddSkill(skill);

        var attacker = new ServerCharacter { Id = 1, Int = 10, Wis = 10, Hp = 100, Sp = 100, Agi = 10 };
        _manager.RegisterCombatant(attacker);
        _manager.StartEncounter(1, new[] { attacker });

        // First Cast
        _manager.UseSkill(new UseSkillRequest { PlayerId = 1, SkillId = skillId, TargetId = 1 });
        var effect = attacker.StatusEffects.First(e => e.Param == "Atk");
        Assert.Equal(10, effect.Value);
        Assert.Equal(1, effect.StackCount);

        // Second Cast
        _manager.UseSkill(new UseSkillRequest { PlayerId = 1, SkillId = skillId, TargetId = 1 });
        effect = attacker.StatusEffects.First(e => e.Param == "Atk");
        Assert.Equal(20, effect.Value);
        Assert.Equal(2, effect.StackCount);
    }
}
