using Microsoft.Extensions.Logging.Abstractions;
using TWL.Server.Features.Combat;
using TWL.Server.Services;
using TWL.Server.Simulation.Networking;
using Xunit;

namespace TWL.Tests.Server.Features.Combat;

public class TurnEngineTests
{
    private class TestCombatant : ServerCombatant
    {
        public override void ReplaceSkill(int oldId, int newId) { }
    }

    [Fact]
    public void StartEncounter_SortsBySpdDescending()
    {
        // Arrange
        var random = new SeedableRandomService(NullLogger<SeedableRandomService>.Instance, 12345);
        var engine = new TurnEngine(random);

        var c1 = new TestCombatant { Id = 1, Name = "Slow", Agi = 10, Hp = 10 }; // Spd = 10
        var c2 = new TestCombatant { Id = 2, Name = "Fast", Agi = 20, Hp = 10 }; // Spd = 20
        var c3 = new TestCombatant { Id = 3, Name = "Medium", Agi = 15, Hp = 10 }; // Spd = 15

        // Act
        engine.StartEncounter(new[] { c1, c2, c3 });

        // Assert
        // Round 1
        Assert.Equal(c2, engine.NextTurn()); // 20
        Assert.Equal(c3, engine.NextTurn()); // 15
        Assert.Equal(c1, engine.NextTurn()); // 10

        // Round 2 (Wrap around)
        Assert.Equal(c2, engine.NextTurn());
    }

    [Fact]
    public void TieBreaking_IsDeterministic()
    {
        // Arrange
        // Two combatants with same speed
        var c1 = new TestCombatant { Id = 1, Name = "A", Agi = 10, Hp = 10 };
        var c2 = new TestCombatant { Id = 2, Name = "B", Agi = 10, Hp = 10 };

        var random1 = new SeedableRandomService(NullLogger<SeedableRandomService>.Instance, 12345);
        var engine1 = new TurnEngine(random1);
        engine1.StartEncounter(new[] { c1, c2 });
        var first1 = engine1.NextTurn();

        var random2 = new SeedableRandomService(NullLogger<SeedableRandomService>.Instance, 12345);
        var engine2 = new TurnEngine(random2);
        engine2.StartEncounter(new[] { c1, c2 });
        var first2 = engine2.NextTurn();

        Assert.NotNull(first1);
        Assert.NotNull(first2);
        Assert.Equal(first1.Id, first2.Id);
    }

    [Fact]
    public void NextTurn_SkipsDead()
    {
        var random = new SeedableRandomService(NullLogger<SeedableRandomService>.Instance, 12345);
        var engine = new TurnEngine(random);

        var c1 = new TestCombatant { Id = 1, Agi = 20, Hp = 10 };
        var c2 = new TestCombatant { Id = 2, Agi = 10, Hp = 10 };

        engine.StartEncounter(new[] { c1, c2 });

        Assert.Equal(c1, engine.NextTurn()); // Round 1 Turn 1

        c2.Hp = 0; // Kill c2

        // Next should be Round 2 Turn 1 (c1), skipping c2 in Round 1 if it was there?
        // Wait, StartEncounter puts everyone in queue.
        // Round 1 queue: c1, c2.
        // We popped c1. Queue has c2.
        // NextTurn will dequeue c2. c2 is dead. It should skip it.
        // Then queue empty -> StartNewRound. c2 is dead, so only c1 added.
        // Dequeue c1.

        Assert.Equal(c1, engine.NextTurn());

        // Verify c2 never comes
        Assert.Equal(c1, engine.NextTurn());
    }
}
