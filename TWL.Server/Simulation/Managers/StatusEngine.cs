using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Services;

namespace TWL.Server.Simulation.Managers;

public class StatusEngine : IStatusEngine
{
    public void Apply(IList<StatusEffectInstance> effects, StatusEffectInstance newEffect)
    {
        // 1. Check Conflicts (Priority)
        if (!string.IsNullOrEmpty(newEffect.ConflictGroup))
        {
            var conflicts = effects.Where(e => e.ConflictGroup == newEffect.ConflictGroup).ToList();
            foreach (var conflict in conflicts)
            {
                if (newEffect.Priority > conflict.Priority)
                {
                    // New effect is stronger, remove old
                    effects.Remove(conflict);
                }
                else if (newEffect.Priority < conflict.Priority)
                {
                    // Existing effect is stronger, ignore new
                    return;
                }
                else
                {
                    // Equal priority: Overwrite logic (last wins)
                    effects.Remove(conflict);
                }
            }
        }

        // 2. Check Existing for Stacking
        var existing = effects.FirstOrDefault(e =>
            e.Tag == newEffect.Tag &&
            string.Equals(e.Param, newEffect.Param, StringComparison.OrdinalIgnoreCase) &&
            newEffect.StackingPolicy != StackingPolicy.SeparateInstances
        );

        if (existing != null)
        {
            switch (newEffect.StackingPolicy)
            {
                case StackingPolicy.NoStackOverwrite:
                    effects.Remove(existing);
                    effects.Add(newEffect);
                    break;

                case StackingPolicy.RefreshDuration:
                    existing.TurnsRemaining = Math.Max(existing.TurnsRemaining, newEffect.TurnsRemaining);
                    break;

                case StackingPolicy.StackUpToN:
                    if (existing.StackCount < newEffect.MaxStacks)
                    {
                        existing.StackCount++;
                        existing.Value += newEffect.Value;
                        // Refresh duration on stack
                        existing.TurnsRemaining = Math.Max(existing.TurnsRemaining, newEffect.TurnsRemaining);
                    }
                    else
                    {
                        // Just refresh if max stack reached
                        existing.TurnsRemaining = Math.Max(existing.TurnsRemaining, newEffect.TurnsRemaining);
                    }

                    break;
            }
        }
        else
        {
            effects.Add(newEffect);
        }
    }

    public void RemoveAll(IList<StatusEffectInstance> effects, Predicate<StatusEffectInstance> match)
    {
        if (effects is List<StatusEffectInstance> list)
        {
            list.RemoveAll(match);
        }
        else
        {
            for (var i = effects.Count - 1; i >= 0; i--)
            {
                if (match(effects[i]))
                {
                    effects.RemoveAt(i);
                }
            }
        }
    }

    public void Tick(IList<StatusEffectInstance> effects)
    {
        for (var i = effects.Count - 1; i >= 0; i--)
        {
            var effect = effects[i];
            effect.TurnsRemaining--;
            if (effect.TurnsRemaining <= 0)
            {
                effects.RemoveAt(i);
            }
        }
    }
}