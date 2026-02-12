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
    public record TierBudget(
        int MinSp, int MaxSp,
        int MinCd, int MaxCd,
        float MaxDamageCoeff,   // Maximum sum of coefficients for Damage skills
        float MaxHealCoeff,     // Maximum sum of coefficients for Heal skills
        int MaxHardControlDuration, // Max turns for hard control (Seal/Stun/Sleep)
        float MaxHardControlChance  // Max base chance for hard control
    );

    public static readonly IReadOnlyDictionary<(SkillFamily Family, int Tier), TierBudget> TierBudgets = new Dictionary<(SkillFamily, int), TierBudget>
    {
        // Tier 1 (Core): SP 5-20, CD 0-2
        // Damage: Max 1.9 (Single/AoE). Heal: Max 4.0. Control: Max 2 turns, 50% chance.
        { (SkillFamily.Core, 1), new TierBudget(5, 20, 0, 2, 1.9f, 4.0f, 2, 0.5f) },

        // Tier 2 (Core): SP 15-40, CD 1-3
        // Damage: Max 2.5. Heal: Max 5.0. Control: Max 3 turns, 80% chance.
        { (SkillFamily.Core, 2), new TierBudget(15, 40, 1, 3, 2.5f, 5.0f, 3, 0.8f) },

        // Tier 3 (Core): SP 30-100, CD 3-6
        // Damage: Max 4.0. Heal: Max 6.0. Control: Max 4 turns, 90% chance.
        { (SkillFamily.Core, 3), new TierBudget(30, 100, 3, 6, 4.0f, 6.0f, 4, 0.9f) },

        // Tier 1 (Special): e.g. Goddess Skills
        // SP 10-50, CD 2-5. Damage: 2.5. Heal: 5.0. Control: 3 turns, 90% chance (Goddess skills are strong).
        { (SkillFamily.Special, 1), new TierBudget(10, 50, 2, 5, 2.5f, 5.0f, 3, 0.9f) },

        // Tier 3 (Special): SP 30-100, CD 3-6.
        // Same as Core T3 but slightly stronger potential (Legendary/Quest).
        { (SkillFamily.Special, 3), new TierBudget(30, 100, 3, 6, 4.5f, 7.0f, 4, 1.0f) }
    };

    // Special Skill Rules
    public const int MinLevelForElementSpecial = 10;
    public const string RebirthJobCategoryName = "RebirthJob";
    public const string ElementSpecialCategoryName = "ElementSpecial";
}
