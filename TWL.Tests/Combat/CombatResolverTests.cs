using Xunit;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Requests;
using TWL.Tests.Mocks;

namespace TWL.Tests.Combat;

public class CombatResolverTests
{
    [Fact]
    public void CalculateDamage_StandardFormula_Correct()
    {
        // Arrange
        // FixedFloat 0.5 returns min + (diff/2) -> 1.0 variance for (0.95, 1.05)
        var mockRandom = new MockRandomService { FixedFloat = 0.5f };

        var resolver = new StandardCombatResolver(mockRandom);

        var attacker = new ServerCharacter { Str = 10 };
        var target = new ServerCharacter();
        var request = new UseSkillRequest();

        // Act
        // Base damage = Str * 2 = 20
        // Variance = 1.0
        // Final = 20
        var damage = resolver.CalculateDamage(attacker, target, request);

        // Assert
        Assert.Equal(20, damage);
    }

    [Fact]
    public void CalculateDamage_VarianceHigh_Correct()
    {
        // Arrange
        // FixedFloat 1.0 returns max -> 1.05 variance
        var mockRandom = new MockRandomService { FixedFloat = 1.0f };

        var resolver = new StandardCombatResolver(mockRandom);

        var attacker = new ServerCharacter { Str = 10 };
        var target = new ServerCharacter();
        var request = new UseSkillRequest();

        // Act
        // Base = 20
        // Variance = 1.05 -> 21
        var damage = resolver.CalculateDamage(attacker, target, request);

        // Assert
        Assert.Equal(21, damage);
    }

    [Fact]
    public void CalculateDamage_VarianceLow_Correct()
    {
        // Arrange
        // FixedFloat 0.0 returns min -> 0.95 variance
        var mockRandom = new MockRandomService { FixedFloat = 0.0f };

        var resolver = new StandardCombatResolver(mockRandom);

        var attacker = new ServerCharacter { Str = 10 };
        var target = new ServerCharacter();
        var request = new UseSkillRequest();

        // Act
        // Base = 20
        // Variance = 0.95 -> 19
        var damage = resolver.CalculateDamage(attacker, target, request);

        // Assert
        Assert.Equal(19, damage);
    }
}
