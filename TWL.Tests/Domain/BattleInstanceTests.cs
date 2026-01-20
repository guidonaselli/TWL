using System.Collections.Generic;
using System.Linq;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using Xunit;

namespace TWL.Tests.Domain
{
    public class BattleInstanceTests
    {
        private class TestCharacter : Character
        {
            public TestCharacter(string name, int spd) : base(name, Element.Fire)
            {
                Spd = spd;
                Str = 10;
                Con = 5;
                Health = 100;
                MaxHealth = 100;
                Team = Team.Player;
            }
        }

        private class TestEnemy : Character
        {
            public TestEnemy(string name, int spd) : base(name, Element.Water)
            {
                Spd = spd;
                Str = 5;
                Con = 2;
                Health = 50;
                MaxHealth = 50;
                Team = Team.Enemy;
            }
        }

        [Fact]
        public void Initialization_CreatesCombatants()
        {
            var p = new TestCharacter("Hero", 10);
            var e = new TestEnemy("Slime", 5);

            var battle = new BattleInstance(new[] { p }, new[] { e });

            Assert.Single(battle.Allies);
            Assert.Single(battle.Enemies);
            Assert.Equal(2, battle.AllCombatants.Count);
        }

        [Fact]
        public void Tick_IncreasesATB()
        {
            var p = new TestCharacter("Hero", 10);
            var e = new TestEnemy("Slime", 5);
            var battle = new BattleInstance(new[] { p }, new[] { e });

            battle.Tick(1.0f);

            // Speed 10 formula: (10 + 10) * 5 = 100 per sec
            // Speed 5 formula: (10 + 5) * 5 = 75 per sec

            Assert.True(battle.Allies[0].Atb > 0);
            Assert.True(battle.Enemies[0].Atb > 0);
        }

        [Fact]
        public void ResolveAction_DamageApplied()
        {
            var p = new TestCharacter("Hero", 10);
            var e = new TestEnemy("Slime", 5);
            var battle = new BattleInstance(new[] { p }, new[] { e });

            // Force turn
            battle.Tick(10.0f);
            var actor = battle.CurrentTurnCombatant;
            var target = battle.Enemies[0];

            Assert.NotNull(actor);
            if (actor.BattleId == battle.Allies[0].BattleId)
            {
                var action = CombatAction.Attack(actor.BattleId, target.BattleId);
                battle.ResolveAction(action);

                // Dmg = Str(10)*2 - Con(2)*2 = 20 - 4 = 16
                Assert.Equal(50 - 16, target.Character.Health);
            }
        }

        [Fact]
        public void BattleEnd_Victory()
        {
             var p = new TestCharacter("Hero", 10);
            var e = new TestEnemy("Slime", 5);
            e.Health = 1;

            var battle = new BattleInstance(new[] { p }, new[] { e });

            // Force turn to player
            // Player SPD 10, Enemy SPD 5. Player acts first.
            battle.Tick(10.0f);

            var actor = battle.CurrentTurnCombatant;
            Assert.Equal(p.Name, actor.Character.Name);

            var action = CombatAction.Attack(actor.BattleId, battle.Enemies[0].BattleId);
            battle.ResolveAction(action);

            Assert.Equal(0, e.Health);
            Assert.Equal(BattleState.Victory, battle.State);
        }
    }
}
