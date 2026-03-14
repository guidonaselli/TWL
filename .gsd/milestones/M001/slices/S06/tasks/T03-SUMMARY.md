# T03: 06-rebirth-system 03 — Summary

Implemented pet rebirth policy and evolution logic with multi-generation diminishing returns and explicit server action routing.

## Key Changes

### Policy & Eligibility
- **Pet Definition**: Added `IsQuestPet` and `EvolutionId` to `PetDefinition` to differentiate between quest-unique pets (rebirth-eligible) and wild pets (non-eligible).
- **Enforcement**: Updated `PetService` and `ServerPet` to reject rebirth and evolution requests for capturable pets, ensuring progression is gated to story-critical companions.

### Progression & Formulas
- **Diminishing Returns**: Implemented a "10/8/5 schedule" for pet rebirth.
    - 1st Rebirth: +10% base stats.
    - 2nd Rebirth: +18% total (+8% additive).
    - 3rd Rebirth: +23% total (+5% additive).
- **Evolution**: Added `TryEvolve` logic to `PetService` and `ServerPet`, allowing quest pets to transition to new definitions (forms) upon reaching level 100.

### Network & Routing
- **Opcode Handling**: Extended `PetActionType` with `Rebirth` and `Evolve`.
- **Client Session**: Updated `HandlePetActionAsync` to route these new actions to the `PetService`.
- **Persistence**: Replaced the binary `HasRebirthed` flag with a `RebirthCount` integer in `ServerPetData` and `PlayerSaveData` to support multiple generations.

## Verification Results

### Must-Haves
- [x] **Eligibility**: Verified that Quest pets can rebirth while Wild pets are rejected (`PetRebirthPolicyTests`).
- [x] **Schedule**: Verified that stats increase by the 10/8/5 schedule and cap after 3 rebirths (`PetRebirthPolicyTests`).
- [x] **Evolution**: Verified that pets correctly transition definitions at level 100 (`PetRebirthEvolutionTests`).

### Automated Tests
- `PetRebirthPolicyTests`: 3/3 passed.
- `PetRebirthEvolutionTests`: 3/3 passed.
- `PetSystemTests` & `PetSystemExpansionTests`: Updated to match new policy and passed.
- Total `PetTests` filter: 27/27 passed.

## Decisions
- **D008**: Used additive diminishing returns (10/8/5) for multi-generation pet rebirth to maintain predictable growth and prevent stat runaway.

## Observability
- **Log Signals**: Added warning logs for failed rebirth/evolution attempts (e.g., "Pet is not a quest pet").
- **Auditability**: `RebirthCount` in save data provides a clear record of pet progression for troubleshooting.
