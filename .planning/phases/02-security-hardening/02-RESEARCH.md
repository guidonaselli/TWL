# Phase 2: Security Hardening - Research

**Researched:** 2026-02-17
**Domain:** Server-side anti-cheat, anti-replay, and transactional safety
**Confidence:** High

## Summary

Phase 2 should harden three attack surfaces currently visible in the codebase:

1. Movement trust boundary (speed-hack / teleport abuse)
2. Network replay boundary (duplicate or stale packet acceptance)
3. Multi-party transaction boundary (race conditions and duplicate processing)

The codebase already has useful primitives we can extend:
- Per-opcode token bucket rate limiting (`RateLimiter`)
- Security audit logging (`SecurityLogger`)
- Idempotency pattern in premium economy operations (`EconomyManager` `operationId` flow)

## Current State Findings

### SEC-01 Movement validation

Observed in `TWL.Server/Simulation/Networking/ClientSession.cs`:
- `HandleMoveAsync` deserializes `{dx,dy}` and directly applies deltas to `Character.X` and `Character.Y`
- No max-distance-per-tick check
- No movement speed policy tied to server tick timing
- No anomaly logging for oversized movement vectors

Observed in `TWL.Shared/Domain/DTO/MoveDTO.cs`:
- Payload contains only `dx` and `dy`

Implication:
- Current logic accepts arbitrary delta magnitudes, which is enough to implement teleport-like movement by packet crafting.

### SEC-02 Replay protection (nonce + timestamp)

Observed in `TWL.Shared/Net/Network/NetMessage.cs` and receive path in `ClientSession`:
- `NetMessage` has `Op` + `JsonPayload` only
- Receive loop validates rate limits but does not validate message freshness/uniqueness
- No nonce, sequence, or timestamp validation window

Implication:
- Captured packets can be replayed if they pass rate limits and payload parsing.

### SEC-03 Serializable isolation for market-style transactions

Observed in `TWL.Server/Persistence/Database/DbService.cs`:
- No reusable transaction executor abstraction yet
- Database operations are basic and not organized around isolation-aware helpers

Observed in `TWL.Server/Simulation/Managers/EconomyManager.cs`:
- In-memory idempotency and ledger chain exist, but no DB-backed Serializable transaction boundary

Implication:
- Current architecture lacks a shared transaction runner that can guarantee serializable execution for future market/guild-bank operations.

### SEC-04 Idempotency keys for multi-party operations

Observed:
- `EconomyManager` already accepts `operationId` for selected operations
- `TradeManager` currently has no operation-id based idempotency path
- No shared interface/service for idempotency policies across valuable operations

Implication:
- Idempotency is partial and implementation-specific; this should be generalized to avoid drift when market/guild systems are added.

## Recommended Security Direction

### 1. Introduce a packet trust envelope

- Extend network message metadata with `nonce` and `timestampUtc` (and optionally monotonic sequence)
- Validate in a reusable replay guard before opcode dispatch
- Enforce 30-second freshness window and duplicate nonce rejection per session/user

### 2. Introduce deterministic movement validator

- Add a server-side movement policy based on max distance per tick and optional map boundary checks
- Reject oversize moves and log with structured security events
- Keep combat movement lock behavior unchanged

### 3. Introduce transaction + idempotency platform for valuable operations

- Add a reusable DB transaction runner with explicit `IsolationLevel.Serializable`
- Add shared idempotency contract for operation keys
- Refactor economy/trade paths to use shared primitives, not bespoke logic
- Prepare interfaces that future market/guild-bank services can plug into without redesign

## Phase Planning Implications

To keep execution quality high and avoid cross-file collisions, Phase 2 should be split into focused plans:

- `02-01`: Replay protection envelope and request validation path
- `02-02`: Movement anti-cheat validation and telemetry
- `02-03`: Serializable + idempotency foundation for valuable operations

Parallelization recommendation:
- Wave 1: `02-01` and `02-03` (different subsystems)
- Wave 2: `02-02` (depends on packet envelope hardening in `02-01`)

## Verification Targets (Phase-level)

- Replay attempt with same nonce is rejected and logged
- Packet older than 30 seconds is rejected
- Oversized movement packet is rejected and player position unchanged
- Valuable operation retried with same idempotency key returns idempotent result
- Serializable transaction runner is used for market-ready transaction boundaries

## Risks and Mitigations

- Risk: Protocol changes break older clients
  - Mitigation: Introduce compatibility gate and staged strict-mode switch
- Risk: False positives in movement validation due to tick jitter
  - Mitigation: Include jitter tolerance and tune via tests/metrics
- Risk: Over-scoping Phase 2 into full market implementation
  - Mitigation: Build reusable security primitives now; full market logic stays in Phase 7

## Conclusion

Phase 2 should deliver hardening primitives and integration points, not feature-complete market systems. This keeps scope aligned with SEC-01..SEC-04 while de-risking later phases.
