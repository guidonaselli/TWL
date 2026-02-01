using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Domain.Skills;
using TWL.Tests.Mocks;

namespace TWL.Tests.Domain.Skills;

public class SkillRulesTests
{
    private readonly StatusEngine _statusEngine = new();

    [Fact]
    public void StackingPolicy_StackUpToN_IncrementsAndRefreshes()
    {
        var effects = new List<StatusEffectInstance>();
        var effect1 = new StatusEffectInstance(SkillEffectTag.BuffStats, 10, 3)
        {
            StackingPolicy = StackingPolicy.StackUpToN,
            MaxStacks = 3,
            Param = "Atk",
            Tag = SkillEffectTag.BuffStats
        };

        // Apply first stack
        _statusEngine.Apply(effects, effect1);
        Assert.Single(effects);
        Assert.Equal(1, effects[0].StackCount);
        Assert.Equal(10, effects[0].Value);
        Assert.Equal(3, effects[0].TurnsRemaining);

        // Apply second stack (simulate time passed: 2 turns left -> should refresh to 3)
        effects[0].TurnsRemaining = 2;
        var effect2 = new StatusEffectInstance(SkillEffectTag.BuffStats, 10, 3)
        {
            StackingPolicy = StackingPolicy.StackUpToN,
            MaxStacks = 3,
            Param = "Atk",
            Tag = SkillEffectTag.BuffStats
        };

        _statusEngine.Apply(effects, effect2);
        Assert.Single(effects);
        Assert.Equal(2, effects[0].StackCount);
        Assert.Equal(20, effects[0].Value);
        Assert.Equal(3, effects[0].TurnsRemaining);
    }

    [Fact]
    public void StackingPolicy_NoStackOverwrite_ReplacesExisting()
    {
        var effects = new List<StatusEffectInstance>();
        var effect1 = new StatusEffectInstance(SkillEffectTag.Burn, 50, 2)
        {
            StackingPolicy = StackingPolicy.NoStackOverwrite,
            Param = "Fire",
            Tag = SkillEffectTag.Burn
        };

        _statusEngine.Apply(effects, effect1);

        // New stronger effect
        var effect2 = new StatusEffectInstance(SkillEffectTag.Burn, 100, 3)
        {
            StackingPolicy = StackingPolicy.NoStackOverwrite,
            Param = "Fire",
            Tag = SkillEffectTag.Burn
        };

        _statusEngine.Apply(effects, effect2);

        Assert.Single(effects);
        Assert.Equal(100, effects[0].Value);
        Assert.Equal(3, effects[0].TurnsRemaining);
    }

    [Fact]
    public void Resistance_PartialOutcome_ReducesDurationAndMagnitude()
    {
        // Arrange
        // Mock Random to return 0.1f. If resistance is 0.5f, 0.1 < 0.5 -> Resistance Triggered.
        var mockRandom = new MockRandomService(0.1f);
        var catalog = new MockSkillCatalog();

        var skillId = 100;
        var skill = new Skill
        {
            SkillId = skillId,
            SpCost = 0,
            TargetType = SkillTargetType.SingleEnemy,
            Effects = new List<SkillEffect>
            {
                new()
                {
                    Tag = SkillEffectTag.DebuffStats,
                    Value = 50,
                    Duration = 4,
                    Param = "Spd",
                    Outcome = OutcomeModel.Partial,
                    ResistanceTags = new List<string> { "SpdResist" }
                }
            }
        };
        catalog.AddSkill(skill);

        var resolver = new StandardCombatResolver(mockRandom, catalog);
        var manager = new CombatManager(resolver, mockRandom, catalog, _statusEngine);

        var attacker = new ServerCharacter { Id = 1, Name = "Attacker" };
        var target = new ServerCharacter { Id = 2, Name = "Target", Hp = 100 };

        // Add 50% resistance
        target.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.BuffStats, 0.5f, 99, "SpdResist"),
            _statusEngine);

        manager.AddCharacter(attacker);
        manager.AddCharacter(target);

        // Act
        var result = manager.UseSkill(new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = skillId });

        // Assert
        Assert.NotNull(result);
        var applied = result[0].AddedEffects[0];
        // Partial: Duration / 2 -> 4/2 = 2. Value * 0.5 -> 50 * 0.5 = 25.
        Assert.Equal(2, applied.TurnsRemaining);
        Assert.Equal(25, applied.Value);
    }

    [Fact]
    public void Resistance_FullOutcome_BlocksApplication()
    {
        // Arrange
        // Mock Random to return 0.1f. Resistance 0.5f. 0.1 < 0.5 -> Resisted.
        var mockRandom = new MockRandomService(0.1f);
        var catalog = new MockSkillCatalog();

        var skillId = 101;
        var skill = new Skill
        {
            SkillId = skillId,
            SpCost = 0,
            TargetType = SkillTargetType.SingleEnemy,
            Effects = new List<SkillEffect>
            {
                new()
                {
                    Tag = SkillEffectTag.Seal,
                    Value = 0,
                    Duration = 2,
                    Outcome = OutcomeModel.Resist, // Default full resist
                    ResistanceTags = new List<string> { "SealResist" }
                }
            }
        };
        catalog.AddSkill(skill);

        var resolver = new StandardCombatResolver(mockRandom, catalog);
        var manager = new CombatManager(resolver, mockRandom, catalog, _statusEngine);

        var attacker = new ServerCharacter { Id = 1, Name = "Attacker" };
        var target = new ServerCharacter { Id = 2, Name = "Target", Hp = 100 };

        // Add 50% resistance
        target.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.BuffStats, 0.5f, 99, "SealResist"),
            _statusEngine);

        manager.AddCharacter(attacker);
        manager.AddCharacter(target);

        // Act
        var result = manager.UseSkill(new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = skillId });

        // Assert
        Assert.Empty(result[0].AddedEffects); // Should be blocked
    }
}