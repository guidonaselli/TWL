# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Missing to Production (Critical Gaps)
- **CI/CD & Testing**:
  - `dotnet test` is failing for 18/290 tests due to File Path resolution errors (`Content/Data/quests.json` not found).
  - Combat Unit Tests (Water Skills) are failing with 0 damage (Logic or Test Setup issue).
  - No automated Content Validation in CI pipeline.
- **Persistence**:
  - Current implementation is `FilePlayerRepository` (JSON). Needs migration to SQL/NoSQL for production (P0).
- **Security**:
  - Rate Limiting and Packet Validation are stubbed but not battle-hardened.
  - No explicit "Anti-Speedhack" validation in movement packets yet.
- **Content**:
  - Maps (Puerto Roca) are stubbed but missing complete Trigger/Spawn definitions.
  - Quests for "Starter Island" and "Puerto Roca" are defined but fail validation in tests.

### Added
- **Core Architecture**:
  - Server-Authoritative loop (`GameServer`, `WorldScheduler`) targeting 50ms ticks.
  - Packet handling pipeline (`ClientSession`, `PacketHandler`).
  - Domain-Driven Design structure (`TWL.Shared`, `TWL.Server`).
- **Systems**:
  - **Combat**: `CombatManager` and `StandardCombatResolver` implemented with Elements (Water/Fire/Wind/Earth), Stats, and Turn order.
  - **Skills**: `SkillService` with JSON-based definitions, Cooldowns, SP cost, and Status Effects (`Buff`, `Debuff`, `Seal`).
  - **Quests**: `ServerQuestManager` with JSON validation and objective tracking (`Kill`, `Deliver`, `Interact`).
  - **Pets**: Basic `PetService` and `PetDefinition`.
  - **World**: `WorldTriggerService` for map transitions and spawn points.

### Changed
- Refactored `CombatManager` to support `LastAttackerId` for Kill Quest credit.
- Updated `MapLoader` to enforce TMX layer strictness.

### Known Issues
- `AquaImpact` skill test returns 0 damage.
- Localization keys mismatch in Jungle Quests (`Into the Green` vs `El Camino del Bosque`).
