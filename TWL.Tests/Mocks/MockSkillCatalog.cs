using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Skills;

namespace TWL.Tests.Mocks;

public class MockSkillCatalog : ISkillCatalog
{
    private readonly Dictionary<int, Skill> _skills = new();

    public Skill? GetSkillById(int id) => _skills.TryGetValue(id, out var skill) ? skill : null;

    public IEnumerable<int> GetAllSkillIds() => _skills.Keys;

    public void AddSkill(Skill skill) => _skills[skill.SkillId] = skill;
}