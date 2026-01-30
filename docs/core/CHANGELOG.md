# Core Changelog
> Tracks changes to Server Architecture, Networking, Persistence, and Security.

## [Unreleased]

### Missing to Production
- **Persistence**: Migration from `FilePlayerRepository` (JSON) to `PostgresPlayerRepository` (SQL).
- **Security**:
    - Movement Validation (Server-side speed/distance check).
    - Packet Sequence/Nonce validation (Anti-Replay).
- **Observability**: Structured Logging (Serilog) integration in `CombatManager` and `TradeManager`.

### Added
- **Networking**: `ClientSession` implementation with `RateLimiter` and `Mediator` pattern.
- **World Loop**: `WorldScheduler` targeting 50ms ticks with basic slippage metrics.
- **Test Infrastructure**: `TestContentLocator` (via regex fix) to resolve `Content/Data` in Unit Tests.

### Changed
- **Architecture**: `GameServer` now manually injects `CombatManager`, `QuestManager`, etc. (Needs Refactor to DI Container).
