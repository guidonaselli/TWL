using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Requests;

namespace TWL.Server.Simulation.Managers;

/// <summary>
/// Defines the contract for resolving combat actions, such as damage calculation.
/// </summary>
public interface ICombatResolver
{
    /// <summary>
    /// Calculates the damage to be dealt by an attacker to a target using a specific skill request.
    /// This method is pure calculation and does not apply the damage or modify state.
    /// </summary>
    /// <param name="attacker">The character initiating the attack.</param>
    /// <param name="target">The character receiving the attack.</param>
    /// <param name="request">The skill request details.</param>
    /// <returns>The calculated damage amount.</returns>
    int CalculateDamage(ServerCharacter attacker, ServerCharacter target, UseSkillRequest request);

    /// <summary>
    /// Calculates the healing amount to be applied by a healer to a target using a specific skill request.
    /// This method is pure calculation and does not apply the heal or modify state.
    /// </summary>
    /// <param name="healer">The character initiating the heal.</param>
    /// <param name="target">The character receiving the heal.</param>
    /// <param name="request">The skill request details.</param>
    /// <returns>The calculated heal amount.</returns>
    int CalculateHeal(ServerCharacter healer, ServerCharacter target, UseSkillRequest request);
}
