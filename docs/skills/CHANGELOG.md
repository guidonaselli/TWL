# Skills Changelog
> Tracks changes to Skill System, Effects, and Calculation logic.

## [Unreleased]

### Missing to Production
- **Progression**: `StageUpgradeRules` (Rank-based evolution) needs dedicated verification tests.
- **Mechanics**:
    - Complex Targeting (Row/Column/Cross) support in `StandardCombatResolver`.
    - Goddess Skills: Hardcoded `GrantGoddessSkills` needs to be moved to a Quest/Event trigger system.
- **Content**:
    - Full implementation of `Water`, `Fire`, `Wind` skill packs (currently stubbed).

### Added
- **System**: `SkillService` with JSON-based definitions.
- **Effects**: Basic Status Effects (`Buff`, `Debuff`, `Seal`, `Cleanse`, `Dispel`).
- **Logic**: `StandardCombatResolver` implementing Elemental Multipliers (Water > Fire > Wind > Earth).

### Known Issues
- `AquaImpact` test formerly returned 0 damage (Fixed in previous iterations, now monitoring).
