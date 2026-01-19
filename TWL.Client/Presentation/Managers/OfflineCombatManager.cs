using System;
using System.Collections.Generic;
using System.Linq;
using TWL.Shared.Domain.Battle;
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
    private readonly BattleInstance _battle;
    private bool _finishedPublished = false;

    // Expose for UI
    public BattleInstance Battle => _battle;
    public string LastMessage { get; private set; } = "Battle start!";
    public LocalBattleState State { get; private set; } = LocalBattleState.AwaitingInput;

    public OfflineCombatManager(IEnumerable<PlayerCharacter> allies, IEnumerable<EnemyCharacter> enemies)
    {
        _battle = new BattleInstance(allies, enemies);
    }

    public Combatant CurrentActor => _battle.CurrentTurnCombatant;

    // ------------------------------------------------------------------
    // 1. Actions
    // ------------------------------------------------------------------
    public void PlayerAction(CombatAction action)
    {
        if (_battle.State != TWL.Shared.Domain.Battle.BattleState.Active) return;
        if (CurrentActor == null || !CurrentActor.Character.IsAlly()) return;
        if (action.ActorId != CurrentActor.BattleId) return; // mismatch

        LastMessage = _battle.ResolveAction(action);
    }

    // ------------------------------------------------------------------
    // 2. Tick
    // ------------------------------------------------------------------
    public void Tick(float deltaTime)
    {
        if (_finishedPublished) return;

        _battle.Tick(deltaTime);

        if (_battle.State != TWL.Shared.Domain.Battle.BattleState.Active)
        {
            PublishFinish();
            return;
        }

        var current = _battle.CurrentTurnCombatant;
        if (current != null)
        {
            if (current.Character.IsAlly())
            {
                State = LocalBattleState.AwaitingInput;
            }
            else
            {
                State = LocalBattleState.ResolvingAI;
                PerformAI(current);
            }
        }
        else
        {
            // Waiting for ATB
            State = LocalBattleState.ResolvingAI; // Technically "Waiting", reusing enum or ignoring
        }
    }

    private void PerformAI(Combatant enemy)
    {
        // Simple AI: Attack random living ally
        var target = _battle.Allies
            .Where(a => a.Character.IsAlive())
            .OrderBy(_ => Guid.NewGuid()) // shuffle
            .FirstOrDefault();

        if (target != null)
        {
            var action = CombatAction.Attack(enemy.BattleId, target.BattleId);
            LastMessage = _battle.ResolveAction(action);
        }
        else
        {
            // No targets? Should act to end turn or defend
            var action = CombatAction.Defend(enemy.BattleId);
            LastMessage = _battle.ResolveAction(action);
        }
    }

    public void ForceEndBattle()
    {
        if (_finishedPublished) return;
        _battle.ForceEnd();
        PublishFinish();
    }

    private void PublishFinish()
    {
        _finishedPublished = true;
        State = LocalBattleState.Finished;

        var victory = _battle.State == TWL.Shared.Domain.Battle.BattleState.Victory;
        var exp = victory ? 20 : 0; // Fixed exp for now
        var loot = victory ? LootTable.RollCommonChest() : new List<Item>();

        LastMessage = victory ? "Victory!" : "Defeat...";

        EventBus.Publish(new BattleFinished(victory, exp, loot));
    }
}
