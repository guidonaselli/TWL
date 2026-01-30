# Quests System Changelog
> Tracks changes in Quest Engine, Content, and Progression.

## [Unreleased]

### Missing to Production (Quests)
- **Content**: Implement "Puerto Roca" Questline (IDs 1100-1104) and "Tent" Quest.
- **Testing**: Fix test harness to correctly load `quests.json` (currently blocking all content validation tests).
- **Localization**: Fix mismatch in Jungle Quest titles (`Into the Green` vs `El Camino del Bosque`).

### Added
- **Quest Engine**:
  - `ServerQuestManager`: Loads and validates `QuestDefinition` JSONs.
  - `PlayerQuestComponent`: Handles `TryProgress` for `Kill`, `Deliver`, `Interact`.
  - `QuestValidator`: Enforces DataId validity and objective structure.
- **Objectives Support**:
  - `Kill`: Verified via `OnCombatantDeath` and `LastAttackerId`.
  - `Deliver`: Basic item consumption support.
  - `PayGold`: Gold deduction support.

### Changed
- Refactored `QuestState` to reside in `TWL.Shared.Domain.Requests` (Note: Consider moving to `Domain.Quests` for consistency).
