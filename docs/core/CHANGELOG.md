# Core System Changelog
> Tracks changes in Architecture, Netcode, Persistence, Security, and base Gameplay Loop.

## [Unreleased]

### Missing to Production (Core)
- **Persistence**: `FilePlayerRepository` is not suitable for concurrency/production. Needs `Postgres` or `Mongo` adapter.
- **Testing**: Fix `TWL.Tests` file path resolution so `quests.json` can be loaded during tests.
- **Combat Logic**: Investigate and fix `AquaImpact` (Water Skill) dealing 0 damage in unit tests.
- **Security**: Implement strict Rate Limiting per Opcode in `ClientSession`.

### Added
- **Server Loop**: `WorldScheduler` implementing 50ms fixed tick loop.
- **Combat Engine**:
  - `CombatManager`: Handles Turn logic, Cooldowns, SP consumption.
  - `StandardCombatResolver`: Implements `(Stat * Coeff * ElemMult) - Def` formula.
  - `StatusEngine`: Supports `Buff`, `Debuff`, `Cleanse`, `Dispel`, `Seal` tags.
  - **Elements**: Water > Fire > Wind > Earth > Water cycle implemented.
- **Validation**:
  - `MapValidator` for TMX triggers.
  - `QuestValidator` for JSON objectives.

### Changed
- `ClientSession`: Added `HandleMoveAsync` with server-side trigger checks (`CheckTriggers`).
