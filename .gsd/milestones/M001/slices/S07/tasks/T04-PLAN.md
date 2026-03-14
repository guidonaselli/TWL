# T04: 07-p2p-market-system 04

**Slice:** S07 — **Milestone:** M001

## Description

Implement listing purchase settlement with atomic transfer, tax handling, and idempotency.

Purpose: This delivers MKT-03 and MKT-06 while satisfying MKT-08's anti-duplication expectation for valuable transfers.
Output: Atomic purchase flow + configurable tax policy + settlement/idempotency/tax regression tests.

## Must-Haves

- [ ] "Players can purchase listings with atomic gold/item transfer between buyer and seller"
- [ ] "Market purchase applies configurable tax and transfers net proceeds correctly"
- [ ] "Duplicate purchase attempts are idempotent and cannot double-spend or duplicate items"

## Files

- `TWL.Server/Simulation/Managers/MarketManager.cs`
- `TWL.Server/Simulation/Managers/EconomyManager.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Shared/Domain/DTO/MarketDTOs.cs`
- `TWL.Server/Persistence/Database/DbService.cs`
- `TWL.Tests/Market/MarketPurchaseSettlementTests.cs`
- `TWL.Tests/Market/MarketIdempotencyTests.cs`
- `TWL.Tests/Market/MarketTaxCalculationTests.cs`
