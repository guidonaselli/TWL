using System.Text.Json;
using System.Text.Json.Serialization;
using TWL.Shared.Domain.Characters;

namespace TWL.Shared.Domain.Skills;

public class SkillRegistry : ISkillCatalog
{
    public static SkillRegistry Instance { get; } = new();

    private readonly Dictionary<int, Skill> _skills = new();

    private SkillRegistry() { }

    public void LoadSkills(string jsonContent)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        var definitions = JsonSerializer.Deserialize<List<SkillDataDto>>(jsonContent, options);

        if (definitions == null) return;

        foreach (var def in definitions)
        {
            var skill = new Skill
            {
                SkillId = def.SkillId,
                Id = def.SkillId,
                Name = def.Name,
                DisplayNameKey = def.DisplayNameKey,
                Description = def.Description,
                Family = def.Family,
                Category = def.Category,
                Element = def.Element,
                Branch = def.Branch,
                Tier = def.Tier,
                TargetType = def.TargetType,
                SpCost = def.SpCost,
                Cooldown = def.Cooldown,
                Scaling = def.Scaling ?? new List<SkillScaling>(),
                Effects = def.Effects ?? new List<SkillEffect>(),
                HitRules = def.HitRules,
                Restrictions = def.Restrictions,

                // Requirements
                StrRequirement = def.Requirements?.Str ?? 0,
                ConRequirement = def.Requirements?.Con ?? 0,
                IntRequirement = def.Requirements?.Int ?? 0,
                WisRequirement = def.Requirements?.Wis ?? 0,
                AgiRequirement = def.Requirements?.Agi ?? 0,

                Stage = def.Stage > 0 ? def.Stage : 1,
                UnlockRules = new SkillUnlockRules
                {
                    Level = def.UnlockRules?.Level ?? 0,
                    ParentSkillId = def.UnlockRules?.ParentSkillId,
                    ParentSkillRank = def.UnlockRules?.ParentSkillRank,
                    QuestId = def.UnlockRules?.QuestId,
                    QuestFlag = def.UnlockRules?.QuestFlag
                },
                StageUpgradeRules = def.StageUpgradeRules,

                // Legacy Mapping (for compatibility if needed internally)
                Type = MapBranchToType(def.Branch, def.Effects),
                Power = 0, // Calculated dynamically now
                Level = 1,
                MaxLevel = 1,
            };

            _skills[skill.SkillId] = skill;
        }
    }

    private SkillType MapBranchToType(SkillBranch branch, List<SkillEffect>? effects)
    {
        if (branch == SkillBranch.Physical) return SkillType.PhysicalDamage;
        if (branch == SkillBranch.Magical) return SkillType.MagicalDamage;
        // Simple heuristic for Support
        if (effects != null && effects.Exists(e => e.Tag == SkillEffectTag.Heal)) return SkillType.Buff; // Heal treated as Buff type?
        return SkillType.Buff;
    }

    public Skill? GetSkillById(int id)
    {
        _skills.TryGetValue(id, out var skill);
        return skill;
    }

    public IEnumerable<int> GetAllSkillIds()
    {
        return _skills.Keys;
    }

    // DTOs for JSON Loading
    private class SkillDataDto
    {
        public int SkillId { get; set; }
        public string Name { get; set; }
        public string DisplayNameKey { get; set; }
        public string Description { get; set; }
        public SkillFamily Family { get; set; }
        public SkillCategory Category { get; set; }
        public Element Element { get; set; }
        public SkillBranch Branch { get; set; }
        public int Tier { get; set; }
        public SkillTargetType TargetType { get; set; }
        public int SpCost { get; set; }
        public int Cooldown { get; set; }
        public List<SkillScaling>? Scaling { get; set; }
        public List<SkillEffect>? Effects { get; set; }
        public SkillHitRules? HitRules { get; set; }
        public SkillRestrictions? Restrictions { get; set; }
        public RequirementDto? Requirements { get; set; }
        public int Stage { get; set; }
        public UnlockRulesDto? UnlockRules { get; set; }
        public StageUpgradeRules? StageUpgradeRules { get; set; }
    }

    private class UnlockRulesDto
    {
        public int Level { get; set; }
        public int? ParentSkillId { get; set; }
        public int? ParentSkillRank { get; set; }
        public int? QuestId { get; set; }
        public string? QuestFlag { get; set; }
    }

    private class RequirementDto
    {
        public int Str { get; set; }
        public int Con { get; set; }
        public int Int { get; set; }
        public int Wis { get; set; }
        public int Agi { get; set; }
    }
}
