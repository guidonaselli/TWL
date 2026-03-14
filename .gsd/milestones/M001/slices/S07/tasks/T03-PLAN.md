# T03: 07-p2p-market-system 03

**Slice:** S07 — **Milestone:** M001

## Description

Implement market discovery features: listing filters and item price-history analytics.

Purpose: This delivers MKT-02 and MKT-05 and prepares stable query/read flows for execution UI.
Output: Query service + search/history DTO expansion + client ingestion + search/history regression tests.

## Must-Haves

- [ ] "Players can search and filter listings by name, type, price range, and rarity"
- [ ] "Market query responses are server-driven and stable for client rendering"
- [ ] "Players can view min/avg/max price history for recent item transactions"

## Files

- `TWL.Server/Simulation/Managers/MarketQueryService.cs`
- `TWL.Server/Simulation/Managers/MarketManager.cs`
- `TWL.Shared/Domain/DTO/MarketDTOs.cs`
- `TWL.Shared/Domain/Requests/MarketplaceUpdate.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Client/Presentation/Managers/ClientMarketplaceManager.cs`
- `TWL.Tests/Market/MarketSearchTests.cs`
- `TWL.Tests/Market/MarketPriceHistoryTests.cs`
