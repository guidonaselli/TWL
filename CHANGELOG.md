# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Current Verified State
**Maturity**: Proto-Alpha (Vertical Slice Incomplete)
*   **Core**: Networking loop established.
*   **Economy**: Premium Currency & Shop implemented (Idempotent).
*   **Combat**: Basic turn-based resolution active.
*   **Persistence**: **PROTOTYPE ONLY** (JSON Files).

### Production V1 Blockers (P0)
*   **Persistence**: Migration from `FilePlayerRepository` to PostgreSQL (Atomic Transactions).
*   **Quality**: Fix 8 Failing Validation Tests (Localization, Quest Chains).
*   **Security**: Authoritative Movement Validation (Anti-Speedhack).
*   **Security**: Packet Replay Protection (Nonce/Sequence).

### Next Milestones (P1)
*   **Economy**: Hybrid Market (Centralized Ledger + Stalls) - *Currently Missing*.
*   **Social**: Party & Guild Systems - *Currently Missing*.
*   **World**: Instance Lockouts (5/day) & Isolation.
*   **Gameplay**: Death Penalty (1% EXP, 1 Durability) implementation.

### Added
- **Docs**: `PRODUCTION_GAP_ANALYSIS.md` detailing technical risks and feature matrix.
- **Docs**: `PRODUCTION_BACKLOG.md` with prioritized P0/P1 tasks.
- **Docs**: `docs/rules/GAMEPLAY_CONTRACTS.md` defining strict SSOT.

## [0.1.0] - 2024-05-01
### Added
- Initial Vertical Slice implementation.
- Basic Combat System (Turn-based).
- Quest Engine (Linear chains).
- Pet System (Basic Capture & Stats).
- File-based Persistence.
