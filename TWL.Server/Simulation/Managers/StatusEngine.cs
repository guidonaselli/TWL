using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Services;

namespace TWL.Server.Simulation.Managers;

public class StatusEngine : IStatusEngine
{
    public void Apply(IList<StatusEffectInstance> effects, StatusEffectInstance newEffect)
    {
        // 1. Check Existing for Stacking (Before Conflicts to support Refresh/Stacking correctly)
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
                    // Fallthrough to conflict/overwrite logic?
                    // Or just handle it here: Remove existing, then proceed to add new (checking conflicts again?)
                    // If we remove existing here, we still need to check conflicts with OTHERS.
                    // But usually NoStackOverwrite implies we just replace THIS one.
                    // But if there are *other* conflicts (different Tag/Param but same Group), we need to check them.
                    // So we remove existing, then treat newEffect as fresh.
                    effects.Remove(existing);
                    // existing is null now effectively for the logic below
                    break;

                case StackingPolicy.RefreshDuration:
                    existing.TurnsRemaining = Math.Max(existing.TurnsRemaining, newEffect.TurnsRemaining);
                    return; // Done

                case StackingPolicy.StackUpToN:
                    if (existing.StackCount < newEffect.MaxStacks)
                    {
                        existing.StackCount++;
                        existing.Value += newEffect.Value;
                        existing.TurnsRemaining = Math.Max(existing.TurnsRemaining, newEffect.TurnsRemaining);
                    }
                    else
                    {
                        existing.TurnsRemaining = Math.Max(existing.TurnsRemaining, newEffect.TurnsRemaining);
                    }
                    return; // Done
            }
        }

        // 2. Check Conflicts (Priority)
        if (!string.IsNullOrEmpty(newEffect.ConflictGroup))
        {
            var conflicts = effects.Where(e => e.ConflictGroup == newEffect.ConflictGroup).ToList();
            foreach (var conflict in conflicts)
            {
                if (newEffect.Priority > conflict.Priority)
                {
                    effects.Remove(conflict);
                }
                else if (newEffect.Priority < conflict.Priority)
                {
                    return;
                }
                else
                {
                    effects.Remove(conflict);
                }
            }
        }

        // 3. Add New
        effects.Add(newEffect);
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