using System.Collections.Generic;
using TWL.Shared.Domain.Characters;

namespace TWL.Shared.Domain.Skills;

public class Skill
{
    // Identifiers
    public int SkillId;
    public string Name = string.Empty;
    public string DisplayNameKey = string.Empty; // For UI localization
    public string Description = string.Empty;

    // Classification
    public SkillFamily Family { get; set; } = SkillFamily.Core;
    public SkillCategory Category { get; set; } = SkillCategory.None;

    // Core Attributes
    public Element Element;
    public SkillBranch Branch;
    public int Tier;
    public SkillTargetType TargetType;

    // Usage
    public int SpCost;
    public int Cooldown;

    // Mechanics
    public List<SkillScaling> Scaling { get; set; } = new();
    public List<SkillEffect> Effects { get; set; } = new();
    public SkillHitRules? HitRules { get; set; } // For control/seal skills
    public SkillRestrictions? Restrictions { get; set; } // For special/limited skills

    // Legacy / Compatibility fields (Can be mapped from new structure or kept for backward compat)
    public int Id; // Redundant, same as SkillId usually
    public SkillType Type;
    public float Power;
    public float SealChance;
    public float UnsealChance;
    public int Level;
    public int MaxLevel;

    // Requirements
    public int StrRequirement;
    public int ConRequirement;
    public int IntRequirement;
    public int WisRequirement;
    public int AgiRequirement;

    public int Stage { get; set; } = 1;
    public SkillUnlockRules UnlockRules { get; set; } = new();
    public StageUpgradeRules? StageUpgradeRules { get; set; }

    public override string ToString()
    {
        return $"{Name} ({Element}, {Branch}-T{Tier}) [SP: {SpCost}]";
    }

    // Placeholder for application logic if needed, but logic should move to BattleInstance/System
    public void Apply(Character src, Character tgt) { }
}

public class SkillUnlockRules
{
    public int Level { get; set; }
    public int? ParentSkillId { get; set; }
    public int? ParentSkillRank { get; set; }
    public string? QuestId { get; set; }
    public string? QuestFlag { get; set; } // Required server-side flag (e.g., from quest completion)
}

public class StageUpgradeRules
{
    public int RankThreshold { get; set; }
    public int? NextSkillId { get; set; }
}
