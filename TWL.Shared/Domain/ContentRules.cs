using TWL.Shared.Domain.Skills;

namespace TWL.Shared.Domain;

/// <summary>
/// Single Source of Truth for Content Rules (Skills, Quests, Budgets).
/// </summary>
public static class ContentRules
{
    // Skill Categories
    public static readonly IReadOnlySet<SkillCategory> ValidCategories = new HashSet<SkillCategory>
    {
        SkillCategory.None,
        SkillCategory.RebirthJob,
        SkillCategory.ElementSpecial,
        SkillCategory.Fairy,
        SkillCategory.Dragon,
        SkillCategory.Goddess
    };

    public static readonly IReadOnlySet<SkillCategory> SpecialCategories = new HashSet<SkillCategory>
    {
        SkillCategory.RebirthJob,
        SkillCategory.ElementSpecial,
        SkillCategory.Fairy,
        SkillCategory.Dragon,
        SkillCategory.Goddess
    };

    // Goddess Skills Definitions (Strict Naming/IDs)
    public static readonly IReadOnlyDictionary<int, string> GoddessSkills = new Dictionary<int, string>
    {
        { 2001, "Shrink" },
        { 2002, "Blockage" },
        { 2003, "Hotfire" },
        { 2004, "Vanish" }
    };

    // Tier Budgets
    public record TierBudget(int MinSp, int MaxSp, int MinCd, int MaxCd);

    public static readonly IReadOnlyDictionary<(SkillFamily Family, int Tier), TierBudget> TierBudgets = new Dictionary<(SkillFamily, int), TierBudget>
    {
        // Tier 1 (Core): SP 5-20, Cooldown 0-2
        { (SkillFamily.Core, 1), new TierBudget(5, 20, 0, 2) },

        // Tier 2 (Core): SP 15-40, Cooldown 1-3
        { (SkillFamily.Core, 2), new TierBudget(15, 40, 1, 3) },

        // Tier 3 (Core): SP 30-100, Cooldown 3-6
        { (SkillFamily.Core, 3), new TierBudget(30, 100, 3, 6) },

        // Tier 3 (Special): SP 30-100, Cooldown 3-6 (Assuming same budget for now based on tests)
        { (SkillFamily.Special, 3), new TierBudget(30, 100, 3, 6) },

        // Tier 1 (Special): e.g. Goddess Skills, varying budgets but usually higher cost/impact.
        // For now, we enforce generic ranges or specific overrides if needed.
        // Goddess skills have SP 10-20, CD 3-4. Let's define a loose budget for Special T1.
        { (SkillFamily.Special, 1), new TierBudget(10, 50, 2, 5) }
    };

    // Special Skill Rules
    public const int MinLevelForElementSpecial = 10;
    public const string RebirthJobCategoryName = "RebirthJob";
    public const string ElementSpecialCategoryName = "ElementSpecial";
}
