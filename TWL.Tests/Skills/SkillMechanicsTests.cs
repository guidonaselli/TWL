using Moq;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Services;

namespace TWL.Tests.Skills;

public class SkillMechanicsTests
{
    private readonly CombatManager _combatManager;
    private readonly Mock<IRandomService> _randomMock;
    private readonly StatusEngine _realStatusEngine;
    private readonly Mock<ICombatResolver> _resolverMock;
    private readonly Mock<ISkillCatalog> _skillCatalogMock;

    public SkillMechanicsTests()
    {
        _randomMock = new Mock<IRandomService>();
        _resolverMock = new Mock<ICombatResolver>();
        _skillCatalogMock = new Mock<ISkillCatalog>();
        _realStatusEngine = new StatusEngine();

        _combatManager = new CombatManager(
            _resolverMock.Object,
            _randomMock.Object,
            _skillCatalogMock.Object,
            _realStatusEngine
        );
    }

    [Fact]
    public void UseSkill_OutcomePartial_AppliesHalvedEffect()
    {
        // Arrange
        var attacker = new ServerCharacter { Id = 1, Name = "Attacker", Sp = 100, Int = 20 };
        var target = new ServerCharacter { Id = 2, Name = "Target", Hp = 100, Wis = 20 };

        // Target has resistance buff
        target.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.BuffStats, 0.5f, 3, "EarthResist"),
            _realStatusEngine);

        var skill = new Skill
        {
            SkillId = 100,
            SpCost = 10,
            Effects = new List<SkillEffect>
            {
                new()
                {
                    Tag = SkillEffectTag.Seal,
                    Value = 0,
                    Duration = 4,
                    ResistanceTags = new List<string> { "EarthResist" },
                    Outcome = OutcomeModel.Partial
                }
            }
        };

        _skillCatalogMock.Setup(x => x.GetSkillById(100)).Returns(skill);
        // NextFloat() call for resistance check returns 0.0 (fail roll, < 0.5 resistance)
        _randomMock.Setup(x => x.NextFloat()).Returns(0.0f);
        // Other random calls
        _randomMock.Setup(x => x.NextFloat(It.IsAny<float>(), It.IsAny<float>())).Returns(1.0f);

        _combatManager.RegisterCombatant(attacker);
        _combatManager.RegisterCombatant(target);

        // Act
        var result = _combatManager.UseSkill(new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = 100 });

        // Assert
        Assert.NotNull(result);
        Assert.Single(result[0].AddedEffects);
        var effect = result[0].AddedEffects[0];
        Assert.Equal(2, effect.TurnsRemaining); // Halved from 4
    }

    [Fact]
    public void UseSkill_OutcomeImmunity_PreventsEffect()
    {
        // Arrange
        var attacker = new ServerCharacter { Id = 1, Sp = 100 };
        var target = new ServerCharacter { Id = 2 };

        // Target has 100% resistance
        target.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.BuffStats, 1.0f, 3, "EarthResist"),
            _realStatusEngine);

        var skill = new Skill
        {
            SkillId = 100,
            SpCost = 10,
            Effects = new List<SkillEffect>
            {
                new()
                {
                    Tag = SkillEffectTag.Seal,
                    ResistanceTags = new List<string> { "EarthResist" },
                    Outcome = OutcomeModel.Immunity
                }
            }
        };

        _skillCatalogMock.Setup(x => x.GetSkillById(100)).Returns(skill);

        _combatManager.RegisterCombatant(attacker);
        _combatManager.RegisterCombatant(target);

        // Act
        var result = _combatManager.UseSkill(new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = 100 });

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result[0].AddedEffects);
    }

    [Fact]
    public void UseSkill_Cooldown_PreventsUsage()
    {
        // Arrange
        var attacker = new ServerCharacter { Id = 1, Sp = 100 };
        var target = new ServerCharacter { Id = 2 };
        var skill = new Skill { SkillId = 101, SpCost = 10, Cooldown = 2, Effects = new List<SkillEffect>() };

        _skillCatalogMock.Setup(x => x.GetSkillById(101)).Returns(skill);
        _combatManager.RegisterCombatant(attacker);
        _combatManager.RegisterCombatant(target);

        // Act 1: First Use
        var result1 = _combatManager.UseSkill(new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = 101 });
        Assert.NotNull(result1);
        Assert.True(attacker.IsSkillOnCooldown(101));

        // Act 2: Second Use (Should fail)
        var result2 = _combatManager.UseSkill(new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = 101 });
        Assert.Empty(result2);

        // Act 3: Tick
        attacker.TickCooldowns(); // 1 turn left
        attacker.TickCooldowns(); // 0 turns left

        // Act 4: Third Use (Should success)
        var result3 = _combatManager.UseSkill(new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = 101 });
        Assert.NotNull(result3);
    }

    [Fact]
    public void StatusEngine_StackUpToN_IncrementsStacks()
    {
        var effects = new List<StatusEffectInstance>();

        var effect1 = new StatusEffectInstance(SkillEffectTag.Burn, 10, 3)
        {
            StackingPolicy = StackingPolicy.StackUpToN,
            MaxStacks = 3,
            SourceSkillId = 10
        };

        // Act 1
        _realStatusEngine.Apply(effects, effect1);
        Assert.Single(effects);
        Assert.Equal(1, effects[0].StackCount);
        Assert.Equal(10, effects[0].Value);

        // Act 2
        var effect2 = new StatusEffectInstance(SkillEffectTag.Burn, 10, 3)
        {
            StackingPolicy = StackingPolicy.StackUpToN,
            MaxStacks = 3,
            SourceSkillId = 10
        };
        _realStatusEngine.Apply(effects, effect2);

        Assert.Single(effects);
        Assert.Equal(2, effects[0].StackCount);
        Assert.Equal(20, effects[0].Value);
    }
}