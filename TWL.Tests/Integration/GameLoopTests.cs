using System;
using System.Collections.Generic;
using System.Linq;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Skills;
using Xunit;

namespace TWL.Tests.Integration;

public class GameLoopTests
{
    [Fact]
    public void FullBattleLoop_Simulation()
    {
        // 1. Setup
        var player = new PlayerCharacter(Guid.NewGuid(), "Hero", Element.Fire);
        player.Agi = 10;
        player.Str = 20; // High attack
        player.Health = 100;
        player.MaxHealth = 100;

        var enemy = new EnemyCharacter("Slime", Element.Earth, false);
        enemy.Health = 50;
        enemy.MaxHealth = 50;
        enemy.Agi = 5;
        enemy.Con = 0;

        var battle = new BattleInstance(new[] { player }, new[] { enemy });

        // 2. Tick until Player Turn
        // Player speed 10, Enemy 5. Player should go first.
        int safety = 0;
        while (battle.CurrentTurnCombatant == null && safety++ < 1000)
        {
            battle.Tick(0.1f);
        }

        Assert.NotNull(battle.CurrentTurnCombatant);
        Assert.Equal(player.Name, battle.CurrentTurnCombatant.Character.Name);

        // 3. Player Attacks
        var action = CombatAction.Attack(battle.CurrentTurnCombatant.BattleId, battle.Enemies[0].BattleId);
        string result = battle.ResolveAction(action);

        Assert.Contains("attacks Slime", result);
        Assert.True(enemy.Health < 50);

        // 4. Tick until Enemy Turn (or next turn)
        // Since enemy took damage but is alive (maybe). 20 Str * 2 = 40 Atk. Enemy Con 0 = 0 Def. Dmg 40. HP 50 -> 10.
        // Enemy should be next.

        // Reset turn
        safety = 0;
        while (battle.CurrentTurnCombatant == null && safety++ < 1000)
        {
            battle.Tick(0.1f);
        }

        Assert.NotNull(battle.CurrentTurnCombatant);
        Assert.Equal(enemy.Name, battle.CurrentTurnCombatant.Character.Name);

        // 5. Enemy Attacks
        action = CombatAction.Attack(battle.CurrentTurnCombatant.BattleId, battle.Allies[0].BattleId);
        result = battle.ResolveAction(action);
        Assert.Contains("attacks Hero", result);
        Assert.True(player.Health < 100);

        // 6. Player Finishes
        safety = 0;
        while (battle.CurrentTurnCombatant == null && safety++ < 1000)
        {
            battle.Tick(0.1f);
        }
        Assert.Equal(player.Name, battle.CurrentTurnCombatant.Character.Name);

        action = CombatAction.Attack(battle.CurrentTurnCombatant.BattleId, battle.Enemies[0].BattleId);
        result = battle.ResolveAction(action);

        Assert.True(enemy.Health <= 0);

        // 7. Verify Victory
        // Tick one more time to check end
        battle.Tick(0.1f);

        Assert.Equal(BattleState.Victory, battle.State);
    }
}
