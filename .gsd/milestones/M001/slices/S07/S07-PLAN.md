# S07: P2p Market System

**Goal:** Create Phase 7 market foundation: domain service, persistence schema, and network contracts.
**Demo:** Create Phase 7 market foundation: domain service, persistence schema, and network contracts.

## Must-Haves


## Tasks

- [x] **T01: 07-p2p-market-system 01**
  - Create Phase 7 market foundation: domain service, persistence schema, and network contracts.

Purpose: This establishes the server-authoritative base needed for all listing/search/purchase/trade features.
Output: Market service interface + manager, DTO/opcode surface, persistence schema updates, and contract tests.
- [x] **T02: 07-p2p-market-system 02**
  - Implement listing lifecycle operations including create, cancel, and automatic expiration return.

Purpose: This delivers MKT-01, MKT-04, and MKT-07 with abuse-resistant lifecycle handling.
Output: Listing state machine + expiration scheduler + lifecycle regression tests.
- [x] **T03: 07-p2p-market-system 03**
  - Implement market discovery features: listing filters and item price-history analytics.

Purpose: This delivers MKT-02 and MKT-05 and prepares stable query/read flows for execution UI.
Output: Query service + search/history DTO expansion + client ingestion + search/history regression tests.
- [ ] **T04: 07-p2p-market-system 04**
  - Implement listing purchase settlement with atomic transfer, tax handling, and idempotency.

Purpose: This delivers MKT-03 and MKT-06 while satisfying MKT-08's anti-duplication expectation for valuable transfers.
Output: Atomic purchase flow + configurable tax policy + settlement/idempotency/tax regression tests.
- [ ] **T05: 07-p2p-market-system 05**
  - Implement direct player-to-player trade window and finalize client market/trade integration.

Purpose: This delivers MKT-08 and completes the market phase with a server-authoritative live trading path.
Output: Trade session manager + trade DTO/opcode routing + client trade window integration + direct-trade regression tests.

## Files Likely Touched

- `TWL.Server/Simulation/Managers/IMarketService.cs`
- `TWL.Server/Simulation/Managers/MarketManager.cs`
- `TWL.Server/Persistence/Database/DbService.cs`
- `TWL.Shared/Domain/DTO/MarketDTOs.cs`
- `TWL.Shared/Net/Network/Opcode.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Server/Simulation/Program.cs`
- `TWL.Tests/Market/MarketContractTests.cs`
- `TWL.Server/Simulation/Managers/MarketManager.cs`
- `TWL.Server/Simulation/Managers/MarketListingScheduler.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Server/Persistence/Services/PlayerService.cs`
- `TWL.Shared/Domain/DTO/MarketDTOs.cs`
- `TWL.Tests/Market/MarketListingLifecycleTests.cs`
- `TWL.Tests/Market/MarketExpirationTests.cs`
- `TWL.Server/Simulation/Managers/MarketQueryService.cs`
- `TWL.Server/Simulation/Managers/MarketManager.cs`
- `TWL.Shared/Domain/DTO/MarketDTOs.cs`
- `TWL.Shared/Domain/Requests/MarketplaceUpdate.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Client/Presentation/Managers/ClientMarketplaceManager.cs`
- `TWL.Tests/Market/MarketSearchTests.cs`
- `TWL.Tests/Market/MarketPriceHistoryTests.cs`
- `TWL.Server/Simulation/Managers/MarketManager.cs`
- `TWL.Server/Simulation/Managers/EconomyManager.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Shared/Domain/DTO/MarketDTOs.cs`
- `TWL.Server/Persistence/Database/DbService.cs`
- `TWL.Tests/Market/MarketPurchaseSettlementTests.cs`
- `TWL.Tests/Market/MarketIdempotencyTests.cs`
- `TWL.Tests/Market/MarketTaxCalculationTests.cs`
- `TWL.Server/Simulation/Managers/TradeSessionManager.cs`
- `TWL.Server/Simulation/Managers/TradeManager.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Shared/Domain/DTO/TradeDTOs.cs`
- `TWL.Shared/Net/Network/Opcode.cs`
- `TWL.Client/Presentation/Managers/GameClientManager.cs`
- `TWL.Client/Presentation/Managers/ClientMarketplaceManager.cs`
- `TWL.Client/Presentation/UI/UiTradeWindow.cs`
- `TWL.Tests/Market/DirectTradeWindowTests.cs`
- `TWL.Tests/Market/MarketTradeIntegrationTests.cs`
