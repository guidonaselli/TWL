# Quests Changelog

## [Unreleased]
### Missing (Fixes Required)
- **Validation**: 8 Tests Failing in `PuertoRocaQuestTests` and `LocalizationValidationTests` (Missing IDs/Keys).
- **Logic**: `TimeLimitSeconds` enforcement (Quest Failure).
- **Logic**: "Instance Wipe" failure condition.
- **Content**: "Starter Island" and "Puerto Roca" questlines (IDs 1000-1200).

### Added
- **Engine**: `ServerQuestManager` supporting Linear Chains and Branching (MutualExclusion).
- **Components**: `PlayerQuestComponent` for persistence.
- **Logic**: `QuestDefinition` schema with Rewards, Objectives, and Flags.
