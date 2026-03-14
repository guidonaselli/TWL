# S06: Rebirth System

**Goal:** Implement character rebirth transactional foundation, including formula policy, atomic state mutation, persistence history, and network entry points.
**Demo:** Implement character rebirth transactional foundation, including formula policy, atomic state mutation, persistence history, and network entry points.

## Must-Haves


## Observability / Diagnostics

- **Rebirth Transaction Audit**: All rebirth attempts (success and failure) must be logged with `RebirthAuditLog` entries in `PlayerSaveData`.
- **Atomic Failure Visibility**: Verification tests must simulate persistence failures to ensure partial state is not committed.
- **Formula Inspection**: `RebirthManager` should expose static formula calculation for unit testing without full character context.

## Verification

- [x] **Character Rebirth Transactional Integrity**: Rerun `CharacterRebirthTransactionTests` to verify atomic state mutation and history logging.
- [x] **Rebirth Requirement Enforcement**: Rerun `CharacterRebirthRequirementTests` to verify level and item checks.
- [x] **Pet Rebirth Policy Verification**: Rerun `PetRebirthPolicyTests` for pet-specific rebirth rules.
- [x] **Diagnostic Surface Check**: Verify `RebirthAuditLog` contains expected entries after a rebirth attempt.

## Tasks

- [x] **T01: 06-rebirth-system 01**
  - Implement character rebirth transactional foundation, including formula policy, atomic state mutation, persistence history, and network entry points.

Purpose: This delivers REB-01, REB-05, REB-06, and REB-07 as a secure base for all remaining rebirth functionality.
Output: Rebirth service + DTO contracts + opcode/session wiring + transaction and formula regression tests.
- [x] **T02: 06-rebirth-system 02**
  - Implement rebirth eligibility enforcement and prestige visibility while preserving character build continuity after rebirth.

Purpose: This delivers REB-02, REB-03, and REB-04 using the transactional rebirth foundation from Plan 06-01.
Output: Rebirth requirement checks + payload/UI propagation for prestige display + retention and display regression tests.
- [x] **T03: 06-rebirth-system 03**
  - Complete pet rebirth policy for Phase 6, including eligibility differentiation and diminishing rebirth progression.

Purpose: This delivers PET-03 and PET-04 using the same safety principles established for character rebirth.
Output: Upgraded pet rebirth domain/service logic + action routing + deterministic pet rebirth/evolution tests.
- [x] **T04: 06-rebirth-system 04**
  - Consolidate Phase 6 with end-to-end verification, rollback safety checks, and cross-system regressions.

Purpose: This closes remaining integration risk so execution can proceed with high confidence and minimal rework.
Output: End-to-end and failure-path test suites proving character and pet rebirth correctness across networking, persistence, and quest gating.

## Files Likely Touched

- `TWL.Server/Simulation/Managers/IRebirthService.cs`
- `TWL.Server/Simulation/Managers/RebirthManager.cs`
- `TWL.Shared/Domain/DTO/RebirthDTOs.cs`
- `TWL.Server/Persistence/PlayerSaveData.cs`
- `TWL.Shared/Net/Network/Opcode.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Server/Simulation/Program.cs`
- `TWL.Tests/Rebirth/CharacterRebirthTransactionTests.cs`
- `TWL.Server/Simulation/Managers/RebirthManager.cs`
- `TWL.Server/Simulation/Networking/ServerCharacter.cs`
- `TWL.Server/Simulation/Networking/Components/PlayerQuestComponent.cs`
- `TWL.Shared/Net/Payloads/LoginResponseDto.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Client/Presentation/Managers/PlayerCharacterData.cs`
- `TWL.Client/Presentation/UI/UiGameplay.cs`
- `TWL.Tests/Rebirth/CharacterRebirthRequirementTests.cs`
- `TWL.Tests/Rebirth/RebirthPrestigeDisplayTests.cs`
- `TWL.Server/Simulation/Networking/ServerPet.cs`
- `TWL.Server/Services/PetService.cs`
- `TWL.Shared/Services/IPetService.cs`
- `TWL.Shared/Domain/Characters/PetDefinition.cs`
- `TWL.Shared/Domain/Requests/PetActionRequest.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Tests/PetTests/PetRebirthPolicyTests.cs`
- `TWL.Tests/PetTests/PetRebirthEvolutionTests.cs`
- `TWL.Tests/Rebirth/RebirthEndToEndTests.cs`
- `TWL.Tests/Rebirth/RebirthRollbackAuditTests.cs`
- `TWL.Tests/PetTests/PetRebirthIntegrationTests.cs`
- `TWL.Tests/Quests/QuestGatingTests.cs`
- `TWL.Server/Simulation/Managers/RebirthManager.cs`
- `TWL.Server/Services/PetService.cs`
