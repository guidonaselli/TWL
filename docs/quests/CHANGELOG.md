# Quests Changelog

## [Unreleased]
### Current Verified State
*   **Engine**: `ServerQuestManager` supports Linear Chains and Branching.
*   **Components**: `PlayerQuestComponent` handles persistence.
*   **Status**: **Unstable/Broken**.

### Production V1 Blockers (P0)
- **Validation**: Fix 8 Failing Tests:
    - `LocalizationValidationTests` (Missing Keys)
    - `HiddenCoveTests` (Chain Broken)
    - `QuestRuinsExpansionTests` (Chain Broken)
    - `HiddenRuinsQuestTests` (Chain Broken)
- **Logic**: Implement `TimeLimitSeconds` enforcement (Quest Failure).
- **Logic**: Implement "Instance Wipe" failure condition.

### Next Milestones (P1)
- **Content**: "Starter Island" and "Puerto Roca" questlines (IDs 1000-1200).
- **Content**: Complete Localization for all Quest Titles/Descriptions.

### Added
- **Logic**: `QuestDefinition` schema with Rewards, Objectives, and Flags.
