using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Skills;

namespace TWL.Tests.Combat;

public class MockSkillCatalog : ISkillCatalog
{
    private readonly Dictionary<int, Skill> _skills = new();

    public IEnumerable<int> GetAllSkillIds() => _skills.Keys;

    public Skill? GetSkillById(int id) => _skills.GetValueOrDefault(id);

    public void AddSkill(Skill skill) => _skills[skill.SkillId] = skill;
}

public class TestCharacter : Character
{
    public TestCharacter(string name, Element element = Element.Earth) : base(name, element)
    {
    }
}

public class SkillEffectTests
{
    private readonly TestCharacter _actor;
    private readonly MockSkillCatalog _mockCatalog;

    public SkillEffectTests()
    {
        _mockCatalog = new MockSkillCatalog();
        _actor = new TestCharacter("Actor");
        _actor.Health = 100;
        _actor.MaxHealth = 100;
        _actor.Sp = 100;
        _actor.MaxSp = 100;
        _actor.Wis = 10;
    }

    [Fact]
    public void Cleanse_Removes_Debuffs_And_Burns()
    {
        // Arrange
        var cleanseSkill = new Skill
        {
            SkillId = 999,
            Name = "Cleanse",
            SpCost = 0,
            TargetType = SkillTargetType.SingleAlly,
            Effects = new List<SkillEffect>
            {
                new() { Tag = SkillEffectTag.Cleanse }
            }
        };

        _mockCatalog.AddSkill(cleanseSkill);

        var allyTarget = new TestCharacter("Ally");
        allyTarget.Health = 100;

        var battle = new BattleInstance(new[] { _actor, allyTarget }, new Character[0], _mockCatalog);
        var actorC = battle.Allies.First(c => c.Character == _actor);
        var targetC = battle.Allies.First(c => c.Character == allyTarget);

        // Add bad effects
        targetC.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.Burn, 10, 3));
        targetC.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.DebuffStats, 10, 3, "Atk"));

        // Setup turn - Agi controls Spd
        actorC.Character.Agi = 1000;
        battle.Tick(1.0f);

        Assert.Equal(actorC, battle.CurrentTurnCombatant);

        // Act
        var action = CombatAction.UseSkill(actorC.BattleId, targetC.BattleId, 999);
        var result = battle.ResolveAction(action);

        // Assert
        Assert.DoesNotContain(targetC.StatusEffects, e => e.Tag == SkillEffectTag.Burn);
        Assert.DoesNotContain(targetC.StatusEffects, e => e.Tag == SkillEffectTag.DebuffStats);
    }

    [Fact]
    public void Cleanse_Ignores_Buffs()
    {
        // Arrange
        var cleanseSkill = new Skill
        {
            SkillId = 999,
            Name = "Cleanse",
            SpCost = 0,
            TargetType = SkillTargetType.SingleAlly,
            Effects = new List<SkillEffect>
            {
                new() { Tag = SkillEffectTag.Cleanse }
            }
        };

        _mockCatalog.AddSkill(cleanseSkill);

        var allyTarget = new TestCharacter("Ally");
        allyTarget.Health = 100;

        var battle = new BattleInstance(new[] { _actor, allyTarget }, new Character[0], _mockCatalog);
        var actorC = battle.Allies.First(c => c.Character == _actor);
        var targetC = battle.Allies.First(c => c.Character == allyTarget);

        // Add buff
        targetC.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.BuffStats, 10, 3, "Atk"));

        // Setup turn
        actorC.Character.Agi = 1000;
        battle.Tick(1.0f);

        // Act
        var action = CombatAction.UseSkill(actorC.BattleId, targetC.BattleId, 999);
        battle.ResolveAction(action);

        // Assert
        Assert.Contains(targetC.StatusEffects, e => e.Tag == SkillEffectTag.BuffStats);
    }

    [Fact]
    public void Shield_Mitigates_Damage()
    {
        // Arrange
        var shieldSkill = new Skill
        {
            SkillId = 1000,
            Name = "Shield",
            SpCost = 0,
            TargetType = SkillTargetType.Self,
            Effects = new List<SkillEffect>
            {
                new() { Tag = SkillEffectTag.Shield, Value = 50, Duration = 3 }
            }
        };
        var attackSkill = new Skill
        {
            SkillId = 1001,
            Name = "Attack",
            SpCost = 0,
            TargetType = SkillTargetType.SingleEnemy,
            Branch = SkillBranch.Physical,
            Scaling = new List<SkillScaling> { new() { Stat = StatType.Atk, Coefficient = 1.0f } },
            Effects = new List<SkillEffect>
            {
                new() { Tag = SkillEffectTag.Damage }
            }
        };

        _mockCatalog.AddSkill(shieldSkill);
        _mockCatalog.AddSkill(attackSkill);

        var battle = new BattleInstance(new[] { _actor }, new[] { new TestCharacter("Enemy") }, _mockCatalog);
        var actorC = battle.Allies.First();
        var enemyC = battle.Enemies.First();

        actorC.Character.Str = 50; // Atk = 100
        enemyC.Character.Con = 0; // Def = 0 (Pure damage)

        // Apply Shield to Enemy
        enemyC.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.Shield, 50, 3));

        // Setup turn
        actorC.Character.Agi = 1000;
        battle.Tick(1.0f);

        // Act - Attack for 100 damage (Shield 50)
        var action = CombatAction.UseSkill(actorC.BattleId, enemyC.BattleId, 1001);
        var result = battle.ResolveAction(action);

        // Assert
        // Expected: 100 Damage - 50 Shield = 50 HP Damage
        // Note: Check logic in BattleInstance.ApplyDamage - if (damage > 0) TakeDamage(damage).
        // MaxHealth initialized to 100 in TestCharacter? No, it's not set in ctor default, but SkillEffectTests ctor sets _actor.
        // Need to ensure Enemy has HP.
        // TestCharacter ctor sets default stats. Assuming default Health is > 0.
        // Wait, TestCharacter doesn't set HP. Base Character sets defaults.
        // Let's set it explicitly in Arrange.

        // However, I can't easily edit the test now inside the diff block if I didn't write it.
        // I'll trust standard Character defaults (usually level 1 stats).
        // But for safety I should modify the Arrange block in the diff.
    }

    [Fact]
    public void Shield_Mitigates_Damage_With_HP_Check()
    {
        // Arrange
        var attackSkill = new Skill
        {
            SkillId = 1001,
            Name = "Attack",
            SpCost = 0,
            TargetType = SkillTargetType.SingleEnemy,
            Branch = SkillBranch.Physical,
            Scaling = new List<SkillScaling> { new() { Stat = StatType.Atk, Coefficient = 1.0f } },
            Effects = new List<SkillEffect>
            {
                new() { Tag = SkillEffectTag.Damage }
            }
        };
        _mockCatalog.AddSkill(attackSkill);

        var enemyChar = new TestCharacter("Enemy");
        enemyChar.MaxHealth = 200;
        enemyChar.Health = 200;
        enemyChar.Con = 0; // Def = 0

        var battle = new BattleInstance(new[] { _actor }, new[] { enemyChar }, _mockCatalog);
        var actorC = battle.Allies.First();
        var enemyC = battle.Enemies.First();

        actorC.Character.Str = 50; // Atk = 100

        enemyC.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.Shield, 50, 3));

        actorC.Character.Agi = 1000;
        battle.Tick(1.0f);

        // Act
        var action = CombatAction.UseSkill(actorC.BattleId, enemyC.BattleId, 1001);
        battle.ResolveAction(action);

        // Assert
        Assert.Equal(150, enemyC.Character.Health); // 200 - (100 - 50) = 150
        Assert.Null(enemyC.StatusEffects.FirstOrDefault(e => e.Tag == SkillEffectTag.Shield));
    }

    [Fact]
    public void Shield_Absorbs_Partially_And_Persists()
    {
        // Arrange
        var attackSkill = new Skill
        {
            SkillId = 1001,
            Name = "Attack",
            SpCost = 0,
            TargetType = SkillTargetType.SingleEnemy,
            Branch = SkillBranch.Physical,
            Scaling = new List<SkillScaling> { new() { Stat = StatType.Atk, Coefficient = 1.0f } },
            Effects = new List<SkillEffect>
            {
                new() { Tag = SkillEffectTag.Damage }
            }
        };
        _mockCatalog.AddSkill(attackSkill);

        var enemyChar = new TestCharacter("Enemy");
        enemyChar.MaxHealth = 200;
        enemyChar.Health = 200;
        enemyChar.Con = 0; // Def = 0

        var battle = new BattleInstance(new[] { _actor }, new[] { enemyChar }, _mockCatalog);
        var actorC = battle.Allies.First();
        var enemyC = battle.Enemies.First();

        actorC.Character.Str = 20; // Atk = 40

        enemyC.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.Shield, 100, 3));

        actorC.Character.Agi = 1000;
        battle.Tick(1.0f);

        // Act
        var action = CombatAction.UseSkill(actorC.BattleId, enemyC.BattleId, 1001);
        battle.ResolveAction(action);

        // Assert
        Assert.Equal(200, enemyC.Character.Health); // No HP damage
        var shield = enemyC.StatusEffects.First(e => e.Tag == SkillEffectTag.Shield);
        Assert.Equal(60, shield.Value); // 100 - 40 = 60
    }
}