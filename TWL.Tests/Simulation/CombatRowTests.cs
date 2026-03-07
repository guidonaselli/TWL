using System.Collections.Generic;
using System.Linq;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Party;
using Xunit;

namespace TWL.Tests.Simulation;

public class TestCharacter : Character
{
    public TestCharacter(int id, string name) : base(name, Element.None)
    {
        Id = id;
    }
}

public class CombatRowTests
{
    private Character CreateCharacter(int id, string name)
    {
        var character = new TestCharacter(id, name)
        {
            Str = 50,
            Con = 10,
            Int = 10,
            Wis = 10,
            Agi = 100 // High Spd to move first
        };
        return character;
    }

    [Fact]
    public void CombatRow_DamageScaling_FrontMidBack()
    {
        // Arrange
        var attackerChar = CreateCharacter(1, "Attacker");
        var targetFrontChar = CreateCharacter(2, "FrontTarget");
        var targetMidChar = CreateCharacter(3, "MidTarget");
        var targetBackChar = CreateCharacter(4, "BackTarget");

        var allies = new List<Character> { attackerChar };
        var enemies = new List<Character> { targetFrontChar, targetMidChar, targetBackChar };

        var battle = new BattleInstance(allies, enemies);

        var attacker = battle.Allies.First();
        attacker.GridPosition = new GridPosition(0, 0); // Front

        var targetFront = battle.Enemies.First(c => c.Id == 2);
        targetFront.GridPosition = new GridPosition(0, 0);

        var targetMid = battle.Enemies.First(c => c.Id == 3);
        targetMid.GridPosition = new GridPosition(1, 0);

        var targetBack = battle.Enemies.First(c => c.Id == 4);
        targetBack.GridPosition = new GridPosition(2, 0);

        // Force attacker turn
        attacker.Atb = 100;
        battle.Tick(0.1f); // Set CurrentTurnCombatant

        // Act - Front
        int hpBeforeFront = targetFront.Hp;
        battle.ResolveAction(new CombatAction { ActorId = attacker.BattleId, TargetId = targetFront.BattleId, Type = CombatActionType.Attack });
        int damageFront = hpBeforeFront - targetFront.Hp;

        // Reset turn
        attacker.Atb = 100;
        battle.Tick(0.1f);

        // Act - Mid
        int hpBeforeMid = targetMid.Hp;
        battle.ResolveAction(new CombatAction { ActorId = attacker.BattleId, TargetId = targetMid.BattleId, Type = CombatActionType.Attack });
        int damageMid = hpBeforeMid - targetMid.Hp;

        // Reset turn
        attacker.Atb = 100;
        battle.Tick(0.1f);

        // Act - Back
        int hpBeforeBack = targetBack.Hp;
        battle.ResolveAction(new CombatAction { ActorId = attacker.BattleId, TargetId = targetBack.BattleId, Type = CombatActionType.Attack });
        int damageBack = hpBeforeBack - targetBack.Hp;

        // Assert
        Assert.True(damageFront > damageMid, $"Front damage ({damageFront}) should be > Mid ({damageMid})");
        Assert.True(damageMid > damageBack, $"Mid damage ({damageMid}) should be > Back ({damageBack})");
    }

    [Fact]
    public void CombatRow_AttackerRow_AlsoScalesDamage()
    {
        // Arrange
        var targetChar = CreateCharacter(1, "Target");
        var attackerFrontChar = CreateCharacter(2, "FrontAttacker");
        var attackerMidChar = CreateCharacter(3, "MidAttacker");
        var attackerBackChar = CreateCharacter(4, "BackAttacker");

        var allies = new List<Character> { attackerFrontChar, attackerMidChar, attackerBackChar };
        var enemies = new List<Character> { targetChar };

        var battle = new BattleInstance(allies, enemies);

        var target = battle.Enemies.First();
        target.GridPosition = new GridPosition(0, 0); // Target always Front row

        var attackerFront = battle.Allies.First(c => c.Id == 2);
        attackerFront.GridPosition = new GridPosition(0, 0);

        var attackerMid = battle.Allies.First(c => c.Id == 3);
        attackerMid.GridPosition = new GridPosition(1, 0);

        var attackerBack = battle.Allies.First(c => c.Id == 4);
        attackerBack.GridPosition = new GridPosition(2, 0);

        // Front Attack
        attackerFront.Atb = 100;
        battle.Tick(0.1f);
        int hpBefore1 = target.Hp;
        battle.ResolveAction(new CombatAction { ActorId = attackerFront.BattleId, TargetId = target.BattleId, Type = CombatActionType.Attack });
        int damageFromFront = hpBefore1 - target.Hp;

        // Mid Attack
        attackerMid.Atb = 100;
        battle.Tick(0.1f);
        int hpBefore2 = target.Hp;
        battle.ResolveAction(new CombatAction { ActorId = attackerMid.BattleId, TargetId = target.BattleId, Type = CombatActionType.Attack });
        int damageFromMid = hpBefore2 - target.Hp;

        // Back Attack
        attackerBack.Atb = 100;
        battle.Tick(0.1f);
        int hpBefore3 = target.Hp;
        battle.ResolveAction(new CombatAction { ActorId = attackerBack.BattleId, TargetId = target.BattleId, Type = CombatActionType.Attack });
        int damageFromBack = hpBefore3 - target.Hp;

        // Assert
        Assert.True(damageFromFront > damageFromMid, $"Attacker in front ({damageFromFront}) should deal more damage than mid ({damageFromMid}).");
        Assert.True(damageFromMid > damageFromBack, $"Attacker in mid ({damageFromMid}) should deal more damage than back ({damageFromBack}).");
    }
}
