---
id: S07
title: P2P Market System
milestone: M001
written: 2026-03-14
---

# S07: P2P Market System — UAT

**Milestone:** M001
**Written:** 2026-03-14

## UAT Type

- UAT mode: artifact-driven & live-runtime
- Why this mode is sufficient: The market and trade systems are backend-heavy with complex state transitions that are best verified through comprehensive integration tests and database audits.

## Preconditions

- TWL.Server is running with an active PostgreSQL database.
- Database migrations for `market_listings` and `market_history` have been applied.
- Two test accounts (Seller and Buyer) are created with sufficient inventory and gold.

## Smoke Test

- **Listing Creation**: Seller lists a "Red Potion" for 100 Gold. Verify the item is removed from Seller's inventory and a record appears in `market_listings` with status `Active`.

## Test Cases

### 1. Market Purchase Settlement

1. Seller lists "Common Sword" for 500 Gold.
2. Buyer (with 1000 Gold) searches for "Common Sword" and finds the listing.
3. Buyer sends a purchase request for the listing ID.
4. **Expected:** 
   - Buyer loses 500 Gold and gains "Common Sword".
   - Seller gains 475 Gold (after 5% tax).
   - Listing status in DB becomes `Sold`.
   - A record in `market_history` shows the transaction with 25 Gold tax recorded.

### 2. Listing Expiration

1. Seller lists "Old Map" for 1000 Gold with a very short expiration (via debug override or manual DB edit).
2. Wait for `MarketListingScheduler` to process.
3. **Expected:** 
   - Listing status in DB becomes `Expired`.
   - "Old Map" is returned to Seller's inventory or mailbox (depending on implementation, currently inventory).
   - Server logs show "Listing [ID] expired and item returned to Seller [ID]".

### 3. Direct P2P Trade dual-confirmation

1. Player A invites Player B to trade.
2. Player B accepts.
3. Player A offers 100 Gold and a "Wooden Shield".
4. Player B offers a "Bronze Helmet".
5. Player A clicks "Confirm". Trade state becomes `Locked` for A.
6. Player B clicks "Confirm". Trade executes.
7. **Expected:** 
   - Player A has "Bronze Helmet" and 0 Gold.
   - Player B has "Wooden Shield", 100 Gold, and 0 "Bronze Helmet".
   - Server logs verify "Trade [ID] executed successfully".

## Edge Cases

### Purchase Race Condition

1. Two buyers attempt to purchase the same listing at the exact same millisecond.
2. **Expected:** Only one buyer succeeds; the other receives a "Listing no longer available" error. Database serializable isolation prevents double-selling.

### Trade Inventory Full

1. Player A offers an item to Player B.
2. Player B has a full inventory.
3. Both players confirm the trade.
4. **Expected:** Trade fails and is cancelled. Items are returned to original owners. Server logs "Trade [ID] cancelled: Target inventory full".

## Failure Signals

- **Console Errors**: "MarketManager: Error processing purchase" or "TradeManager: Batch transfer failed".
- **Database Inconsistency**: A listing marked `Sold` but no corresponding entry in `market_history`.
- **Negative Balances**: Gold dropping below zero during a transaction.

## Requirements Proved By This UAT

- MKT-01, MKT-02, MKT-03, MKT-04, MKT-05, MKT-06, MKT-07, MKT-08.

## Not Proven By This UAT

- Long-term scalability of the `market_history` table (e.g., millions of rows).
- Client-side UI animations or drag-and-drop experience.

## Notes for Tester

- Use `MARKET_TAX_RATE` env var to test different tax scenarios.
- Check `IMarketService.GetStats()` to verify total volume tracked by the server matches the sum of transactions.
