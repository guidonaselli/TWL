using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Domain.Skills;
using TWL.Tests.Mocks;

namespace TWL.Tests.Combat;

public class CombatManagerTests
{
    private const string TestSkillJson = @"
[
  {
    ""SkillId"": 999,
    ""Name"": ""Test Strike"",
    ""Element"": ""Earth"",
    ""Branch"": ""Physical"",
    ""Tier"": 1,
    ""TargetType"": ""SingleEnemy"",
    ""SpCost"": 0,
    ""Scaling"": [ { ""Stat"": ""Str"", ""Coefficient"": 2.0 } ],
    ""Effects"": [ { ""Tag"": ""Damage"" } ]
  }
]
";

    private ISkillCatalog CreateMockCatalog()
    {
        var catalog = new MockSkillCatalog();
        var skill = new Skill
        {
            SkillId = 999,
            Name = "Test Strike",
            Element = Element.Earth,
            Branch = SkillBranch.Physical,
            TargetType = SkillTargetType.SingleEnemy,
            SpCost = 0,
            Scaling = new List<SkillScaling>
            {
                new() { Stat = StatType.Str, Coefficient = 2.0f }
            },
            Effects = new List<SkillEffect>
            {
                new() { Tag = SkillEffectTag.Damage }
            }
        };
        catalog.AddSkill(skill);
        return catalog;
    }

    [Fact]
    public void UseSkill_CalculatesDamage_WithVariance_Normal()
    {
        // Arrange
        // MockRandomService defaults to 0.5f -> Variance 1.0
        var mockRandom = new MockRandomService(0.5f);
        var catalog = CreateMockCatalog();
        var resolver = new StandardCombatResolver(mockRandom, catalog);
        var manager = new CombatManager(resolver, mockRandom, catalog, new StatusEngine());

        var attacker = new ServerCharacter { Id = 1, Name = "Attacker", Str = 100 }; // Atk=200? No, Str=100.
        // Skill scaling: Str * 2 = 200.

        var target = new ServerCharacter { Id = 2, Name = "Target", Hp = 1000, Con = 0 };
        // Con 0 -> Def 0. To match previous test behavior where Defense was ignored.

        manager.AddCharacter(attacker);
        manager.AddCharacter(target);

        // Act
        // Base Damage = 200
        // Variance = 1.0
        // Final Damage = 200 - 0 = 200
        var request = new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = 999 };
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
        var catalog = CreateMockCatalog();
        var resolver = new StandardCombatResolver(mockRandom, catalog);
        var manager = new CombatManager(resolver, mockRandom, catalog, new StatusEngine());

        var attacker = new ServerCharacter { Id = 1, Name = "Attacker", Str = 100 };
        var target = new ServerCharacter { Id = 2, Name = "Target", Hp = 1000, Con = 0 };
        manager.AddCharacter(attacker);
        manager.AddCharacter(target);

        // Act
        // Base Damage = 200
        // Final Damage = 200 * 0.95 = 190
        var request = new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = 999 };
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
        var catalog = CreateMockCatalog();
        var resolver = new StandardCombatResolver(mockRandom, catalog);
        var manager = new CombatManager(resolver, mockRandom, catalog, new StatusEngine());

        var attacker = new ServerCharacter { Id = 1, Name = "Attacker", Str = 100 };
        var target = new ServerCharacter { Id = 2, Name = "Target", Hp = 1000, Con = 0 };
        manager.AddCharacter(attacker);
        manager.AddCharacter(target);

        // Act
        // Base Damage = 200
        // Final Damage = 200 * 1.05 = 210
        var request = new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = 999 };
        var result = manager.UseSkill(request);

        // Assert
        Assert.Equal(210, result.Damage);
    }
}