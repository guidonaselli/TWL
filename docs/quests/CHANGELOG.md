# Quests Changelog
> Tracks changes to Quest System, Objectives, and Narratives.

## [Unreleased]

### Missing to Production
- **Content Fixes**:
    - `Puerto Roca` questline (IDs 1100-1104) is defined but missing specific triggers/NPCs, causing test failures.
    - Localization Keys mismatch in Jungle Quests (`Into the Green` vs `El Camino del Bosque`).
- **Features**:
    - **Instance Objectives**: "Complete Instance X" logic is stubbed.
    - **Time Limits**: No support for timed quests.
    - **Escort Failure**: Death of escort target does not currently fail the quest.

### Added
- **System**: `ServerQuestManager` with JSON validation.
- **Objectives**: Support for `Kill`, `Deliver`, `Interact`, `PayGold`.
- **Integration**: `CombatManager` events now drive `Kill` objective progress automatically via `LastAttackerId`.

### Broken
- **Tests**: 8/292 tests failing due to missing/incorrect content in `quests.json`.
