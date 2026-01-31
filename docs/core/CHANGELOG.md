# Core Changelog

## [Unreleased]
### Missing
- **Persistence**: Migration to PostgreSQL for atomic transactions.
- **Networking**: Packet replay protection (Nonce/Sequence).
- **Security**: Movement validation (Speed/Teleport checks).
- **Observability**: Structured Logging (Serilog) implementation in critical paths (Combat/Trade).
- **Death**: Implementation of 1% EXP Loss and 1 Durability Loss per death.

### Added
- **Infrastructure**: Basic `FilePlayerRepository` (Prototype).
- **Networking**: `GameServer` loop and `ClientSession` handling.
- **Metrics**: Basic `ServerMetrics` for Tick Time and Slippage.
