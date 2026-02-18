# Phase 7: P2P Market System - Research

**Researched:** 2026-02-17  
**Domain:** Player-to-player market listings, purchase settlement, search/indexing, and direct trade integration  
**Confidence:** High

## Summary

Phase 7 has partial economic/security primitives but no server-authoritative P2P marketplace yet. The current `EconomyManager` handles premium-currency shop purchases with idempotency, append-only ledger hashing, and compensation patterns, while `TradeManager` handles bind-policy-safe direct item transfer. However, there is no market listing aggregate, no listing search/filter API, no listing purchase settlement, no listing expiration return flow, and no market price-history projection.

Client-side marketplace code exists (`MarketplaceManager`, `UiMarketplace`, `SceneMarketplace`) but is local/in-memory and not backed by server contracts. This should be treated as provisional UI scaffolding that must be rewired to server-driven data.

The safest implementation path is to reuse proven patterns already in the codebase:
- idempotency operation keys from `EconomyManager`
- strict item-policy transfer rules from `TradeManager`
- network handling conventions in `ClientSession` + `Opcode`
- persistence discipline from `PlayerService` and repository-backed save flows

## Current State Findings

### 1. Economy primitives are strong, marketplace domain is missing

Observed:
- `EconomyManager` supports transaction states, rate limiting, idempotency, compensation on failure, and hashed ledger chain.
- `IEconomyService` exposes only premium shop flows (`InitiatePurchase`, `VerifyPurchase`, `BuyShopItem`, `GiftShopItem`).
- No listing model/service/API exists for player-item market operations.

Implication:
- Market should be implemented as a dedicated service/domain using existing economy reliability patterns.

### 2. Server networking has no market opcode surface

Observed:
- `Opcode` includes `BuyShopItemRequest` and premium purchase ops only.
- `ClientSession` has handlers for shop purchase, but none for create listing, cancel listing, market search, or listing purchase.

Implication:
- Need explicit market opcode + DTO surface before any client/server feature flow is possible.

### 3. Existing client marketplace is local and non-authoritative

Observed:
- `MarketplaceManager`/`UiMarketplace`/`SceneMarketplace` store listings in local memory.
- `ClientMarketplaceManager.OnMarketplaceUpdate` is effectively a stub.
- `MarketplaceUpdate` payload is too thin for listings, filters, expiration metadata, and history.

Implication:
- Client paths must be converted to consume authoritative server snapshots/deltas.

### 4. Trade and bind-policy hardening is already test-backed

Observed:
- `TradeManager` and security tests enforce strong transfer restrictions and rollback behavior.
- Existing tests around `BindPolicy` and mixed-stack transfer edge cases are extensive.

Implication:
- Direct P2P trade window work (`MKT-08`) should extend `TradeManager` flows, not duplicate transfer logic.

### 5. Data persistence for market domain is not present yet

Observed:
- `DbService` currently creates only `accounts` and `players` tables.
- No market tables, listing history, or search indexes are provisioned.

Implication:
- Phase 7 plans must include explicit market persistence model and migration path for listing lifecycle and history data.

## Recommended Planning Shape

Create 5 executable plans:

1. `07-01` Market foundation contracts and persistence schema: listing DTOs/opcodes, market service interface, listing storage model.
2. `07-02` Listing lifecycle operations: create listing, cancel listing, and automatic expiration-return flow.
3. `07-03` Market browsing and analytics: search/filter API plus min/avg/max price-history projection.
4. `07-04` Purchase settlement pipeline: atomic buyer/seller transfer with configurable tax and idempotency guards.
5. `07-05` Direct P2P trade window + client integration: extend trade request/confirm UX and wire server-authoritative market/trade updates into client state.

Wave recommendation:
- Wave 1: `07-01`
- Wave 2: `07-02`, `07-03` (parallel; both depend on contracts/storage foundation)
- Wave 3: `07-04` (depends on listing lifecycle + search/read model)
- Wave 4: `07-05` (depends on stable server contracts from prior plans)

## Verification Targets (Phase-level)

- Player can create listings with item, quantity, price, and expiration window.
- Player can search listings with filters (name/type/price/rarity) and receive deterministic results.
- Player can buy listings with atomic item/gold transfer and configurable tax deduction.
- Player can cancel own listings and receive item return safely.
- Listings expire automatically and return unsold items.
- Price history shows min/avg/max over recent completed transactions.
- Direct player-to-player trade flow supports dual confirmation and bind-policy-safe transfer.
- All market operations support idempotency and reject duplicate execution.

## Risks and Mitigations

- Risk: race conditions between simultaneous buyers on one listing.
  - Mitigation: atomic listing lock/update and idempotent purchase operation ids.

- Risk: duplication exploits during listing cancel/expiration return.
  - Mitigation: authoritative listing state machine + terminal-state checks + rollback tests.

- Risk: client displays stale or local-only listing state.
  - Mitigation: server snapshot + delta stream model; remove local-authoritative mutations.

- Risk: tax and payout drift from expected economy math.
  - Mitigation: centralize settlement math in one service and test with boundary/rounding cases.

## Conclusion

Phase 7 should be planned as an economy-critical subsystem with strict server authority. The codebase already has useful anti-abuse patterns; the key is to reuse them for listing lifecycle, settlement, and trade flows rather than inventing separate mechanics. A five-plan approach keeps risk bounded while still covering all MKT requirements comprehensively.
