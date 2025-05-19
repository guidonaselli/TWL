using TWL.Shared.Domain.Skills;

namespace TWL.Shared.Domain.Characters;

public interface ISkillCatalog
{
    IEnumerable<int> GetAllSkillIds();
    Skill? GetSkillById(int id);
}