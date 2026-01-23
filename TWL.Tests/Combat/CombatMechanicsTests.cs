using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Skills;
using Xunit;

namespace TWL.Tests.Combat;

public class MockSkillCatalogMechanics : ISkillCatalog
{
    private Dictionary<int, Skill> _skills = new();

    public void AddSkill(Skill skill)
    {
        _skills[skill.SkillId] = skill;
    }

    public IEnumerable<int> GetAllSkillIds() => _skills.Keys;

    public Skill? GetSkillById(int id) => _skills.GetValueOrDefault(id);
}

public class TestCharacterMechanics : Character
{
    public TestCharacterMechanics(string name, Element element) : base(name, element)
    {
    }
}

public class CombatMechanicsTests
{
    private readonly MockSkillCatalogMechanics _mockCatalog;
    private readonly TestCharacterMechanics _attackerFire;
    private readonly TestCharacterMechanics _targetWind;  // Weak to Fire
    private readonly TestCharacterMechanics _targetWater; // Resists Fire
    private readonly TestCharacterMechanics _targetEarth; // Neutral to Fire

    public CombatMechanicsTests()
    {
        _mockCatalog = new MockSkillCatalogMechanics();
        _attackerFire = new TestCharacterMechanics("FireMage", Element.Fire);
        _targetWind = new TestCharacterMechanics("WindTarget", Element.Wind);
        _targetWater = new TestCharacterMechanics("WaterTarget", Element.Water);
        _targetEarth = new TestCharacterMechanics("EarthTarget", Element.Earth);

        // Stats Setup
        // Attacker: Mat = 100 (Int 50)
        _attackerFire.Int = 50;
        _attackerFire.Wis = 50;

        // Targets: Def/Mdf = 0 for easier calc
        _targetWind.Wis = 0;
        _targetWater.Wis = 0;
        _targetEarth.Wis = 0;

        // HP setup
        _targetWind.Health = 1000;
        _targetWater.Health = 1000;
        _targetEarth.Health = 1000;
    }

    [Fact]
    public void Fire_Vs_Wind_DealsBonusDamage()
    {
        // 1.5x Multiplier
        // Skill: 100 dmg base
        var skill = new Skill
        {
            SkillId = 1,
            Element = Element.Fire,
            Branch = SkillBranch.Magical,
            Effects = new List<SkillEffect> { new() { Tag = SkillEffectTag.Damage } },
            Scaling = new List<SkillScaling> { new() { Stat = StatType.Mat, Coefficient = 1.0f } }
        };
        _mockCatalog.AddSkill(skill);

        var battle = new BattleInstance(new[] { _attackerFire }, new[] { _targetWind }, _mockCatalog);
        var actor = battle.Allies[0]; // Fire
        var target = battle.Enemies[0]; // Wind

        actor.Atb = 100;
        battle.Tick(0.1f);

        var msg = battle.ResolveAction(CombatAction.UseSkill(actor.BattleId, target.BattleId, 1));

        // Base Dmg = 100 (Mat) * 1.0 = 100.
        // Multiplier = 1.5.
        // Expected = 150.

        Assert.Contains("for 150", msg);
    }

    [Fact]
    public void Fire_Vs_Water_DealsReducedDamage()
    {
        // 0.5x Multiplier
        var skill = new Skill
        {
            SkillId = 1,
            Element = Element.Fire,
            Branch = SkillBranch.Magical,
            Effects = new List<SkillEffect> { new() { Tag = SkillEffectTag.Damage } },
            Scaling = new List<SkillScaling> { new() { Stat = StatType.Mat, Coefficient = 1.0f } }
        };
        _mockCatalog.AddSkill(skill);

        var battle = new BattleInstance(new[] { _attackerFire }, new[] { _targetWater }, _mockCatalog);
        var actor = battle.Allies[0];
        var target = battle.Enemies[0];

        actor.Atb = 100;
        battle.Tick(0.1f);

        var msg = battle.ResolveAction(CombatAction.UseSkill(actor.BattleId, target.BattleId, 1));

        // Base = 100. Mult = 0.5. Expected = 50.
        Assert.Contains("for 50", msg);
    }

    [Fact]
    public void Fire_Vs_Earth_DealsNormalDamage()
    {
        // 1.0x Multiplier
        var skill = new Skill
        {
            SkillId = 1,
            Element = Element.Fire,
            Branch = SkillBranch.Magical,
            Effects = new List<SkillEffect> { new() { Tag = SkillEffectTag.Damage } },
            Scaling = new List<SkillScaling> { new() { Stat = StatType.Mat, Coefficient = 1.0f } }
        };
        _mockCatalog.AddSkill(skill);

        var battle = new BattleInstance(new[] { _attackerFire }, new[] { _targetEarth }, _mockCatalog);
        var actor = battle.Allies[0];
        var target = battle.Enemies[0];

        actor.Atb = 100;
        battle.Tick(0.1f);

        var msg = battle.ResolveAction(CombatAction.UseSkill(actor.BattleId, target.BattleId, 1));

        // Base = 100. Mult = 1.0. Expected = 100.
        Assert.Contains("for 100", msg);
    }

    [Fact]
    public void IgniteSpirit_AppliesAttackBuff()
    {
        var skill = new Skill
        {
            SkillId = 24, // Matching definition
            Element = Element.Fire,
            Branch = SkillBranch.Support,
            TargetType = SkillTargetType.SingleAlly,
            Effects = new List<SkillEffect> {
                new() { Tag = SkillEffectTag.BuffStats, Param = "Atk", Value = 20, Duration = 3 }
            },
            Scaling = new List<SkillScaling> { new() { Stat = StatType.Wis, Coefficient = 1.0f } }
        };
        _mockCatalog.AddSkill(skill);

        var battle = new BattleInstance(new[] { _attackerFire }, new[] { _targetEarth }, _mockCatalog);
        var actor = battle.Allies[0];

        actor.Atb = 100;
        battle.Tick(0.1f);

        // Cast on Self
        battle.ResolveAction(CombatAction.UseSkill(actor.BattleId, actor.BattleId, 24));

        Assert.Contains(actor.StatusEffects, e => e.Tag == SkillEffectTag.BuffStats && e.Param == "Atk");
    }

    [Fact]
    public void ControlHitChance_GuaranteesSeal_WhenIntMuchHigher()
    {
        // Attacker Int 100, Target Wis 0. Diff = 100.
        // Base Chance 0.1. Bonus = 100 * 0.01 = 1.0. Total = 1.1 -> clamped to 1.0.

        _attackerFire.Int = 100;
        _targetEarth.Wis = 0;

        var skill = new Skill
        {
            SkillId = 99,
            Element = Element.Fire,
            Branch = SkillBranch.Support,
            TargetType = SkillTargetType.SingleEnemy,
            Effects = new List<SkillEffect> {
                new() { Tag = SkillEffectTag.Seal, Chance = 0.1f, Duration = 1 }
            }
        };
        _mockCatalog.AddSkill(skill);

        var battle = new BattleInstance(new[] { _attackerFire }, new[] { _targetEarth }, _mockCatalog);
        var actor = battle.Allies[0];
        var target = battle.Enemies[0];

        actor.Atb = 100;
        battle.Tick(0.1f);

        // We run it multiple times to be sure, although 1.0 probability should be deterministic.
        battle.ResolveAction(CombatAction.UseSkill(actor.BattleId, target.BattleId, 99));

        Assert.Contains(target.StatusEffects, e => e.Tag == SkillEffectTag.Seal);
    }
}
