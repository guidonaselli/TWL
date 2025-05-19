namespace TWL.Shared.Domain.Skills;

public enum SkillType
{
    PhysicalDamage, // Usa STR para el daño
    MagicalDamage, // Usa INT para el daño
    Seal, // Sella al objetivo
    Unseal, // Quita sell
    Buff, // Aumenta stats del aliado
    Debuff // Quita stats del enemigo
}