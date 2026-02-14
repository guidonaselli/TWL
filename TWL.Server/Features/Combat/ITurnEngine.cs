using TWL.Server.Simulation.Networking;

namespace TWL.Server.Features.Combat;

public interface ITurnEngine
{
    /// <summary>
    /// Gets all participants in the encounter.
    /// </summary>
    IReadOnlyList<ServerCombatant> Participants { get; }

    /// <summary>
    /// Gets the combatant whose turn it currently is.
    /// </summary>
    ServerCombatant? CurrentCombatant { get; }

    /// <summary>
    /// Tracks the world tick when the last action occurred (or turn started).
    /// Used for AI pacing and player timeouts.
    /// </summary>
    long LastActionTick { get; set; }

    /// <summary>
    /// Initializes the turn order for a new encounter.
    /// </summary>
    /// <param name="combatants">The participants in the encounter.</param>
    void StartEncounter(IEnumerable<ServerCombatant> combatants);

    /// <summary>
    /// Advances to the next turn and returns the active combatant.
    /// triggers TickCooldowns on the new active combatant.
    /// </summary>
    ServerCombatant? NextTurn();

    /// <summary>
    /// Registers that the current combatant has finished their action.
    /// In a simple round-robin, this might just call NextTurn(),
    /// but in CT systems it might recalculate order.
    /// </summary>
    void EndTurn();

    /// <summary>
    /// Adds a new combatant to the ongoing encounter (e.g. summon).
    /// </summary>
    void AddCombatant(ServerCombatant combatant);

    /// <summary>
    /// Removes a combatant (e.g. death/flee).
    /// </summary>
    void RemoveCombatant(int combatantId);
}
