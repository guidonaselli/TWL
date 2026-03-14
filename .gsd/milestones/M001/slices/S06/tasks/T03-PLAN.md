# T03: 06-rebirth-system 03

**Slice:** S06 — **Milestone:** M001

## Description

Complete pet rebirth policy for Phase 6, including eligibility differentiation and diminishing rebirth progression.

Purpose: This delivers PET-03 and PET-04 using the same safety principles established for character rebirth.
Output: Upgraded pet rebirth domain/service logic + action routing + deterministic pet rebirth/evolution tests.

## Must-Haves

- [ ] "Quest pets can rebirth/evolve while capturable pets are rejected from rebirth"
- [ ] "Pet rebirth applies generation-based diminishing stat bonuses using 10/8/5 schedule"
- [ ] "Pet rebirth and evolution outcomes are accessible through explicit server action routing"

## Files

- `TWL.Server/Simulation/Networking/ServerPet.cs`
- `TWL.Server/Services/PetService.cs`
- `TWL.Shared/Services/IPetService.cs`
- `TWL.Shared/Domain/Characters/PetDefinition.cs`
- `TWL.Shared/Domain/Requests/PetActionRequest.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Tests/PetTests/PetRebirthPolicyTests.cs`
- `TWL.Tests/PetTests/PetRebirthEvolutionTests.cs`
