using TWL.Server.Simulation.Managers;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Skills;

namespace TWL.Tests.Skills;

public class StatusEngineTests
{
    private readonly StatusEngine _statusEngine;

    public StatusEngineTests()
    {
        _statusEngine = new StatusEngine();
    }

    [Fact]
    public void Apply_StackUpToN_IncrementsStackAndValue()
    {
        // Arrange
        var effects = new List<StatusEffectInstance>();
        var existing = new StatusEffectInstance(SkillEffectTag.Burn, 10, 3)
        {
            StackingPolicy = StackingPolicy.StackUpToN,
            MaxStacks = 3,
            StackCount = 1,
            Param = "Fire"
        };
        effects.Add(existing);

        var newEffect = new StatusEffectInstance(SkillEffectTag.Burn, 10, 3)
        {
            StackingPolicy = StackingPolicy.StackUpToN,
            MaxStacks = 3,
            Param = "Fire"
        };

        // Act
        _statusEngine.Apply(effects, newEffect);

        // Assert
        Assert.Single(effects);
        Assert.Equal(2, effects[0].StackCount);
        Assert.Equal(20, effects[0].Value);
        Assert.Equal(3, effects[0].TurnsRemaining);
    }

    [Fact]
    public void Apply_StackUpToN_ReachesMaxStacks()
    {
        // Arrange
        var effects = new List<StatusEffectInstance>();
        var existing = new StatusEffectInstance(SkillEffectTag.Burn, 10, 3)
        {
            StackingPolicy = StackingPolicy.StackUpToN,
            MaxStacks = 2,
            StackCount = 2,
            Value = 20,
            Param = "Fire"
        };
        effects.Add(existing);

        var newEffect = new StatusEffectInstance(SkillEffectTag.Burn, 10, 3)
        {
            StackingPolicy = StackingPolicy.StackUpToN,
            MaxStacks = 2,
            Param = "Fire"
        };

        // Act
        _statusEngine.Apply(effects, newEffect);

        // Assert
        Assert.Single(effects);
        Assert.Equal(2, effects[0].StackCount); // Should not increase
        Assert.Equal(20, effects[0].Value); // Should not increase
        Assert.Equal(3, effects[0].TurnsRemaining); // Duration refreshes
    }

    [Fact]
    public void Apply_NoStackOverwrite_RespectsPriority()
    {
        // Arrange
        var effects = new List<StatusEffectInstance>();
        var existing = new StatusEffectInstance(SkillEffectTag.BuffStats, 10, 3, "Atk")
        {
            StackingPolicy = StackingPolicy.NoStackOverwrite,
            ConflictGroup = "Buff_Atk",
            Priority = 2
        };
        effects.Add(existing);

        var newEffect = new StatusEffectInstance(SkillEffectTag.BuffStats, 20, 3, "Atk")
        {
            StackingPolicy = StackingPolicy.NoStackOverwrite,
            ConflictGroup = "Buff_Atk",
            Priority = 1 // Lower priority
        };

        // Act
        _statusEngine.Apply(effects, newEffect);

        // Assert
        Assert.Single(effects);
        Assert.Equal(10, effects[0].Value); // Should remain existing
        Assert.Equal(2, effects[0].Priority);
    }

    [Fact]
    public void Apply_NoStackOverwrite_OverwritesLowerPriority()
    {
        // Arrange
        var effects = new List<StatusEffectInstance>();
        var existing = new StatusEffectInstance(SkillEffectTag.BuffStats, 10, 3, "Atk")
        {
            StackingPolicy = StackingPolicy.NoStackOverwrite,
            ConflictGroup = "Buff_Atk",
            Priority = 1
        };
        effects.Add(existing);

        var newEffect = new StatusEffectInstance(SkillEffectTag.BuffStats, 20, 3, "Atk")
        {
            StackingPolicy = StackingPolicy.NoStackOverwrite,
            ConflictGroup = "Buff_Atk",
            Priority = 2 // Higher priority
        };

        // Act
        _statusEngine.Apply(effects, newEffect);

        // Assert
        Assert.Single(effects);
        Assert.Equal(20, effects[0].Value); // Should be new
        Assert.Equal(2, effects[0].Priority);
    }

    [Fact]
    public void Apply_ConflictGroup_RemovesConflicting()
    {
        // Arrange
        var effects = new List<StatusEffectInstance>();
        var existing = new StatusEffectInstance(SkillEffectTag.Seal, 0, 3)
        {
            StackingPolicy = StackingPolicy.NoStackOverwrite,
            ConflictGroup = "HardControl",
            Priority = 1
        };
        effects.Add(existing);

        var newEffect = new StatusEffectInstance(SkillEffectTag.DebuffStats, 0, 3) // Different Tag
        {
            StackingPolicy = StackingPolicy.NoStackOverwrite,
            ConflictGroup = "HardControl", // Same Group
            Priority = 2
        };

        // Act
        _statusEngine.Apply(effects, newEffect);

        // Assert
        Assert.Single(effects);
        Assert.Equal(SkillEffectTag.DebuffStats, effects[0].Tag);
    }
}
