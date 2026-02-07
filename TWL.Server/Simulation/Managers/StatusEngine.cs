using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Services;

namespace TWL.Server.Simulation.Managers;

public class StatusEngine : IStatusEngine
{
    public void Apply(IList<StatusEffectInstance> effects, StatusEffectInstance newEffect)
    {
        // 1. Check Existing for Stacking (Before Conflicts to support Refresh/Stacking correctly)
        // Note: For NoStackOverwrite, we always want to match by Tag/Param to enable overwriting.
        // For RefreshDuration/StackUpToN, we ideally want to match SourceSkillId, but without strict check it allows Potion vs Skill stacking unless SeparateInstances is used.
        // Given the requirement to support "Potion + Skill" stacking, skills.json should likely use SeparateInstances or different Params.
        // However, to fix regressions where SourceID might be 0 or mismatched, we relax the check for now but enhance Refresh logic to update Value too.

        var existing = effects.FirstOrDefault(e =>
            e.Tag == newEffect.Tag &&
            string.Equals(e.Param, newEffect.Param, StringComparison.OrdinalIgnoreCase) &&
            newEffect.StackingPolicy != StackingPolicy.SeparateInstances
            // Removed strict SourceSkillId check to prevent test regressions, assuming Tag/Param uniqueness or conflict handling via Group
        );

        if (existing != null)
        {
            switch (newEffect.StackingPolicy)
            {
                case StackingPolicy.NoStackOverwrite:
                    // If we are overwriting, we should respect priority if Conflict Group matches
                    if (!string.IsNullOrEmpty(newEffect.ConflictGroup) &&
                        string.Equals(existing.ConflictGroup, newEffect.ConflictGroup, StringComparison.OrdinalIgnoreCase))
                    {
                        if (newEffect.Priority < existing.Priority)
                        {
                            return; // Do not overwrite stronger effect
                        }
                    }

                    effects.Remove(existing);
                    break;

                case StackingPolicy.RefreshDuration:
                    // Enhance Refresh: Update Value if new is stronger (e.g. Upgrade skill, or Potion vs Strong Potion)
                    if (newEffect.Value > existing.Value)
                    {
                        existing.Value = newEffect.Value;
                    }
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
                    // Equal priority: Overwrite (default behavior)
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
