using Xunit;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Requests;
using TWL.Tests.Mocks;

namespace TWL.Tests.Combat;

public class CombatManagerTests
{
    [Fact]
    public void UseSkill_CalculatesDamage_WithVariance_Normal()
    {
        // Arrange
        // MockRandomService defaults to 0.5f
        // Variance logic: 0.95f + (1.05f - 0.95f) * NextFloat()
        // If NextFloat is 0.5f, then variance is 0.95 + 0.10 * 0.5 = 1.00
        var mockRandom = new MockRandomService(0.5f);
        var resolver = new StandardCombatResolver(mockRandom);
        var manager = new CombatManager(resolver);

        var attacker = new ServerCharacter { Id = 1, Name = "Attacker", Str = 100 };
        var target = new ServerCharacter { Id = 2, Name = "Target", Hp = 1000 };
        manager.AddCharacter(attacker);
        manager.AddCharacter(target);

        // Act
        // Base Damage = 100 * 2 = 200
        // Variance = 1.0
        // Final Damage = 200
        var request = new UseSkillRequest { PlayerId = 1, TargetId = 2 };
        var result = manager.UseSkill(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200, result.Damage);
    }

    [Fact]
    public void UseSkill_CalculatesDamage_WithLowVariance()
    {
        // Arrange
        // Set Mock to 0.0 -> Variance should be 0.95
        var mockRandom = new MockRandomService(0.0f);
        var resolver = new StandardCombatResolver(mockRandom);
        var manager = new CombatManager(resolver);

        var attacker = new ServerCharacter { Id = 1, Name = "Attacker", Str = 100 };
        var target = new ServerCharacter { Id = 2, Name = "Target", Hp = 1000 };
        manager.AddCharacter(attacker);
        manager.AddCharacter(target);

        // Act
        // Base Damage = 200
        // Final Damage = 200 * 0.95 = 190
        var request = new UseSkillRequest { PlayerId = 1, TargetId = 2 };
        var result = manager.UseSkill(request);

        // Assert
        Assert.Equal(190, result.Damage);
    }

    [Fact]
    public void UseSkill_CalculatesDamage_WithHighVariance()
    {
        // Arrange
        // Set Mock to 1.0 -> Variance should be 1.05
        var mockRandom = new MockRandomService(1.0f);
        var resolver = new StandardCombatResolver(mockRandom);
        var manager = new CombatManager(resolver);

        var attacker = new ServerCharacter { Id = 1, Name = "Attacker", Str = 100 };
        var target = new ServerCharacter { Id = 2, Name = "Target", Hp = 1000 };
        manager.AddCharacter(attacker);
        manager.AddCharacter(target);

        // Act
        // Base Damage = 200
        // Final Damage = 200 * 1.05 = 210
        var request = new UseSkillRequest { PlayerId = 1, TargetId = 2 };
        var result = manager.UseSkill(request);

        // Assert
        Assert.Equal(210, result.Damage);
    }
}
