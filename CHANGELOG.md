# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Current Verified State
**Maturity**: Proto-Alpha (Vertical Slice In Progress)
*   **Core**: Networking loop established. Protocol versioning pending (P0).
*   **Economy**: Premium Currency & Shop implemented (Idempotent).
*   **Combat**: Basic turn-based resolution active. Pet AI integrated.
*   **Persistence**: **Hybrid PostgreSQL** (EF Core + Dapper) active. `FilePlayerRepository` removed.

### Production V1 Blockers (P0)
*   **Security**: Protocol Schema Validation (Fail-Closed).
*   **Security**: Authoritative Movement Validation (Anti-Speedhack).
*   **Security**: Packet Replay Protection (Nonce/Sequence).
*   **Quality**: Fix 5 Failing Validation/Reliability Tests.

### Next Milestones (P1)
*   **Economy**: Hybrid Market (Centralized Ledger + Stalls) - *Currently Missing*.
*   **Social**: Party & Guild Systems - *Currently Missing*.
*   **World**: Instance Lockouts (5/day) & Isolation.
*   **Gameplay**: Death Penalty (1% EXP, 1 Durability) implementation.

### Added
- **Docs**: `PRODUCTION_GAP_ANALYSIS.md` detailing technical risks and feature matrix.
- **Docs**: `PRODUCTION_BACKLOG.md` with prioritized P0/P1 tasks.
- **Docs**: `docs/rules/GAMEPLAY_CONTRACTS.md` defining strict SSOT.

### Changed
- **Persistence**: Replaced `FilePlayerRepository` with `DbPlayerRepository` (PostgreSQL).
- **Pets**: Integrated `AutoBattleManager` for Pet AI.
- **Roadmap**: Re-prioritized `CORE-001` (Security) as P0. Split `ECO-005` (Marketplace).

## [0.1.0] - 2024-05-01
### Added
- Initial Vertical Slice implementation.
- Basic Combat System (Turn-based).
- Quest Engine (Linear chains).
- Pet System (Basic Capture & Stats).
- File-based Persistence.
