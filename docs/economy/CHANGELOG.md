# Economy Changelog
> Tracks changes to Market, Trade, Crafting, and Currency.

## [Unreleased]

### Missing to Production
- **Market System**:
    - **Centralized Listings**: Schema and Logic for Global Auction House / Listings.
    - **Player Stalls**: UI/Logic to view listings via "Tent" frontend.
- **Security**:
    - **Transaction Ledger**: No audit log for Gems/Gold exchanges.
    - **Idempotency**: `BuyShopItem` lacks idempotency keys (Double-spend risk).
- **Crafting**: Alchemy (Compound) and Manufacturing logic is missing.

### Added
- **System**: Basic `EconomyManager` for Shop Purchases (`BuyShopItem`).
- **Currency**: Support for Gold and Gems (Premium Currency) flow (`PurchaseGemsIntent`).

### Planned
- **Hybrid Market**: Design decision finalized (Centralized Listings + Stall Frontend).
