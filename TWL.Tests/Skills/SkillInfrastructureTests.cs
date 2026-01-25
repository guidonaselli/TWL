using System;
using System.Reflection;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Skills;
using Xunit;

namespace TWL.Tests.Skills;

public class SkillInfrastructureTests
{
    [Fact]
    public void LoadSkills_ParsesSpecialProperties()
    {
        var json = @"[
          {
            ""SkillId"": 9001,
            ""Name"": ""Test Special"",
            ""DisplayNameKey"": ""Skill_TestSpecial"",
            ""Description"": ""A test skill."",
            ""Family"": ""Special"",
            ""Category"": ""Fairy"",
            ""Element"": ""Earth"",
            ""Branch"": ""Support"",
            ""Tier"": 1,
            ""TargetType"": ""SingleAlly"",
            ""SpCost"": 30,
            ""Cooldown"": 3,
            ""HitRules"": { ""BaseChance"": 0.8, ""StatDependence"": ""Int-Wis"" },
            ""Restrictions"": { ""UniquePerCharacter"": true, ""BindOnAcquire"": true },
            ""UnlockRules"": { ""QuestFlag"": ""Test_Flag"" }
          }
        ]";

        SkillRegistry.Instance.LoadSkills(json);
        var skill = SkillRegistry.Instance.GetSkillById(9001);

        Assert.NotNull(skill);
        Assert.Equal("Test Special", skill.Name);
        Assert.Equal("Skill_TestSpecial", skill.DisplayNameKey);
        Assert.Equal(SkillFamily.Special, skill.Family);
        Assert.Equal(SkillCategory.Fairy, skill.Category);

        Assert.NotNull(skill.HitRules);
        Assert.Equal(0.8f, skill.HitRules.BaseChance);
        Assert.Equal("Int-Wis", skill.HitRules.StatDependence);

        Assert.NotNull(skill.Restrictions);
        Assert.True(skill.Restrictions.UniquePerCharacter);
        Assert.True(skill.Restrictions.BindOnAcquire);
        Assert.False(skill.Restrictions.NotTradeable); // Default false if not set

        Assert.NotNull(skill.UnlockRules);
        Assert.Equal("Test_Flag", skill.UnlockRules.QuestFlag);
    }

    [Fact]
    public void GoddessSkills_Granted_OnLogin_Water()
    {
        var charData = new ServerCharacter { CharacterElement = Element.Water };
        var session = new TestClientSession(charData);

        // Before grant
        Assert.DoesNotContain(2001, charData.KnownSkills);
        Assert.DoesNotContain("GS_GRANTED", session.QuestComponent.Flags);

        session.TriggerGrantGoddessSkills();

        // After grant
        Assert.Contains(2001, charData.KnownSkills); // Shrink
        Assert.Contains("GS_GRANTED", session.QuestComponent.Flags);
    }

    [Fact]
    public void GoddessSkills_Granted_OnLogin_Fire()
    {
        var charData = new ServerCharacter { CharacterElement = Element.Fire };
        var session = new TestClientSession(charData);

        session.TriggerGrantGoddessSkills();

        Assert.Contains(2003, charData.KnownSkills); // Hotfire
        Assert.Contains("GS_GRANTED", session.QuestComponent.Flags);
    }

    [Fact]
    public void RealSkillsJson_IsValid()
    {
        var path = "/app/TWL.Server/Content/Data/skills.json";
        if (!File.Exists(path))
        {
             // Fallback if not absolute
             path = Path.Combine(Environment.CurrentDirectory, "../../../../TWL.Server/Content/Data/skills.json");
        }

        Assert.True(File.Exists(path), $"File not found at {path}");
        var json = File.ReadAllText(path);

        // Should not throw
        SkillRegistry.Instance.LoadSkills(json);

        // Verify Fairy Light exists
        var fairy = SkillRegistry.Instance.GetSkillById(6001);
        Assert.NotNull(fairy);
        Assert.Equal("Fairy Light", fairy.Name);
        Assert.Equal(SkillCategory.Fairy, fairy.Category);
        Assert.NotNull(fairy.UnlockRules);
        Assert.Equal("Unlock_FairyLight", fairy.UnlockRules.QuestFlag);
    }
}

public class TestClientSession : ClientSession
{
    public TestClientSession(ServerCharacter character)
    {
        Character = character;
        // Inject a component with null manager (safe for Flags access)
        QuestComponent = new PlayerQuestComponent(null!);
        QuestComponent.Character = character;
    }

    public void TriggerGrantGoddessSkills()
    {
        var method = typeof(ClientSession).GetMethod("GrantGoddessSkills", BindingFlags.NonPublic | BindingFlags.Instance);
        if (method == null) throw new Exception("Method GrantGoddessSkills not found");
        method.Invoke(this, null);
    }
}
