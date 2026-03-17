# T02: 10-combat-progression-integration 02

**Slice:** S10 — **Milestone:** M001

## Description

Implement durability and broken-state mechanics for equipped items to complete the non-EXP portion of `CMB-01` and all of `CMB-02`.

Purpose: Attach item wear to death penalties and enforce stat-disable semantics for broken gear.
Output: Item durability model, server durability mutation logic, and durability regression tests.

## Must-Haves

- [x] "Every equipped item loses 1 durability when player death penalty is applied"
- [x] "Items at 0 durability are treated as Broken and no longer contribute stat effects"
- [x] "Durability and broken-state data persist through save/load without corrupting legacy saves"

## Files

- `TWL.Shared/Domain/Models/Item.cs`
- `TWL.Server/Persistence/PlayerSaveData.cs`
- `TWL.Server/Simulation/Networking/ServerCharacter.cs`
- `TWL.Server/Services/Combat/DeathPenaltyService.cs`
- `TWL.Tests/Server/Equipment/DurabilitySystemTests.cs`

6. Enrichment: Explicitly handle serialization differences between new durability format and legacy items without durability fields in `PlayerSaveData`. Ensure test coverage explicitly checks legacy migration behavior.
