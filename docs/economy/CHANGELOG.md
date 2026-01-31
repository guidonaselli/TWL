# Economy System Changelog

## [Unreleased]

### Missing
- **Market**: Hybrid System (Centralized Ledger + Player Stalls) is unimplemented.
- **Trading**: `TradeManager` exists but lacks atomic cross-player locking for complex scenarios.
- **Anti-Dupe**: Strict Transaction Ledger for all Item movements is not enforced.

### Existing
- **Ledger**: `EconomyManager` logs Gem/Gold transactions to file.
- **Policies**: `BindPolicy` (BoP/BoE/AccountBound) is supported in `Item` model.
- **Shops**: Basic NPC Shop buy/sell logic is implemented.
