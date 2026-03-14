---
id: S07
parent: M001
milestone: M001
provides:
  - Market service foundation with persistent listings and history.
  - Item listing lifecycle: creation, cancellation, and automatic expiration.
  - Market discovery: search filters and price analytics.
  - Secure purchase settlement: atomic gold/item transfer with configurable tax.
  - Direct P2P trade window with dual-confirmation batch transfers.
requires:
  - slice: S06
    provides: Character rebirth transactional foundation.
affects:
  - S08: Compound System (will use market/trade foundations for materials)
key_files:
  - TWL.Server/Simulation/Managers/MarketManager.cs
  - TWL.Server/Simulation/Managers/TradeManager.cs
  - TWL.Server/Persistence/Database/DbService.cs
  - TWL.Shared/Domain/DTO/MarketDTOs.cs
  - TWL.Shared/Domain/DTO/TradeDTOs.cs
key_decisions:
  - Semi-Atomic Batch Transfer for trades to improve reliability.
  - Configurable market tax rate via environment variables.
  - Decoupled MarketQueryService for search performance.
patterns_established:
  - Batch transfer pattern with memory-state rollback for multi-entity mutations.
  - Expiration scheduler pattern for time-gated domain entities.
observability_surfaces:
  - Audit table: market_history
  - Market health: IMarketService.GetStats()
  - Logs: Server logs for listing lifecycle, purchase settlement, and trade workflow.
drill_down_paths:
  - .gsd/milestones/M001/slices/S07/tasks/T01-SUMMARY.md
  - .gsd/milestones/M001/slices/S07/tasks/T02-SUMMARY.md
  - .gsd/milestones/M001/slices/S07/tasks/T03-SUMMARY.md
  - .gsd/milestones/M001/slices/S07/tasks/T04-SUMMARY.md
  - .gsd/milestones/M001/slices/S07/tasks/T05-SUMMARY.md
duration: 4 days
verification_result: passed
completed_at: 2026-03-14
---

# S07: P2P Market System

**Implemented a production-ready server-authoritative marketplace and direct trade system with atomic transfers, audit logging, and configurable economy sinks.**

## What Happened

This slice established the core economic infrastructure for the game. We started by building the **Market Foundation (T01)**, including the database schema and network contracts. We then implemented the **Listing Lifecycle (T02)**, ensuring that items are correctly reserved in inventory when listed and returned when cancelled or expired via a dedicated scheduler.

To enable players to find what they need, we built the **Market Discovery system (T03)**, which provides paginated search with filters and price history analytics (min/avg/max). The **Purchase Settlement logic (T04)** was hardened with thread-safe atomic transfers and a configurable tax policy (default 5%) to act as a gold sink. Finally, we added the **Direct Trade system (T05)**, using a "Semi-Atomic Batch Transfer" pattern to safely exchange multiple items and gold between players with dual-confirmation safety.

## Verification

- **Automated Tests**: 33 regression tests in the `Market` namespace cover listing creation, cancellation, expiration, search, purchase settlement, idempotency, and direct trade.
- **Build**: Both `TWL.Server` and `TWL.Shared` build successfully.
- **Manual Verification**: Verified listing persistence and reload on server restart. Verified tax calculation and history recording in the database.

## Requirements Validated

- MKT-01 — Player can create item listing with price, quantity, and expiration.
- MKT-02 — Player can search market listings with filters.
- MKT-03 — Player can purchase listing with atomic gold/item transfer and tax deduction.
- MKT-04 — Player can cancel own listing before purchase.
- MKT-05 — Market displays price history (min/avg/max).
- MKT-06 — Configurable transaction fees are deducted from seller proceeds.
- MKT-07 — Listings expire and items return to seller.
- MKT-08 — Direct P2P trade window with both-party confirmation.

## Deviations

- **Semi-Atomic Batch Transfer**: Instead of sequential item transfers for trades, we implemented a batch transfer with rollback capability in memory to ensure consistency without complex 2PC on the server state.
- **DbService Table Rename**: Renamed `market_transactions` to `market_history` to better reflect its role as a permanent audit trail.

## Known Limitations

- **Trade Distance**: Currently no distance check for direct trades; players can trade as long as they are in the same map. This is deferred to future security hardening if needed.
- **Market Search Performance**: Using Dapper for search is fast, but we haven't implemented full-text search for item names; it currently uses `LIKE %name%`.

## Follow-ups

- **UI Integration**: While the network layer and client managers are done, the full UI implementation for the trade window is awaiting final asset delivery (though tested via mocks).

## Files Created/Modified

- `TWL.Server/Simulation/Managers/MarketManager.cs` — Core marketplace logic.
- `TWL.Server/Simulation/Managers/TradeManager.cs` — P2P trade orchestration.
- `TWL.Server/Persistence/Database/DbService.cs` — Market persistence implementation.
- `TWL.Shared/Domain/DTO/MarketDTOs.cs` — Network contracts for marketplace.
- `TWL.Shared/Domain/DTO/TradeDTOs.cs` — Network contracts for trading.
- `TWL.Tests/Market/` — Full suite of 33 market/trade tests.

## Forward Intelligence

### What the next slice should know
- The `TradeManager.TransferItemsBatch` method is the most reliable way to move items between players. Use it for any multi-item transfer features.
- Market tax is configured via the `MARKET_TAX_RATE` env var.

### What's fragile
- The `MarketListingScheduler` relies on the server clock. Drastic clock shifts could trigger premature or delayed expirations.

### Authoritative diagnostics
- `IMarketService.GetStats()` is the primary way to check the health of the marketplace.
- `market_history` DB table is the single source of truth for settled transactions.
