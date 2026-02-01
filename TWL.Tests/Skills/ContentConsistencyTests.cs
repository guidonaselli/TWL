using System.Reflection;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Skills;

namespace TWL.Tests.Skills;

public class ContentConsistencyTests
{
    private readonly ISkillCatalog _skillCatalog;

    public ContentConsistencyTests()
    {
        // Load actual skills.json
        var path = Path.Combine(AppContext.BaseDirectory, "Content/Data/skills.json");
        var json = File.ReadAllText(path);

        // Reset/Init Registry
        var ctor = typeof(SkillRegistry).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
            Type.EmptyTypes, null);
        var registry = (SkillRegistry)ctor.Invoke(null);
        registry.LoadSkills(json);
        _skillCatalog = registry;
    }

    [Fact]
    public void All_StageUpgrades_PointTo_ValidSkills()
    {
        foreach (var id in _skillCatalog.GetAllSkillIds())
        {
            var skill = _skillCatalog.GetSkillById(id);
            if (skill.StageUpgradeRules != null && skill.StageUpgradeRules.NextSkillId.HasValue)
            {
                var nextId = skill.StageUpgradeRules.NextSkillId.Value;
                var nextSkill = _skillCatalog.GetSkillById(nextId);

                Assert.NotNull(nextSkill);
                Assert.Equal(skill.Element, nextSkill.Element);
                Assert.Equal(skill.Branch, nextSkill.Branch);
                // Assert.Equal(skill.Stage + 1, nextSkill.Stage); // Check strictly Stage + 1
            }
        }
    }

    [Fact]
    public void Stage_Evolution_Chains_Are_Complete()
    {
        // Check standard 3-stage skills
        // Earth Physical: 1001 -> 1002 -> 1003
        VerifyChain(1001, 1002, 1003);
        // Earth Magical: 1101 -> 1102 -> 1103
        VerifyChain(1101, 1102, 1103);
        // Earth Support: 1201 -> 1202 -> 1203
        VerifyChain(1201, 1202, 1203);

        // Water Physical
        VerifyChain(3001, 3002, 3003);
        // Water Magical
        VerifyChain(3101, 3102, 3103);
        // Water Support
        VerifyChain(3201, 3202, 3203);

        // Fire Physical
        VerifyChain(4001, 4002, 4003);
        // Fire Magical
        VerifyChain(4101, 4102, 4103);
        // Fire Support
        VerifyChain(4201, 4202, 4203);

        // Wind Physical
        VerifyChain(5001, 5002, 5003);
        // Wind Magical
        VerifyChain(5101, 5102, 5103);
        // Wind Support
        VerifyChain(5201, 5202, 5203);
    }

    private void VerifyChain(int s1, int s2, int s3)
    {
        var skill1 = _skillCatalog.GetSkillById(s1);
        Assert.NotNull(skill1);
        Assert.Equal(1, skill1.Stage);
        Assert.NotNull(skill1.StageUpgradeRules);
        Assert.Equal(s2, skill1.StageUpgradeRules.NextSkillId);

        var skill2 = _skillCatalog.GetSkillById(s2);
        Assert.NotNull(skill2);
        Assert.Equal(2, skill2.Stage);
        Assert.NotNull(skill2.StageUpgradeRules);
        Assert.Equal(s3, skill2.StageUpgradeRules.NextSkillId);

        var skill3 = _skillCatalog.GetSkillById(s3);
        Assert.NotNull(skill3);
        Assert.Equal(3, skill3.Stage);
    }
}