# Quest System Changelog

## [Unreleased]

### Missing
- **Fixes**: 8 Validation Tests failing (Puerto Roca, Jungle Quests).
- **Logic**: Explicit "Failure" conditions (Time Limit, NPC Death) are not enforced in `ServerQuestManager`.
- **Validation**: `QuestGraphValidator` to detect dead-ends or loops is missing.

### Existing
- **Engine**: `ServerQuestManager` supports Linear Chains, Branching (Mutual Exclusion), and Flag-based gating.
- **Data**: `quests.json` defines initial content (needs repair).
- **State**: `PlayerQuestComponent` tracks active/completed quests.
