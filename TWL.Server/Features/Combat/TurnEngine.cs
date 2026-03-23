using TWL.Server.Simulation.Networking;
using TWL.Shared.Services;

namespace TWL.Server.Features.Combat;

public class TurnEngine : ITurnEngine
{
    private readonly IRandomService _random;
    private readonly IStatusEngine? _statusEngine;
    private readonly List<ServerCombatant> _combatants = new();
    private readonly List<ServerCombatant> _aliveBuffer = new();
    private readonly Queue<ServerCombatant> _turnQueue = new();

    public IReadOnlyList<ServerCombatant> Participants => _combatants;
    public ServerCombatant? CurrentCombatant { get; private set; }
    public long LastActionTick { get; set; }

    public TurnEngine(IRandomService random, IStatusEngine? statusEngine = null)
    {
        _random = random;
        _statusEngine = statusEngine;
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
                if (_statusEngine != null)
                {
                    CurrentCombatant.TickStatusEffects(_statusEngine);
                }
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
        _aliveBuffer.Clear();
        foreach (var c in _combatants)
        {
            if (c.Hp > 0)
            {
                _aliveBuffer.Add(c);
            }
        }

        if (_aliveBuffer.Count == 0)
        {
            _turnQueue.Clear();
            return;
        }

        // 1. Deterministic Shuffle for Tie-Breaking
        // Fisher-Yates shuffle
        int n = _aliveBuffer.Count;
        for (int i = 0; i < n - 1; i++)
        {
            // Next(min, max) excludes max, so we want range [i, n)
            int r = _random.Next(i, n, "TurnShuffle");
            (_aliveBuffer[r], _aliveBuffer[i]) = (_aliveBuffer[i], _aliveBuffer[r]);
        }

        // 2. Stable Sort by SPD Descending using Insertion Sort
        for (int i = 1; i < n; i++)
        {
            var key = _aliveBuffer[i];
            int j = i - 1;

            // Stable descending sort: keep elements with same Spd in their relative shuffled order.
            while (j >= 0 && _aliveBuffer[j].Spd < key.Spd)
            {
                _aliveBuffer[j + 1] = _aliveBuffer[j];
                j--;
            }
            _aliveBuffer[j + 1] = key;
        }

        _turnQueue.Clear();
        foreach (var c in _aliveBuffer)
        {
            _turnQueue.Enqueue(c);
        }
    }
}
