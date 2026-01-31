# Skills Changelog

## [Unreleased]
### Current Verified State
*   **System**: `SkillService` handles Learn, Upgrade, and Forget.
*   **Logic**: `UnlockRules` implemented.
*   **Status**: Partial (Engine ready, Content missing).

### Production V1 Blockers (P0)
- **Validation**: Add unit tests for `StageUpgradeRules`.
- **Logic**: Refactor `GrantGoddessSkills` (currently hardcoded) to be data-driven.

### Next Milestones (P1)
- **Content**: Implement Earth/Water/Fire/Wind T1 Skill Packs.
- **Balancing**: Define Tier Budgets for SP and Damage Coefficients.
- **Logic**: Implement Advanced Counters (Stacking Policies, Resistance).

### Added
- **Logic**: `StatusEngine` supporting Buffs/Debuffs/DoTs.
