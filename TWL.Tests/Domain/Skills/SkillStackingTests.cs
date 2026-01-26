using Xunit;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Skills;

namespace TWL.Tests.Domain.Skills;

public class SkillStackingTests
{
    private ServerCharacter _character;

    public SkillStackingTests()
    {
        _character = new ServerCharacter
        {
            Id = 1,
            Name = "TestChar",
            Con = 10,
            Int = 10
        };
    }

    [Fact]
    public void AddStatusEffect_NoStackOverwrite_ShouldReplace()
    {
        var effect1 = new StatusEffectInstance(SkillEffectTag.BuffStats, 10, 3, "Atk")
        {
            StackingPolicy = StackingPolicy.NoStackOverwrite
        };
        var effect2 = new StatusEffectInstance(SkillEffectTag.BuffStats, 20, 2, "Atk")
        {
            StackingPolicy = StackingPolicy.NoStackOverwrite
        };

        _character.AddStatusEffect(effect1);
        Assert.Single(_character.StatusEffects);
        Assert.Equal(10, _character.StatusEffects[0].Value);

        _character.AddStatusEffect(effect2);
        Assert.Single(_character.StatusEffects);
        Assert.Equal(20, _character.StatusEffects[0].Value);
        Assert.Equal(2, _character.StatusEffects[0].TurnsRemaining);
    }

    [Fact]
    public void AddStatusEffect_RefreshDuration_ShouldUpdateDuration()
    {
        var effect1 = new StatusEffectInstance(SkillEffectTag.BuffStats, 10, 2, "Def")
        {
            StackingPolicy = StackingPolicy.RefreshDuration
        };
        var effect2 = new StatusEffectInstance(SkillEffectTag.BuffStats, 10, 5, "Def")
        {
            StackingPolicy = StackingPolicy.RefreshDuration
        };

        _character.AddStatusEffect(effect1);
        _character.AddStatusEffect(effect2);

        Assert.Single(_character.StatusEffects);
        Assert.Equal(10, _character.StatusEffects[0].Value);
        Assert.Equal(5, _character.StatusEffects[0].TurnsRemaining);
    }

    [Fact]
    public void AddStatusEffect_StackUpToN_ShouldIncrementStack()
    {
        // Must create separate instances to simulate distinct applications
        _character.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.Burn, 50, 3, "Fire")
        {
            StackingPolicy = StackingPolicy.StackUpToN,
            MaxStacks = 3
        });
        Assert.Equal(1, _character.StatusEffects[0].StackCount);
        Assert.Equal(50, _character.StatusEffects[0].Value);

        _character.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.Burn, 50, 3, "Fire")
        {
            StackingPolicy = StackingPolicy.StackUpToN,
            MaxStacks = 3
        });
        Assert.Equal(2, _character.StatusEffects[0].StackCount);
        Assert.Equal(100, _character.StatusEffects[0].Value);

        _character.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.Burn, 50, 3, "Fire")
        {
            StackingPolicy = StackingPolicy.StackUpToN,
            MaxStacks = 3
        });
        Assert.Equal(3, _character.StatusEffects[0].StackCount);
        Assert.Equal(150, _character.StatusEffects[0].Value);

        // Max stack reached, only refresh duration
        _character.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.Burn, 50, 3, "Fire")
        {
            StackingPolicy = StackingPolicy.StackUpToN,
            MaxStacks = 3
        });
        Assert.Equal(3, _character.StatusEffects[0].StackCount);
        Assert.Equal(150, _character.StatusEffects[0].Value);
    }

    [Fact]
    public void AddStatusEffect_PriorityConflict_ShouldOverwriteLowerPriority()
    {
        var weakStun = new StatusEffectInstance(SkillEffectTag.Seal, 1, 2, "Stun")
        {
            ConflictGroup = "HardControl",
            Priority = 1
        };

        var strongFreeze = new StatusEffectInstance(SkillEffectTag.Seal, 1, 3, "Freeze")
        {
            ConflictGroup = "HardControl",
            Priority = 10
        };

        _character.AddStatusEffect(weakStun);
        Assert.Single(_character.StatusEffects);
        Assert.Equal("Stun", _character.StatusEffects[0].Param);

        _character.AddStatusEffect(strongFreeze);
        Assert.Single(_character.StatusEffects);
        Assert.Equal("Freeze", _character.StatusEffects[0].Param);
    }

    [Fact]
    public void AddStatusEffect_PriorityConflict_ShouldIgnoreLowerPriority()
    {
        var strongFreeze = new StatusEffectInstance(SkillEffectTag.Seal, 1, 3, "Freeze")
        {
            ConflictGroup = "HardControl",
            Priority = 10
        };

        var weakStun = new StatusEffectInstance(SkillEffectTag.Seal, 1, 2, "Stun")
        {
            ConflictGroup = "HardControl",
            Priority = 1
        };

        _character.AddStatusEffect(strongFreeze);
        _character.AddStatusEffect(weakStun);

        Assert.Single(_character.StatusEffects);
        Assert.Equal("Freeze", _character.StatusEffects[0].Param);
    }
}
