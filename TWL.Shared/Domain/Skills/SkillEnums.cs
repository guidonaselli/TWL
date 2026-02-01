using System.Text.Json.Serialization;

namespace TWL.Shared.Domain.Skills;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SkillBranch
{
    Physical, // ATK
    Magical, // MATK
    Support // SUPPORT
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SkillTargetType
{
    SingleEnemy,
    Self,
    SingleAlly,
    AllEnemies,
    AllAllies,
    RowEnemies, // E.g. front row
    RowAllies,
    ColumnEnemies,
    CrossEnemies
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SkillEffectTag
{
    None,
    Damage,
    Heal,
    Shield,
    BuffStats, // Param: Stat name (e.g. "Atk")
    DebuffStats,
    Cleanse, // Remove Debuffs
    Dispel, // Remove Buffs
    Seal, // Control
    Burn, // DoT
    RestoreSp // Instant SP Restore
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SkillFamily
{
    Core,
    Special
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SkillCategory
{
    None,
    RebirthJob,
    ElementSpecial,
    Fairy,
    Dragon,
    Griffin,
    Goddess
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RebirthClass
{
    None,
    Champion,
    Defender,
    Slayer,
    Archmage,
    Oracle,
    Sage
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StackingPolicy
{
    NoStackOverwrite, // Default: New overwrites old
    RefreshDuration, // Refresh duration only
    StackUpToN, // Increment stack count up to N
    SeparateInstances // Allow multiple instances
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OutcomeModel
{
    Full, // Standard application
    Partial, // Duration/Magnitude reduced
    Resist, // Failed application due to resistance
    Immunity // Hard immunity
}