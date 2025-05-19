using System;
using System.Collections.Generic;
using System.Linq;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Events;
using TWL.Shared.Domain.Models;
using TWL.Shared.Net;

namespace TWL.Client.Presentation.Managers;

public enum LocalBattleState
{
    AwaitingInput,
    ResolvingAI,
    Finished
}

public class OfflineCombatManager
{
    // ---------- datos internos ----------
    private readonly List<PlayerCharacter> _allies;
    private readonly List<EnemyCharacter> _enemies;
    private int _turnIndex;

    public OfflineCombatManager(IEnumerable<PlayerCharacter> allies,
        IEnumerable<EnemyCharacter> enemies)
    {
        _allies = allies.ToList();
        _enemies = enemies.ToList();
    }

    // ---------- API pública ----------
    public string LastMessage { get; private set; } = "Battle start!";
    public LocalBattleState State { get; private set; } = LocalBattleState.AwaitingInput;

    public PlayerCharacter CurrentActor => _allies[_turnIndex % _allies.Count];

    // ------------------------------------------------------------------
    // 1. Acción del jugador
    // ------------------------------------------------------------------
    public void PlayerAttack(EnemyCharacter target)
    {
        if (State != LocalBattleState.AwaitingInput || target.Health <= 0) return;

        var dmg = CalcDamage(CurrentActor.Str, target.CalculateDefense());
        target.Health = Math.Max(0, target.Health - dmg);

        LastMessage = $"{CurrentActor.Name} hits {target.Name} for {dmg}";
        _turnIndex++;
        State = LocalBattleState.ResolvingAI;
    }

    // ------------------------------------------------------------------
    // 2. Tick por frame
    // ------------------------------------------------------------------
    public void Tick()
    {
        if (State == LocalBattleState.Finished) return;

        if (IsBattleOver())
        {
            PublishFinish();
            return;
        }

        // turno IA
        if (State == LocalBattleState.ResolvingAI)
        {
            var enemy = _enemies.FirstOrDefault(e => e.Health > 0);
            var ally = _allies.FirstOrDefault(a => a.Health > 0);

            if (enemy != null && ally != null)
            {
                var dmg = CalcDamage(enemy.Str, ally.CalculateDefense());
                ally.Health = Math.Max(0, ally.Health - dmg);

                LastMessage = $"{enemy.Name} hits {ally.Name} for {dmg}";
            }

            _turnIndex++;
            State = LocalBattleState.AwaitingInput;
        }
    }

    // ------------------------------------------------------------------
    // 3. Auxiliares
    // ------------------------------------------------------------------
    private static int CalcDamage(int atk, int def)
    {
        var dmg = atk * 2 - def;
        return dmg < 1 ? 1 : dmg;
    }

    private bool IsBattleOver()
    {
        return _enemies.All(e => e.Health <= 0) ||
               _allies.All(a => a.Health <= 0);
    }

    private void PublishFinish()
    {
        State = LocalBattleState.Finished;

        var victory = _enemies.All(e => e.Health <= 0);
        var exp = victory ? 20 : 0;
        var loot = victory ? LootTable.RollCommonChest() : new List<Item>();

        EventBus.Publish(new BattleFinished(victory, exp, loot));
        LastMessage = victory ? "Victory!" : "Defeat...";
    }
}