using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Skills;
using Xunit;

namespace TWL.Tests.Combat;

public class MockSkillCatalog : ISkillCatalog
{
    private Dictionary<int, Skill> _skills = new();

    public void AddSkill(Skill skill)
    {
        _skills[skill.SkillId] = skill;
    }

    public IEnumerable<int> GetAllSkillIds() => _skills.Keys;

    public Skill? GetSkillById(int id) => _skills.GetValueOrDefault(id);
}

public class TestCharacter : Character
{
    public TestCharacter(string name, Element element = Element.Earth) : base(name, element)
    {
    }
}

public class SkillEffectTests
{
    private readonly MockSkillCatalog _mockCatalog;
    private readonly TestCharacter _actor;

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
                new SkillEffect { Tag = SkillEffectTag.Cleanse }
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
                new SkillEffect { Tag = SkillEffectTag.Cleanse }
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
}
