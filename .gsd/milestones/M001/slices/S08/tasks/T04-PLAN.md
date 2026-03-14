# T04: 08-compound-system 04

**Slice:** S08 — **Milestone:** M001

## Description

Integrate non-refundable compound fee economics and anti-arbitrage safeguards.

Purpose: This delivers CMP-06 and hardens compound requests against replay and duplicate-charge inconsistencies.
Output: Economy fee extension, compound idempotency integration, session forwarding updates, and fee/idempotency tests.

## Must-Haves

- [ ] "Each compound attempt charges a non-refundable fee before outcome resolution"
- [ ] "Fee-charged compound operations are idempotent and reject duplicate replay from the same operation id"
- [ ] "Economy logs make compound fee charges auditable and distinguishable from other economy operations"

## Files

- `TWL.Server/Simulation/Managers/IEconomyService.cs`
- `TWL.Server/Simulation/Managers/EconomyManager.cs`
- `TWL.Server/Simulation/Managers/CompoundManager.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Shared/Domain/DTO/CompoundDTOs.cs`
- `TWL.Tests/Compound/CompoundFeeIdempotencyTests.cs`
