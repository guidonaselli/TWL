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

## Steps

1. **Policy Definition**: Update `PetDefinition` to include `IsQuestPet` and `EvolutionId` fields.
2. **Formula Implementation**: Implement the diminishing returns formula (10/8/5 schedule) in `PetService`.
3. **Rebirth Service Logic**: Extend `PetService` to handle `RebirthPetAsync` with eligibility checks (only Quest pets).
4. **Action Routing**: Update `ClientSession` and `PetActionRequest` to route rebirth/evolution requests to `PetService`.
5. **Domain Updates**: Ensure `ServerPet` correctly maps the result of a rebirth/evolution to its persistent state.
6. **Verification**: Create `PetRebirthPolicyTests` and `PetRebirthEvolutionTests` to verify all must-haves.

## Files

- `TWL.Server/Simulation/Networking/ServerPet.cs`
- `TWL.Server/Services/PetService.cs`
- `TWL.Shared/Services/IPetService.cs`
- `TWL.Shared/Domain/Characters/PetDefinition.cs`
- `TWL.Shared/Domain/Requests/PetActionRequest.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Tests/PetTests/PetRebirthPolicyTests.cs`
- `TWL.Tests/PetTests/PetRebirthEvolutionTests.cs`

## Observability Impact

- **Pet History Logs**: Rebirth attempts will be logged in the `PetHistory` component of `PlayerSaveData` (if applicable) or through general server audit logs.
- **Stat Delta Visibility**: Tests will verify that base stats increase by the expected 10/8/5 schedule, making formula drift visible.
- **Rejection Signals**: Rebirth failures for capturable pets will return a specific `PetActionResponse` with a clear "Not a quest pet" reason.
