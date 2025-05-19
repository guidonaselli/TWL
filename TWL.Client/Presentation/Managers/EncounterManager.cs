using System;
using System.Collections.Generic;
using TWL.Shared;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Events;
using TWL.Shared.Net;

namespace TWL.Client.Managers;

/// Comprueba cada n-pasos si se dispara un encuentro aleatorio.
public class EncounterManager
{
    private const int MinSteps = 50; // pasos mínimos antes de poder luchar
    private const float EncounterChance = 0.05f;

    private static readonly Random _rng = new();
    private bool _firstTick = true;

    private int _stepsSinceLast;

    public void CheckEncounter(PlayerCharacter player)
    {
        if (_firstTick)
        {
            _firstTick = false;
            return;
        }

        _stepsSinceLast++;
        if (_stepsSinceLast < MinSteps) return;

        if (_rng.NextDouble() < EncounterChance)
        {
            _stepsSinceLast = 0;
            StartBattle(player);
        }
    }

    // ------------------------------------------------------------------
    private void StartBattle(PlayerCharacter player)
    {
        // por ahora siempre 1 enemigo Slime genérico
        var slime = new EnemyCharacter("Slime", Element.Earth, false)
        {
            Health = 30, MaxHealth = 30, Str = 4
        };

        var allies = new List<PlayerCharacter> { player };
        var enemies = new List<EnemyCharacter> { slime };

        EventBus.Publish(new BattleStarted(allies, enemies));
    }
}