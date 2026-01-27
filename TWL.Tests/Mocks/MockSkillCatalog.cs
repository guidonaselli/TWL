using System.Collections.Generic;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Domain.Characters;

namespace TWL.Tests.Mocks;

public class MockSkillCatalog : ISkillCatalog
{
    private readonly Dictionary<int, Skill> _skills = new();

    public void AddSkill(Skill skill)
    {
        _skills[skill.SkillId] = skill;
    }

    public Skill? GetSkillById(int id)
    {
        return _skills.TryGetValue(id, out var skill) ? skill : null;
    }

    public IEnumerable<int> GetAllSkillIds()
    {
        return _skills.Keys;
    }
}
