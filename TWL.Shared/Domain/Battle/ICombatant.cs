using TWL.Shared.Domain.Characters;

namespace TWL.Shared.Domain.Battle;

/// <summary>
/// Defines the contract for an entity participating in combat.
/// Exposes stats and attributes required for damage calculation.
/// </summary>
public interface ICombatant
{
    int Id { get; }
    string Name { get; }
    Element CharacterElement { get; }

    // Primary Stats
    int Str { get; }
    int Con { get; }
    int Int { get; }
    int Wis { get; }
    int Agi { get; }

    // Derived Battle Stats (including modifiers)
    int Atk { get; }
    int Def { get; }
    int Mat { get; }
    int Mdf { get; }
    int Spd { get; }
}
