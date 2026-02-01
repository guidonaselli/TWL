using TWL.Server.Simulation.Managers;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Skills;

namespace TWL.Tests.Server.Simulation.Managers;

public class StatusEngineTests
{
    private readonly StatusEngine _engine = new();

    [Fact]
    public void Apply_AddSimpleEffect_AddsToList()
    {
        var list = new List<StatusEffectInstance>();
        var effect = new StatusEffectInstance(SkillEffectTag.BuffStats, 10, 3);

        _engine.Apply(list, effect);

        Assert.Single(list);
        Assert.Equal(effect, list[0]);
    }

    [Fact]
    public void Apply_Conflict_PriorityWins()
    {
        var list = new List<StatusEffectInstance>();
        var weakEffect = new StatusEffectInstance(SkillEffectTag.BuffStats, 10, 3)
            { ConflictGroup = "Group1", Priority = 1 };
        var strongEffect = new StatusEffectInstance(SkillEffectTag.BuffStats, 20, 3)
            { ConflictGroup = "Group1", Priority = 2 };

        _engine.Apply(list, weakEffect);
        Assert.Single(list);
        Assert.Equal(weakEffect, list[0]);

        _engine.Apply(list, strongEffect);
        Assert.Single(list);
        Assert.Equal(strongEffect, list[0]); // Replaced
    }

    [Fact]
    public void Apply_Conflict_LowPriorityIgnored()
    {
        var list = new List<StatusEffectInstance>();
        var strongEffect = new StatusEffectInstance(SkillEffectTag.BuffStats, 20, 3)
            { ConflictGroup = "Group1", Priority = 2 };
        var weakEffect = new StatusEffectInstance(SkillEffectTag.BuffStats, 10, 3)
            { ConflictGroup = "Group1", Priority = 1 };

        _engine.Apply(list, strongEffect);
        _engine.Apply(list, weakEffect);

        Assert.Single(list);
        Assert.Equal(strongEffect, list[0]);
    }

    [Fact]
    public void Apply_Stacking_RefreshDuration()
    {
        var list = new List<StatusEffectInstance>();
        var effect1 = new StatusEffectInstance(SkillEffectTag.Burn, 10, 3)
            { StackingPolicy = StackingPolicy.RefreshDuration, Param = "Fire" };
        var effect2 = new StatusEffectInstance(SkillEffectTag.Burn, 10, 5)
            { StackingPolicy = StackingPolicy.RefreshDuration, Param = "Fire" };

        _engine.Apply(list, effect1);
        _engine.Apply(list, effect2);

        Assert.Single(list);
        Assert.Equal(5, list[0].TurnsRemaining);
    }

    [Fact]
    public void Tick_DecrementsAndRemovesExpired()
    {
        var list = new List<StatusEffectInstance>
        {
            new(SkillEffectTag.BuffStats, 10, 1),
            new(SkillEffectTag.BuffStats, 10, 2)
        };

        _engine.Tick(list);

        Assert.Single(list); // First expired (1-1=0 -> remove)
        Assert.Equal(1, list[0].TurnsRemaining);
    }
}