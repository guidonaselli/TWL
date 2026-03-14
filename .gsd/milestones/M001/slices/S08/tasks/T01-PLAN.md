# T01: 08-compound-system 01

**Slice:** S08 — **Milestone:** M001

## Description

Create Phase 8 compound foundation contracts and persistence metadata.

Purpose: This establishes the server-authoritative base required for all compound NPC, formula, and fee mechanics.
Output: Compound service interface/manager, shared DTOs, enhancement item fields, DI wiring, and contract coverage tests.

## Must-Haves

- [ ] "Compound operations execute through a dedicated server-authoritative service, not client-local forge logic"
- [ ] "Equipment enhancement metadata persists across save/load without losing level or bonus information"
- [ ] "Compound service is registered in runtime DI and reachable by server handlers"

## Files

- `TWL.Server/Simulation/Managers/ICompoundService.cs`
- `TWL.Server/Simulation/Managers/CompoundManager.cs`
- `TWL.Shared/Domain/DTO/CompoundDTOs.cs`
- `TWL.Shared/Domain/Models/Item.cs`
- `TWL.Server/Simulation/Networking/ServerCharacter.cs`
- `TWL.Server/Simulation/Program.cs`
- `TWL.Tests/Compound/CompoundContractTests.cs`
