using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Skills;

namespace TWL.Client.Presentation.Skills;

public class SkillManager : ISkillCatalog
{
    private readonly Dictionary<int, Skill> _skills;

    public SkillManager()
    {
        _skills = new Dictionary<int, Skill>();
        LoadSkillsFromJson("./Content/Data/skills.json");
    }

    public void LoadSkillsFromJson(string path)
    {
        var json = File.ReadAllText(path);
        var definitions = JsonConvert.DeserializeObject<List<SkillDefinition>>(json);

        foreach (var def in definitions)
        {
            var skill = new Skill
            {
                SkillId = def.SkillId,
                Name = def.Name,
                Description = def.Description,
                // Convert string values from JSON to enums (case insensitive)
                Element = (Element)Enum.Parse(typeof(Element), def.Element, true),
                Type = (SkillType)Enum.Parse(typeof(SkillType), def.Type, true),
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
                UnsealChance = def.UnsealChance.HasValue ? (float)def.UnsealChance.Value : 0f
            };
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