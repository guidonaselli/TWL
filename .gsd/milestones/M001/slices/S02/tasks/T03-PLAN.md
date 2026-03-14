# T03: 02-security-hardening 03

**Slice:** S02 — **Milestone:** M001

## Description

Establish shared idempotency and Serializable transaction foundations for valuable multi-party operations.

Purpose: This plan delivers SEC-03 and SEC-04 in a reusable way that Phase 7 market and Phase 5 guild-bank flows can consume without redesign.
Output: DbService transaction helper, shared idempotency validator, and manager integrations/tests proving duplicate-safe execution semantics.

## Must-Haves

- [ ] "A valuable operation retried with the same operation key executes only once"
- [ ] "Valuable multi-party operations run through a Serializable transaction boundary"
- [ ] "Trade and economy operation logs contain operation identity for audit and replay analysis"

## Files

- `TWL.Server/Persistence/Database/DbService.cs`
- `TWL.Server/Security/Idempotency/OperationRecord.cs`
- `TWL.Server/Security/Idempotency/IdempotencyValidator.cs`
- `TWL.Server/Simulation/Managers/EconomyManager.cs`
- `TWL.Server/Simulation/Managers/TradeManager.cs`
- `TWL.Server/Simulation/Managers/IEconomyService.cs`
- `TWL.Tests/Security/IdempotencyValidatorTests.cs`
- `TWL.Tests/Security/EconomyIdempotencyFlowTests.cs`
- `TWL.Tests/Security/SerializableTransactionPolicyTests.cs`
