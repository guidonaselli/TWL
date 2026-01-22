namespace TWL.Shared.Domain.Skills;

public enum SkillBranch
{
    Physical, // ATK
    Magical,  // MATK
    Support   // SUPPORT
}

public enum SkillTargetType
{
    SingleEnemy,
    Self,
    SingleAlly,
    AllEnemies,
    AllAllies,
    RowEnemies, // E.g. front row
    RowAllies
}

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
    Burn // DoT
}
