# Core Changelog

## [Unreleased]
### Current Verified State
*   **Networking**: Functional `GameServer` loop with `ClientSession`.
*   **Persistence**: Prototype `FilePlayerRepository` (JSON).
*   **Metrics**: Basic counters for Tick Time and Slippage.

### Production V1 Blockers (P0)
- **Persistence**: Migration to PostgreSQL for atomic transactions.
- **Networking**: Packet replay protection (Nonce/Sequence).
- **Security**: Movement validation (Speed/Teleport checks).
- **Death**: Implementation of 1% EXP Loss and 1 Durability Loss per death (SSOT Violation).

### Next Milestones (P1)
- **Observability**: Structured Logging (Serilog) implementation in critical paths (Combat/Trade).
- **Social**: Party and Guild Services (Currently Missing).
- **Market**: Hybrid Market System (Currently Missing).

### Added
- **Infrastructure**: Basic `FilePlayerRepository` (Prototype).
- **Networking**: `GameServer` loop and `ClientSession` handling.
