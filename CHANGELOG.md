# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Missing to Production (Critical Gaps)
- **Persistence Layer**: Currently using `FilePlayerRepository` (JSON). **Must migrate to PostgreSQL** (P0) to prevent data loss and support atomic transactions.
- **Content Integrity**: 8/292 tests failing. `quests.json` has missing IDs/Keys. `Puerto Roca` questline is broken.
- **Security**: No authoritative Movement Validation (Anti-Speedhack). No Packet Replay protection.
- **Market System**: "Hybrid Market" (Centralized Listings + Stalls) is not implemented.
- **Instance Isolation**: Dungeon maps are currently shared world. Need dynamic instance cloning for Parties.
- **Pet Systems**: Amity logic and Death penalties (Amity loss on KO) are missing.

### Added
- **Core Architecture**: `GameServer` loop (50ms tick), `ClientSession` pipeline, and `PacketHandler`.
- **Test Harness Fixes**: Fixed `FileNotFoundException` in `TWL.Tests` by correctly resolving `Content/Data` paths (PR #FixTests).
- **Documentation**: Added `docs/PRODUCTION_GAP_ANALYSIS.md` detailing the path to Vertical Slice.

### Changed
- **Combat**: Refactored `CombatManager` to support `LastAttackerId` for Quest credit.
- **Networking**: Updated `ClientSession` to use `Mediator` pattern for Skills and Interactions.
- **World**: `WorldTriggerService` now handles basic Map Transitions.

### Known Issues
- **Test Failures**: `JungleQuestTests` (Loc mismatch), `PuertoRocaQuestTests` (Missing Data), `HiddenRuinsQuestTests` (Logic).
- **Performance**: `GameServer` uses manual dependency injection; scaling will require a DI Container Refactor.
