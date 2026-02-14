using TWL.Shared.Domain.Battle;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Skills;
using TWL.Tests.Mocks;
using Xunit;

namespace TWL.Tests.Simulation;

public class AutoBattleServiceTests
{
    private MockSkillCatalog _catalog;
    private MockRandomService _random;
    private AutoBattleService _manager;
    private StatusEngine _statusEngine;

    public AutoBattleServiceTests()
    {
        _catalog = new MockSkillCatalog();
        _random = new MockRandomService(0.5f);
        _manager = new AutoBattleService(_catalog);
        _statusEngine = new StatusEngine();
    }

    private ServerCharacter CreateActor(int id, Team team)
    {
        return new ServerCharacter { Id = id, Team = team, Hp = 100, Con = 10, Sp = 100, Int = 10, Wis = 10, Str = 10 };
    }

    [Fact]
    public void GetBestAction_Survival_HealsAllyLowHp()
    {
        var actor = CreateActor(1, Team.Player);
        var ally = CreateActor(2, Team.Player);
        ally.Hp = 20; // 20% < 30%
        var enemy = CreateActor(3, Team.Enemy);

        var healSkill = new Skill
        {
            SkillId = 100,
            Name = "Heal",
            SpCost = 10,
            Effects = new List<SkillEffect> { new SkillEffect { Tag = SkillEffectTag.Heal } },
            TargetType = SkillTargetType.SingleAlly,
            Scaling = new List<SkillScaling> { new SkillScaling { Stat = StatType.Wis, Coefficient = 2.0f } }
        };
        _catalog.AddSkill(healSkill);
        actor.SkillMastery.TryAdd(100, new SkillMastery());

        var result = _manager.GetBestAction(actor, new[] { actor, ally, enemy });

        Assert.NotNull(result);
        Assert.Equal(100, result.SkillId);
        Assert.Equal(ally.Id, result.TargetId);
    }

    [Fact]
    public void GetBestAction_Aggression_UsesStrongestAttack()
    {
        var actor = CreateActor(1, Team.Player);
        var enemy = CreateActor(2, Team.Enemy);

        var weakAttack = new Skill
        {
            SkillId = 101, Name = "Weak", SpCost = 5, Branch = SkillBranch.Physical, TargetType = SkillTargetType.SingleEnemy,
            Effects = new List<SkillEffect> { new SkillEffect { Tag = SkillEffectTag.Damage } },
            Scaling = new List<SkillScaling> { new SkillScaling { Stat = StatType.Str, Coefficient = 1.0f } }
        };
        var strongAttack = new Skill
        {
            SkillId = 102, Name = "Strong", SpCost = 20, Branch = SkillBranch.Physical, TargetType = SkillTargetType.SingleEnemy,
            Effects = new List<SkillEffect> { new SkillEffect { Tag = SkillEffectTag.Damage } },
            Scaling = new List<SkillScaling> { new SkillScaling { Stat = StatType.Str, Coefficient = 2.0f } }
        };

        _catalog.AddSkill(weakAttack);
        _catalog.AddSkill(strongAttack);

        actor.SkillMastery.TryAdd(101, new SkillMastery());
        actor.SkillMastery.TryAdd(102, new SkillMastery());

        var result = _manager.GetBestAction(actor, new[] { actor, enemy });

        Assert.NotNull(result);
        Assert.Equal(102, result.SkillId);
        Assert.Equal(enemy.Id, result.TargetId);
    }

    [Fact]
    public void GetBestAction_Constraints_RespectsMinSp()
    {
        var actor = CreateActor(1, Team.Player);
        actor.Sp = 15;
        _manager.MinSpThreshold = 10;
        var enemy = CreateActor(2, Team.Enemy);

        // Strong skill: Cost 10. Remaining: 5 (Below 10). Should NOT use.
        var strongSkill = new Skill
        {
            SkillId = 200, SpCost = 10, TargetType = SkillTargetType.SingleEnemy,
            Effects = new List<SkillEffect> { new SkillEffect { Tag = SkillEffectTag.Damage } }
        };

        // Basic attack (Cost 0) - should fallback to this
        var basicAttack = new Skill
        {
            SkillId = 1, SpCost = 0, TargetType = SkillTargetType.SingleEnemy,
            Effects = new List<SkillEffect> { new SkillEffect { Tag = SkillEffectTag.Damage } }
        };

        _catalog.AddSkill(strongSkill);
        _catalog.AddSkill(basicAttack);

        actor.SkillMastery.TryAdd(200, new SkillMastery());
        actor.SkillMastery.TryAdd(1, new SkillMastery());

        var result = _manager.GetBestAction(actor, new[] { actor, enemy });

        Assert.NotNull(result);
        Assert.Equal(1, result.SkillId); // Basic attack
    }

    [Fact]
    public void GetBestAction_Buffs_OverwritesIfHigherPriority()
    {
        var actor = CreateActor(1, Team.Player);
        var ally = CreateActor(2, Team.Player);
        var enemy = CreateActor(3, Team.Enemy);

        // Ally has Weak Atk Buff
        ally.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.BuffStats, 10, 3, "Atk")
        {
            ConflictGroup = "Buff_Atk",
            Priority = 1,
            StackingPolicy = StackingPolicy.NoStackOverwrite
        }, _statusEngine);

        // Actor has Strong Atk Buff Skill
        var strongBuff = new Skill
        {
            SkillId = 300, SpCost = 10, TargetType = SkillTargetType.SingleAlly,
            Effects = new List<SkillEffect>
            {
                new SkillEffect
                {
                    Tag = SkillEffectTag.BuffStats, Param = "Atk", Value = 20,
                    ConflictGroup = "Buff_Atk", Priority = 2, StackingPolicy = StackingPolicy.NoStackOverwrite
                }
            }
        };

        _catalog.AddSkill(strongBuff);
        actor.SkillMastery.TryAdd(300, new SkillMastery());

        var result = _manager.GetBestAction(actor, new[] { actor, ally, enemy }, AutoBattlePolicy.Supportive);

        Assert.NotNull(result);
        Assert.Equal(300, result.SkillId);
        Assert.Equal(ally.Id, result.TargetId);
    }

    [Fact]
    public void GetBestAction_Buffs_SkipsIfLowerPriority()
    {
        var actor = CreateActor(1, Team.Player);
        var ally = CreateActor(2, Team.Player);
        var enemy = CreateActor(3, Team.Enemy);

        // Ally has Strong Atk Buff
        ally.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.BuffStats, 20, 3, "Atk")
        {
            ConflictGroup = "Buff_Atk",
            Priority = 2,
            StackingPolicy = StackingPolicy.NoStackOverwrite
        }, _statusEngine);

        // Actor also has Strong Atk Buff (to ensure no candidates)
        actor.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.BuffStats, 20, 3, "Atk")
        {
            ConflictGroup = "Buff_Atk",
            Priority = 2,
            StackingPolicy = StackingPolicy.NoStackOverwrite
        }, _statusEngine);

        // Actor has Weak Atk Buff Skill
        var weakBuff = new Skill
        {
            SkillId = 301, SpCost = 10, TargetType = SkillTargetType.SingleAlly,
            Effects = new List<SkillEffect>
            {
                new SkillEffect
                {
                    Tag = SkillEffectTag.BuffStats, Param = "Atk", Value = 10,
                    ConflictGroup = "Buff_Atk", Priority = 1, StackingPolicy = StackingPolicy.NoStackOverwrite
                }
            }
        };

        _catalog.AddSkill(weakBuff);
        actor.SkillMastery.TryAdd(301, new SkillMastery());

        // Fallback
        var basicAttack = new Skill { SkillId = 1, SpCost = 0, TargetType = SkillTargetType.SingleEnemy, Effects = new List<SkillEffect> { new SkillEffect { Tag = SkillEffectTag.Damage } } };
        _catalog.AddSkill(basicAttack);
        actor.SkillMastery.TryAdd(1, new SkillMastery());

        var result = _manager.GetBestAction(actor, new[] { actor, ally, enemy }, AutoBattlePolicy.Supportive);

        Assert.NotNull(result);
        Assert.NotEqual(301, result.SkillId); // Should not be the buff
        Assert.Equal(1, result.SkillId); // Basic attack
    }

    [Fact]
    public void GetBestAction_Targeting_SelectsBestCandidate()
    {
        var actor = CreateActor(1, Team.Player);
        var weakAlly = CreateActor(2, Team.Player);
        // Atk = Str * 2. We want Atk ~ 10, so Str = 5.
        weakAlly.Str = 5;
        var strongAlly = CreateActor(3, Team.Player);
        // We want Atk ~ 50, so Str = 25.
        strongAlly.Str = 25; // Highest ATK
        var enemy = CreateActor(4, Team.Enemy);

        // Buff Atk Skill
        var buffSkill = new Skill
        {
            SkillId = 400, SpCost = 10, TargetType = SkillTargetType.SingleAlly,
            Effects = new List<SkillEffect>
            {
                new SkillEffect { Tag = SkillEffectTag.BuffStats, Param = "Atk", Value = 10 }
            }
        };

        _catalog.AddSkill(buffSkill);
        actor.SkillMastery.TryAdd(400, new SkillMastery());

        var result = _manager.GetBestAction(actor, new[] { actor, weakAlly, strongAlly, enemy }, AutoBattlePolicy.Supportive);

        Assert.NotNull(result);
        Assert.Equal(400, result.SkillId);
        Assert.Equal(strongAlly.Id, result.TargetId); // Should target strongest ally
    }
}
