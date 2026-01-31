# Skills System Changelog

## [Unreleased]

### Missing
- **Content Integrity**: Tests failing for Skill Keys (Localization).
- **Validation**: `StageUpgradeRules` logic is implemented but lacks specific unit tests.
- **Complex Scopes**: Row/Column targeting logic is stubbed.

### Existing
- **System**: `SkillService` handles learning and upgrading.
- **Logic**: `SkillRegistry` loads definitions. `StandardCombatResolver` applies damage/effects.
- **Progression**: Mastery-by-Use (Rank) and Stage Evolution logic is present in `ServerCharacter.ReplaceSkill`.
