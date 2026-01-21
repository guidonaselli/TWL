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
    private const int MinSteps = 120; // about 2 seconds of walking at 60fps
    private const float EncounterChance = 0.005f; // 0.5% chance per frame (approx 30% per second)

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

    public void ForceEncounter(PlayerCharacter player)
    {
        StartBattle(player);
    }

    // ------------------------------------------------------------------
    private void StartBattle(PlayerCharacter player)
    {
        var enemies = new List<EnemyCharacter>();
        int count = _rng.Next(1, 4); // 1 to 3

        for (int i = 0; i < count; i++)
        {
            int type = _rng.Next(0, 3);
            if (type == 0)
            {
                enemies.Add(new EnemyCharacter("Slime", Element.Earth, false) {
                    Health = 30, MaxHealth = 30, Str = 4, Agi = 3, Level = 1
                });
            }
            else if (type == 1)
            {
                enemies.Add(new EnemyCharacter("Wolf", Element.Wind, false) {
                    Health = 50, MaxHealth = 50, Str = 6, Agi = 6, Level = 3
                });
            }
            else
            {
                enemies.Add(new EnemyCharacter("Bat", Element.Wind, false) {
                    Health = 20, MaxHealth = 20, Str = 3, Agi = 8, Level = 2
                });
            }
        }

        var allies = new List<PlayerCharacter> { player };

        EventBus.Publish(new BattleStarted(allies, enemies));
    }
}
