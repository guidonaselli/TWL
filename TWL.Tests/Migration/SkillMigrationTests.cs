using Xunit;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Constants;
using System.IO;
using System;

namespace TWL.Tests.Migration;

public class SkillMigrationTests
{
    [Fact]
    public void CanonicalIds_MapTo_NewNames()
    {
        // Load the real server skills.json
        // Adjust path to find repo root from bin/Debug/net8.0
        var path = Path.Combine(Environment.CurrentDirectory, "../../../../TWL.Server/Content/Data/skills.json");

        Assert.True(File.Exists(path), $"skills.json not found at {path}");

        var json = File.ReadAllText(path);
        SkillRegistry.Instance.LoadSkills(json);

        // Verify Water
        var waterSkill = SkillRegistry.Instance.GetSkillById(SkillIds.GS_WATER_DIMINUTION);
        Assert.NotNull(waterSkill);
        Assert.Equal("Diminution", waterSkill.Name);
        Assert.Equal("Skill_Diminution", waterSkill.DisplayNameKey);

        // Verify Earth
        var earthSkill = SkillRegistry.Instance.GetSkillById(SkillIds.GS_EARTH_SUPPORT_SEAL);
        Assert.NotNull(earthSkill);
        Assert.Equal("Support Seal", earthSkill.Name);
        Assert.Equal("Skill_SupportSeal", earthSkill.DisplayNameKey);

        // Verify Fire
        var fireSkill = SkillRegistry.Instance.GetSkillById(SkillIds.GS_FIRE_EMBER_SURGE);
        Assert.NotNull(fireSkill);
        Assert.Equal("Ember Surge", fireSkill.Name);
        Assert.Equal("Skill_EmberSurge", fireSkill.DisplayNameKey);

        // Verify Wind
        var windSkill = SkillRegistry.Instance.GetSkillById(SkillIds.GS_WIND_UNTOUCHABLE_VEIL);
        Assert.NotNull(windSkill);
        Assert.Equal("Untouchable Veil", windSkill.Name);
        Assert.Equal("Skill_UntouchableVeil", windSkill.DisplayNameKey);
    }
}
