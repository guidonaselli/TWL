using System.Text.Json;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Domain.Skills;
using TWL.Tests.Mocks;

namespace TWL.Tests.Domain.Skills;

public class SpecialSkillTests
{
    [Fact]
    public void Deserialize_GoddessSkills_VerifyProperties()
    {
        var json = @"
        [
          {
            ""SkillId"": 2001,
            ""Name"": ""Diminution"",
            ""Family"": ""Special"",
            ""Category"": ""Goddess"",
            ""Restrictions"": { ""UniquePerCharacter"": true, ""BindOnAcquire"": true }
          }
        ]";

        var skills = JsonSerializer.Deserialize<List<Skill>>(json);
        Assert.NotNull(skills);
        var skill = skills[0];

        Assert.Equal(SkillFamily.Special, skill.Family);
        Assert.Equal(SkillCategory.Goddess, skill.Category);
        Assert.NotNull(skill.Restrictions);
        Assert.True(skill.Restrictions.UniquePerCharacter);
        Assert.True(skill.Restrictions.BindOnAcquire);
    }

    [Fact]
    public void CoreSkill_HasDefaultFamilyAndCategory()
    {
        var json = @"
        [
          {
            ""SkillId"": 1001,
            ""Name"": ""Rock Smash""
          }
        ]";

        var skills = JsonSerializer.Deserialize<List<Skill>>(json);
        Assert.NotNull(skills);
        var skill = skills[0];

        Assert.Equal(SkillFamily.Core, skill.Family);
        Assert.Equal(SkillCategory.None, skill.Category);
    }

    [Fact]
    public void Deserialize_HitRules_VerifyProperties()
    {
        var json = @"
        [
          {
            ""SkillId"": 1202,
            ""Name"": ""Entangle"",
            ""HitRules"": { ""BaseChance"": 0.7, ""StatDependence"": ""Int-Wis"" }
          }
        ]";

        var skills = JsonSerializer.Deserialize<List<Skill>>(json);
        Assert.NotNull(skills);
        var skill = skills[0];

        Assert.NotNull(skill.HitRules);
        Assert.Equal(0.7f, skill.HitRules.BaseChance);
        Assert.Equal("Int-Wis", skill.HitRules.StatDependence);
    }

    [Fact]
    public void Deserialize_UnlockRules_WithQuestFlag()
    {
        var json = @"
        [
          {
            ""SkillId"": 9001,
            ""Name"": ""Secret Skill"",
            ""UnlockRules"": { ""QuestFlag"": ""Quest_123_Complete"" }
          }
        ]";

        var skills = JsonSerializer.Deserialize<List<Skill>>(json);
        Assert.NotNull(skills);
        var skill = skills[0];

        Assert.NotNull(skill.UnlockRules);
        Assert.Equal("Quest_123_Complete", skill.UnlockRules.QuestFlag);
    }

    [Fact]
    public void CombatManager_ApplySeal_UsesHitRules()
    {
        // 1. Setup Skill
        var json = @"
        [
          {
            ""SkillId"": 3001,
            ""Name"": ""Seal Skill"",
            ""SpCost"": 0,
            ""TargetType"": ""SingleEnemy"",
            ""Element"": ""Earth"",
            ""Branch"": ""Support"",
            ""Effects"": [ { ""Tag"": ""Seal"", ""Duration"": 2 } ],
            ""HitRules"": { ""BaseChance"": 0.5, ""MinChance"": 0.1, ""MaxChance"": 0.9 }
          }
        ]";
        SkillRegistry.Instance.LoadSkills(json);

        // 2. Setup Combat
        var mockRandom = new MockRandomService(0.0f); // 0.0 -> variance 0.95, random check 0.0 (Success)
        var resolver = new StandardCombatResolver(mockRandom, SkillRegistry.Instance);
        var manager = new CombatManager(resolver, mockRandom, SkillRegistry.Instance, new StatusEngine());

        var attacker = new ServerCharacter { Id = 1, Int = 100, Wis = 50 }; // Int - Wis = 50. StatDiff = 0.5.
        // Base 0.5 + 0.5 = 1.0. Clamped to MaxChance 0.9.

        var target = new ServerCharacter { Id = 2, Int = 10, Wis = 10 };

        manager.AddCharacter(attacker);
        manager.AddCharacter(target);

        // 3. Use Skill
        // We need to make sure MockRandom returns < 0.9.
        // MockRandomService implementation usually returns fixed value.
        // If FixedFloat is 0.0, NextFloat() returns 0.0. 0.0 <= 0.9 -> Success.

        var request = new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = 3001 };
        var result = manager.UseSkill(request);

        // 4. Verify
        Assert.NotNull(result);
        Assert.Contains(result.AddedEffects, e => e.Tag == SkillEffectTag.Seal);
    }
}