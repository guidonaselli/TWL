using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Skills;

namespace TWL.Client.Presentation.Skills;

public class SkillManager : ISkillCatalog
{
    private readonly Dictionary<int, Skill> _skills;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public SkillManager()
    {
        _skills = new Dictionary<int, Skill>();
        // Ensure directory exists or handle error gracefully in client
        if (File.Exists("./Content/Data/skills.json"))
        {
            LoadSkillsFromJson("./Content/Data/skills.json");
        }
        else
        {
            // Fallback or log? For now, we assume it exists or we do nothing
        }
    }

    public void LoadSkillsFromJson(string path)
    {
        var json = File.ReadAllText(path);
        // We can deserialize directly to Skill if the JSON matches, or use the Definition DTO.
        // For now, let's keep using the DTO and map it, but update the DTO to handle new fields if we add them to JSON.
        // Or better: update the mapping to fill new fields with defaults.

        var definitions = JsonSerializer.Deserialize<List<SkillDefinition>>(json, _jsonOptions);

        if (definitions == null) return;

        foreach (var def in definitions)
        {
            // Parse Enums safely
            Enum.TryParse(def.Element, true, out Element elem);
            Enum.TryParse(def.Type, true, out SkillType sType);

            var skill = new Skill
            {
                SkillId = def.SkillId,
                Name = def.Name,
                Description = def.Description,
                Element = elem,
                Type = sType,
                Level = def.Level,
                MaxLevel = def.MaxLevel,
                StrRequirement = def.Requirements != null ? def.Requirements.Str : 0,
                ConRequirement = def.Requirements != null ? def.Requirements.Con : 0,
                IntRequirement = def.Requirements != null ? def.Requirements.Int : 0,
                WisRequirement = def.Requirements != null ? def.Requirements.Wis : 0,
                AgiRequirement = def.Requirements != null ? def.Requirements.Agi : 0,
                SpCost = def.SpCost,
                Power = (float)def.Power,
                SealChance = def.SealChance.HasValue ? (float)def.SealChance.Value : 0f,
                UnsealChance = def.UnsealChance.HasValue ? (float)def.UnsealChance.Value : 0f,

                // Initialize new fields with defaults or map from legacy if possible
                Branch = SkillBranch.Physical, // Default, should be in JSON
                Tier = 1,
                TargetType = SkillTargetType.SingleEnemy,
                Scaling = new List<SkillScaling>(), // Empty for legacy loading
                Effects = new List<SkillEffect>()
            };

            // If we want to support the NEW JSON format here, we would need to update SkillDefinition DTO.
            // But since the task is Server-Authoritative, the Client just needs to know basic info for UI (Name, Cost, Description).
            // The Client DOES NOT calculate damage, so it might not need Scaling/Effects details yet.
            // However, it might need TargetType for selection UI.

            _skills[skill.SkillId] = skill;
        }
    }

    public Skill? GetSkillById(int skillId)
    {
        _skills.TryGetValue(skillId, out var skill);
        return skill;
    }

    IEnumerable<int> ISkillCatalog.GetAllSkillIds()
    {
        return _skills.Keys;
    }

    public List<Skill> GetAllSkills()
    {
        return _skills.Values.ToList();
    }

    public IEnumerable<int> GetAllSkillIds()
    {
        return _skills.Keys;
    }
}
