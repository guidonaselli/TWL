# Skills System Changelog
> Tracks changes in Skill Definitions, Mechanics, and Packs.

## [Unreleased]

### Missing to Production (Skills)
- **Content**: Implement full Tier 1 Skill Packs for Earth, Water, Fire, Wind.
- **Validation**: `SkillMigrationTests` and `ServerWaterSkillTests` are failing.
- **Goddess Skills**: Verify IDs 2001-2004 are reserved and not granted by Quests (Test `NoQuestsGrantGoddessSkills` is failing due to missing file).

### Added
- **Skill Engine**:
  - `SkillDefinition` schema with `Effects`, `Scaling`, `Branch` (Physics/Magic/Support).
  - `StageUpgradeRules` for skill evolution (Rank-based).
  - `UnlockRules` for prerequisites.
- **Mechanics**:
  - `ResistanceTags` logic in `CombatManager`.
  - `ConflictGroup` and `StackingPolicy` for Status Effects.

### Changed
- `StandardCombatResolver`: Now supports Elemental Multipliers (1.5x / 0.5x).
