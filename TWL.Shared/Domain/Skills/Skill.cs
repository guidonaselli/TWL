using TWL.Shared.Domain.Characters;

namespace TWL.Shared.Domain.Skills;

public class Skill
{
    public SkillBranch Branch;
    public int Cooldown;
    public string Description = string.Empty;
    public string DescriptionKey = string.Empty;
    public string DisplayNameKey = string.Empty; // For UI localization

    // Core Attributes
    public Element Element;

    // Legacy / Compatibility fields (Can be mapped from new structure or kept for backward compat)
    public int Id; // Redundant, same as SkillId usually
    public int Level;
    public int MaxLevel;
    public string Name = string.Empty;
    public float Power;

    public float SealChance;

    // Identifiers
    public int SkillId;

    // Usage
    public int SpCost;
    public SkillTargetType TargetType;
    public int Tier;
    public SkillType Type;
    public float UnsealChance;

    // Classification
    public SkillFamily Family { get; set; } = SkillFamily.Core;
    public SkillCategory Category { get; set; } = SkillCategory.None;

    // Mechanics
    public List<SkillScaling> Scaling { get; set; } = new();
    public List<SkillEffect> Effects { get; set; } = new();
    public SkillHitRules? HitRules { get; set; } // For control/seal skills
    public SkillRestrictions? Restrictions { get; set; } // For special/limited skills

    // Requirements
    public SkillRequirements Requirements { get; set; } = new();

    public int Stage { get; set; } = 1;
    public SkillUnlockRules UnlockRules { get; set; } = new();
    public StageUpgradeRules? StageUpgradeRules { get; set; }

    public override string ToString() => $"{Name} ({Element}, {Branch}-T{Tier}) [SP: {SpCost}]";

    // Placeholder for application logic if needed, but logic should move to BattleInstance/System
    public void Apply(Character src, Character tgt)
    {
    }
}

public class SkillRequirements
{
    public int Str { get; set; }
    public int Con { get; set; }
    public int Int { get; set; }
    public int Wis { get; set; }
    public int Agi { get; set; }
}

public class SkillUnlockRules
{
    public int Level { get; set; }
    public int? ParentSkillId { get; set; }
    public int? ParentSkillRank { get; set; }
    public int? QuestId { get; set; }
    public string? QuestFlag { get; set; } // Required server-side flag (e.g., from quest completion)
}

public class StageUpgradeRules
{
    public int RankThreshold { get; set; }
    public int? NextSkillId { get; set; }
}