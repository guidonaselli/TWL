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

## Steps

1. **Database Schema**: Ensure `MarketHistory` table exists to record purchases and tax.
2. **DTO Update**: Define `MarketPurchaseRequest` and `MarketPurchaseResponse` in `MarketDTOs.cs`.
3. **Economy Manager**: Add tax calculation logic to `EconomyManager.cs`.
4. **Market Manager**: Implement `PurchaseListingAsync` in `MarketManager.cs` with atomic transfer (gold/item) and tax handling.
5. **Idempotency**: Implement check for already sold/cancelled listings to prevent double-spending/duplication.
6. **Network Routing**: Update `ClientSession.cs` to handle `MarketPurchaseRequest`.
7. **Testing**: Write regression tests for settlement, tax, and idempotency.

## Observability Impact

- **Logs**: Every successful purchase will be logged with `[Market] Purchase Settled: Listing {ListingId}, Buyer {BuyerId}, Seller {SellerId}, Price {Price}, Tax {Tax}`.
- **Failures**: Failed purchases (insufficient gold, listing gone) will be logged as warnings with specific reasons.
- **Persistence**: The `MarketHistory` table will serve as the primary audit trail for all market transactions.
