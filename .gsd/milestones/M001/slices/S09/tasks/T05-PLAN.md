# T05: 09-pet-system-completion 05

**Slice:** S09 — **Milestone:** M001

## Description

Finalize Phase 9 with acceptance-grade verification across all PET requirements.

Purpose: Ensure pet systems are execution-ready and stable across AI, content, bonding, and riding flows.
Output: Phase-level acceptance suite and client/server integration tests with cross-system regression coverage.

## Must-Haves

- [x] "All Phase 9 requirements (PET-01, PET-02, PET-05, PET-06, PET-07) are encoded in repeatable acceptance tests"
- [x] "Pet AI, roster content, bonding behavior, and riding flow work together without cross-system regressions"
- [x] "Pet systems remain compatible with quest/combat integration pathways"

## Steps

1. **Phase-Level Acceptance Tests**: Create `TWL.Tests/PetTests/PetPhaseAcceptanceTests.cs` to verify end-to-end pet lifecycle (Capture -> Bonding -> AI -> Riding).
2. **Client-Server Integration**: Implement `TWL.Tests/PetTests/PetClientServerIntegrationTests.cs` to verify network protocol consistency for pet actions.
3. **Content Integration**: Implement `TWL.Tests/ContentIntegrationTests.cs` to ensure all pets in JSON have valid stats, skills, and assets.
4. **Cross-System Regression**: Create `TWL.Tests/Server/QuestCombatIntegrationTests.cs` to verify pets don't break quest flows or combat state machines.
5. **Final Verification**: Run all S09 tests and `scripts/verify.ps1` to ensure zero regressions across the codebase.

## Observability Impact

- **Test Coverage**: Clear pass/fail signals for all PET requirements (PET-01 to PET-07).
- **Network Trace**: Integration tests will exercise the real `ClientSession` / `PetService` boundary, validating packet serialization/deserialization.
- **Content Health**: JSON validation ensures no broken pet IDs or missing dependencies in the game content.

## Files

- `TWL.Tests/PetTests/PetPhaseAcceptanceTests.cs`
- `TWL.Tests/PetTests/PetClientServerIntegrationTests.cs`
- `TWL.Tests/ContentIntegrationTests.cs`
- `TWL.Tests/Server/QuestCombatIntegrationTests.cs`
