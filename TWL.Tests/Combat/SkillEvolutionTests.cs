using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Domain.Skills;
using TWL.Tests.Mocks;

namespace TWL.Tests.Combat;

public class SkillEvolutionTests
{
    private const string TestSkillsJson = @"
[
  {
    ""SkillId"": 2001,
    ""Name"": ""Base Skill"",
    ""Element"": ""Earth"",
    ""Branch"": ""Physical"",
    ""Tier"": 1,
    ""TargetType"": ""SingleEnemy"",
    ""SpCost"": 5,
    ""Effects"": [ { ""Tag"": ""Damage"" } ],
    ""Scaling"": [ { ""Stat"": ""Str"", ""Coefficient"": 1.0 } ],
    ""Stage"": 1,
    ""StageUpgradeRules"": { ""RankThreshold"": 2, ""NextSkillId"": 2002 }
  },
  {
    ""SkillId"": 2002,
    ""Name"": ""Evolved Skill"",
    ""Element"": ""Earth"",
    ""Branch"": ""Physical"",
    ""Tier"": 2,
    ""TargetType"": ""SingleEnemy"",
    ""SpCost"": 10,
    ""Effects"": [ { ""Tag"": ""Damage"" } ],
    ""Scaling"": [ { ""Stat"": ""Str"", ""Coefficient"": 2.0 } ],
    ""Stage"": 2
  }
]";

    public SkillEvolutionTests()
    {
        SkillRegistry.Instance.LoadSkills(TestSkillsJson);
    }

    [Fact]
    public void UseSkill_IncrementsMastery()
    {
        var mockRng = new MockRandomService(1.0f);
        var resolver = new StandardCombatResolver(mockRng, SkillRegistry.Instance);
        var manager = new CombatManager(resolver, mockRng, SkillRegistry.Instance, new StatusEngine());

        var attacker = new ServerCharacter { Id = 1, Name = "Attacker", Sp = 100, Str = 10 };
        var target = new ServerCharacter { Id = 2, Name = "Target", Hp = 100, Con = 5 };

        manager.AddCharacter(attacker);
        manager.AddCharacter(target);

        var request = new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = 2001 };

        manager.UseSkill(request);

        Assert.True(attacker.SkillMastery.ContainsKey(2001));
        Assert.Equal(1, attacker.SkillMastery[2001].UsageCount);
    }

    [Fact]
    public void UseSkill_EvolvesSkill_WhenThresholdReached()
    {
        var mockRng = new MockRandomService(1.0f);
        var resolver = new StandardCombatResolver(mockRng, SkillRegistry.Instance);
        var manager = new CombatManager(resolver, mockRng, SkillRegistry.Instance, new StatusEngine());

        var attacker = new ServerCharacter { Id = 1, Name = "Attacker", Sp = 100, Str = 10 };

        attacker.SkillMastery[2001] = new SkillMastery { Rank = 2, UsageCount = 10 };

        var target = new ServerCharacter { Id = 2, Name = "Target", Hp = 100, Con = 5 };
        manager.AddCharacter(attacker);
        manager.AddCharacter(target);

        var request = new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = 2001 };

        manager.UseSkill(request);

        Assert.Contains(2002, attacker.KnownSkills);
        Assert.DoesNotContain(2001, attacker.KnownSkills);
    }

    [Fact]
    public void UseSkill_ConsumesSp()
    {
        var mockRng = new MockRandomService(1.0f);
        var resolver = new StandardCombatResolver(mockRng, SkillRegistry.Instance);
        var manager = new CombatManager(resolver, mockRng, SkillRegistry.Instance, new StatusEngine());

        var attacker = new ServerCharacter { Id = 1, Name = "Attacker", Sp = 10, Str = 10 }; // SP 10
        var target = new ServerCharacter { Id = 2, Name = "Target", Hp = 100, Con = 5 };

        manager.AddCharacter(attacker);
        manager.AddCharacter(target);

        // Skill 2001 cost 5.
        var request = new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = 2001 };

        manager.UseSkill(request);

        // Should have 5 SP left
        Assert.Equal(5, attacker.Sp);

        // Use again
        manager.UseSkill(request);
        Assert.Equal(0, attacker.Sp);

        // Use again (fail)
        var result = manager.UseSkill(request);
        Assert.Empty(result);
        Assert.Equal(0, attacker.Sp);
    }
}