# Skills Changelog

## [Unreleased]
### Missing
- **Validation**: `StageUpgradeRules` logic is implemented but lacks specific unit tests.
- **Content**: Earth/Water/Fire/Wind T1 Skill Packs (Data entry).
- **Logic**: `GrantGoddessSkills` is hardcoded; needs to be data-driven or flag-based.
- **Balancing**: Tier Budgets for SP and Damage Coefficients.

### Added
- **System**: `SkillService` handling Learn, Upgrade, and Forget.
- **Logic**: `UnlockRules` (Stat-based and QuestFlag-based).
- **Logic**: `StatusEngine` supporting Buffs/Debuffs/DoTs.
