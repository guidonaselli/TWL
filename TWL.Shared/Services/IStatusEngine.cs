using System;
using System.Collections.Generic;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Skills;

namespace TWL.Shared.Services;

/// <summary>
/// Defines the logic for managing status effects (application, stacking, conflicts, expiration).
/// </summary>
public interface IStatusEngine
{
    /// <summary>
    /// Applies a new status effect to the list of existing effects, handling stacking, priority, and conflicts.
    /// Modifies the effects list in place.
    /// </summary>
    /// <param name="effects">The mutable list of current effects.</param>
    /// <param name="newEffect">The new effect to apply.</param>
    void Apply(IList<StatusEffectInstance> effects, StatusEffectInstance newEffect);

    /// <summary>
    /// Removes all effects that match the specified predicate.
    /// </summary>
    /// <param name="effects">The mutable list of current effects.</param>
    /// <param name="match">The predicate to match effects to remove.</param>
    void RemoveAll(IList<StatusEffectInstance> effects, Predicate<StatusEffectInstance> match);

    /// <summary>
    /// Ticks all effects, decreasing duration. Removes effects that have expired (Duration <= 0).
    /// </summary>
    /// <param name="effects">The mutable list of current effects.</param>
    void Tick(IList<StatusEffectInstance> effects);
}
