using TWL.Client.Presentation.Services;
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
    private bool _finishedPublished;

    public OfflineCombatManager(IEnumerable<PlayerCharacter> allies, IEnumerable<EnemyCharacter> enemies)
    {
        Battle = new BattleInstance(allies, enemies);
    }

    // Expose for UI
    public BattleInstance Battle { get; }

    public string LastMessage { get; private set; } = Loc.T("UI_BATTLE_START");
    public LocalBattleState State { get; private set; } = LocalBattleState.AwaitingInput;

    public Combatant CurrentActor => Battle.CurrentTurnCombatant;

    // ------------------------------------------------------------------
    // 1. Actions
    // ------------------------------------------------------------------
    public void PlayerAction(CombatAction action)
    {
        if (Battle.State != BattleState.Active)
        {
            return;
        }

        if (CurrentActor == null || !CurrentActor.Character.IsAlly())
        {
            return;
        }

        if (action.ActorId != CurrentActor.BattleId)
        {
            return; // mismatch
        }

        LastMessage = Battle.ResolveAction(action);
    }

    // ------------------------------------------------------------------
    // 2. Tick
    // ------------------------------------------------------------------
    public void Tick(float deltaTime)
    {
        if (_finishedPublished)
        {
            return;
        }

        Battle.Tick(deltaTime);

        if (Battle.State != BattleState.Active)
        {
            PublishFinish();
            return;
        }

        var current = Battle.CurrentTurnCombatant;
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
        var target = Battle.Allies
            .Where(a => a.Character.IsAlive())
            .OrderBy(_ => Guid.NewGuid()) // shuffle
            .FirstOrDefault();

        if (target != null)
        {
            var action = CombatAction.Attack(enemy.BattleId, target.BattleId);
            LastMessage = Battle.ResolveAction(action);
        }
        else
        {
            // No targets? Should act to end turn or defend
            var action = CombatAction.Defend(enemy.BattleId);
            LastMessage = Battle.ResolveAction(action);
        }
    }

    public void ForceEndBattle()
    {
        if (_finishedPublished)
        {
            return;
        }

        Battle.ForceEnd();
        PublishFinish();
    }

    private void PublishFinish()
    {
        _finishedPublished = true;
        State = LocalBattleState.Finished;

        var victory = Battle.State == BattleState.Victory;
        var exp = 0;
        if (victory)
        {
            foreach (var enemy in Battle.Enemies)
            {
                if (enemy.Character is EnemyCharacter ec)
                {
                    exp += ec.ExpReward;
                }
            }
        }

        var loot = victory ? LootTable.RollCommonChest() : new List<Item>();

        LastMessage = victory ? Loc.T("UI_BATTLE_VICTORY") : Loc.T("UI_BATTLE_DEFEAT");

        EventBus.Publish(new BattleFinished(victory, exp, loot));
    }
}