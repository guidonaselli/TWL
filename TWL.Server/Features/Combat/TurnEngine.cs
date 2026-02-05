using TWL.Server.Simulation.Networking;
using TWL.Shared.Services;

namespace TWL.Server.Features.Combat;

public class TurnEngine : ITurnEngine
{
    private readonly IRandomService _random;
    private readonly List<ServerCombatant> _combatants = new();
    private Queue<ServerCombatant> _turnQueue = new();

    public ServerCombatant? CurrentCombatant { get; private set; }

    public TurnEngine(IRandomService random)
    {
        _random = random;
    }

    public void StartEncounter(IEnumerable<ServerCombatant> combatants)
    {
        _combatants.Clear();
        _combatants.AddRange(combatants);
        StartNewRound();
    }

    public ServerCombatant? NextTurn()
    {
        while (true)
        {
            if (_turnQueue.Count == 0)
            {
                StartNewRound();
                // If still empty after trying to start new round, it means no one is alive.
                if (_turnQueue.Count == 0)
                {
                    CurrentCombatant = null;
                    return null;
                }
            }

            var candidate = _turnQueue.Dequeue();

            // Skip dead combatants
            if (candidate.Hp > 0)
            {
                CurrentCombatant = candidate;
                CurrentCombatant.TickCooldowns();
                return CurrentCombatant;
            }
        }
    }

    public void EndTurn()
    {
        CurrentCombatant = null;
    }

    public void AddCombatant(ServerCombatant combatant)
    {
        _combatants.Add(combatant);
    }

    public void RemoveCombatant(int combatantId)
    {
        _combatants.RemoveAll(c => c.Id == combatantId);
    }

    private void StartNewRound()
    {
        var alive = _combatants.Where(c => c.Hp > 0).ToList();

        if (alive.Count == 0)
        {
            _turnQueue.Clear();
            return;
        }

        // 1. Deterministic Shuffle for Tie-Breaking
        // Fisher-Yates shuffle
        int n = alive.Count;
        for (int i = 0; i < n - 1; i++)
        {
            // Next(min, max) excludes max, so we want range [i, n)
            int r = _random.Next(i, n);
            (alive[r], alive[i]) = (alive[i], alive[r]);
        }

        // 2. Stable Sort by SPD Descending
        // LINQ OrderByDescending is stable.
        var sorted = alive.OrderByDescending(c => c.Spd).ToList();

        _turnQueue = new Queue<ServerCombatant>(sorted);
    }
}
