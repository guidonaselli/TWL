using TWL.Shared.Domain.Characters;

namespace TWL.Shared.Domain.Skills;

public class Skill
{
    public int Id; // Identificador único

    public int AgiRequirement;
    public int ConRequirement;
    public string Description;
    public Element Element; // Fire, Wind, Earth, Water
    public int IntRequirement;
    public float Level;
    public int MaxLevel;
    public string Name; // Ej: "Fire Punch", "Wind Seal", etc.

    // Poder base (se usará en el cálculo de daño o sanación)
    public float Power;

    // Ej: para un skill de Seal, definimos la probabilidad base de sell
    public float SealChance;
    public int SkillId; // Identificador único

    // Coste de SP para usar la habilidad
    public int SpCost;

    // Requisitos para aprenderla (ej: STR >= 30, o INT >= 50, etc.)
    public int StrRequirement;
    public SkillType Type; // PhysicalDamage, MagicalDamage, Seal, Unseal, etc.

    // Para un skill de "unseal", definimos la probabilidad de removerlo
    public float UnsealChance;
    public int WisRequirement;

    public override string ToString()
    {
        return $"{Name} ({Element}, {Type}) [Req: STR>={StrRequirement}, INT>={IntRequirement}, WIS>={WisRequirement}]";
    }

    public void Apply(Character src, Character tgt) { /*…*/ }
}

// Notas:
//
// Power: un número base que mezclaremos con STR o INT.
//
//     SpCost: SP/MP necesario para lanzarla.
//
//     SealChance, UnsealChance: probabilidad de aplicar/quitar seal (puedes poner 1.0f para 100% si no quieres fallo).