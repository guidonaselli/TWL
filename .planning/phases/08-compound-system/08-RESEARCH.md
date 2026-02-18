# Phase 8: Compound System - Research

**Researched:** 2026-02-17  
**Domain:** Server-authoritative equipment enhancement (compound/forge), success/failure mechanics, and economy sink enforcement  
**Confidence:** High

## Summary

Phase 8 currently has only partial groundwork: client-side forge prototypes (`ForgeSystem`, `EquipmentData`) and server-side quest hooks (`OnCompound` / `OnForge`) but no server-authoritative compound subsystem. There is no compound manager, no compound-specific network opcode/DTO contract, and no economic fee pipeline for enhancement attempts.

The strongest path is to implement compound as an explicit server domain that reuses proven reliability patterns from earlier systems:
- operation id idempotency and ledger-style accounting patterns from `EconomyManager`
- inventory and bind-policy protections in `ServerCharacter` and `TradeManager`
- interaction entry points through `InteractionManager` + `InteractHandler`
- quest progression hooks already listening for compound/forge outcomes

Enhancement outcomes should be deterministic and auditable:
- success chance computed from equipment enhancement state and material bonuses
- failure consumes materials and fee but preserves base item
- success applies permanent enhancement metadata to equipment

## Current State Findings

### 1. Compound is not server-authoritative yet

Observed:
- `TWL.Client/Presentation/Crafting/ForgeSystem.cs` performs local RNG and mutates local equipment data.
- No server compound service or compound operation path exists.

Implication:
- Compound must move to server-authoritative execution to prevent client-side tampering.

### 2. Core quest hooks already support compound/forge progress

Observed:
- `ServerCharacter` exposes `OnCompound` and `OnForge` events and notifier methods.
- `PlayerQuestComponent` already handles `Compound` and `Forge` objective updates.

Implication:
- Compound manager should emit these events after successful domain operations instead of introducing parallel quest pathways.

### 3. Network contract surface is missing

Observed:
- `Opcode` has no compound-specific request/response operations.
- `ClientSession` has no handlers for compound preview/apply/fee responses.

Implication:
- New compound DTOs and opcode handlers are required before client integration can be authoritative.

### 4. Item model can be extended but currently lacks enhancement metadata

Observed:
- Shared `Item` includes only generic fields and `ForgeSuccessRateBonus`.
- Inventory/equipment cloning in `ServerCharacter` is explicit; new item fields must be propagated through copy/save paths.

Implication:
- Enhancement level and cumulative bonus metadata should be added to `Item`, with copy/persistence flow validated.

### 5. Economy anti-abuse patterns already exist and should be reused

Observed:
- `EconomyManager` supports idempotency keys, rate limiting, and secure ledger logging.

Implication:
- Compound fees should be charged through economy service extension with non-refundable semantics and operation-id idempotency.

## Recommended Planning Shape

Create 5 executable plans:

1. `08-01` Compound foundation contracts and persistence metadata.
2. `08-02` Compound NPC access and inventory selection validation flow.
3. `08-03` Success-rate calculation and success/failure outcome engine.
4. `08-04` Non-refundable compound fee and idempotent anti-arbitrage safeguards.
5. `08-05` Client integration and end-to-end compound regression coverage.

Wave recommendation:
- Wave 1: `08-01`
- Wave 2: `08-02`, `08-03` (parallel, both depend on foundation)
- Wave 3: `08-04` (depends on access + outcome engine)
- Wave 4: `08-05` (depends on stable fee-aware server flow)

## Verification Targets (Phase-level)

- Player can access a configured compound NPC and open compound flow.
- Player can submit base equipment plus enhancement materials from inventory.
- Server calculates success rate from enhancement level + materials and returns deterministic result metadata.
- Success applies permanent enhancement bonuses to equipment.
- Failure consumes materials but never destroys base equipment.
- Compound fee is non-refundable and charged regardless of success/failure.
- Repeated requests with same operation id are idempotent and do not double-charge or double-apply.

## Risks and Mitigations

- Risk: enhancement replay or duplicate execution under packet retries.
  - Mitigation: require operation id and idempotent transaction handling.

- Risk: fee bypass creating arbitrage with market/listing economics.
  - Mitigation: enforce pre-roll non-refundable charge in server economy path.

- Risk: stale client assumptions from local forge logic.
  - Mitigation: route all client compound state through server response payloads.

- Risk: enhancement metadata not persisted consistently.
  - Mitigation: update all item clone/save/load paths and add regression tests for persistence round trips.

## Conclusion

Phase 8 should be executed as an economy-sensitive, server-authoritative subsystem. Most required primitives already exist (economy idempotency, quest hooks, interaction dispatch). The main work is introducing compound-specific contracts, authoritative outcome processing, fee enforcement, and client rewiring to consume server decisions.
