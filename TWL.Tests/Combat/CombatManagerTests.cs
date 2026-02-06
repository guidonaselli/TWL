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

        var attacker = new ServerCharacter { Id = 1, Name = "Attacker", Str = 100, Agi = 50, Hp = 1000 }; // Atk=200? No, Str=100.
        // Skill scaling: Str * 2 = 200.

        var target = new ServerCharacter { Id = 2, Name = "Target", Hp = 1000, Con = 0, Team = Team.Enemy, Agi = 10 };
        // Con 0 -> Def 0. To match previous test behavior where Defense was ignored.

        // Start Encounter (Attacker Spd=0, Target Spd=0 -> Attacker first due to list order + stable sort)
        manager.StartEncounter(1, new List<ServerCharacter> { attacker, target });

        // Act
        // Base Damage = 200
        // Variance = 1.0
        // Final Damage = 200 - 0 = 200
        var request = new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = 999 };
        var result = manager.UseSkill(request);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(200, result[0].Damage);
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

        var attacker = new ServerCharacter { Id = 1, Name = "Attacker", Str = 100, Agi = 50, Hp = 1000 };
        var target = new ServerCharacter { Id = 2, Name = "Target", Hp = 1000, Con = 0, Team = Team.Enemy, Agi = 10 };
        manager.StartEncounter(1, new List<ServerCharacter> { attacker, target });

        // Act
        // Base Damage = 200
        // Final Damage = 200 * 0.95 = 190
        var request = new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = 999 };
        var result = manager.UseSkill(request);

        // Assert
        Assert.Single(result);
        Assert.Equal(190, result[0].Damage);
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

        var attacker = new ServerCharacter { Id = 1, Name = "Attacker", Str = 100, Agi = 50, Hp = 1000 };
        var target = new ServerCharacter { Id = 2, Name = "Target", Hp = 1000, Con = 0, Team = Team.Enemy, Agi = 10 };
        manager.StartEncounter(1, new List<ServerCharacter> { attacker, target });

        // Act
        // Base Damage = 200
        // Final Damage = 200 * 1.05 = 210
        var request = new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = 999 };
        var result = manager.UseSkill(request);

        // Assert
        Assert.Single(result);
        Assert.Equal(210, result[0].Damage);
    }

    [Fact]
    public void UseSkill_EnforcesTurnOrder()
    {
        var mockRandom = new MockRandomService(0.5f);
        var catalog = CreateMockCatalog();
        var resolver = new StandardCombatResolver(mockRandom, catalog);
        var manager = new CombatManager(resolver, mockRandom, catalog, new StatusEngine());

        var attacker = new ServerCharacter { Id = 1, Agi = 10, Hp = 100 };
        var target = new ServerCharacter { Id = 2, Agi = 20, Hp = 100, Team = Team.Enemy }; // Higher speed

        manager.StartEncounter(1, new List<ServerCharacter> { attacker, target });

        // Target (Id 2) should be first.
        // Try attacker (Id 1) using skill.
        var request = new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = 999 };
        var result = manager.UseSkill(request);

        Assert.Empty(result);

        // Target uses skill
        var request2 = new UseSkillRequest { PlayerId = 2, TargetId = 1, SkillId = 999 };
        var result2 = manager.UseSkill(request2);

        Assert.Single(result2);

        // Now it should be Attacker's turn
        var result3 = manager.UseSkill(request);
        Assert.Single(result3);
    }
}