# S10: Combat Progression Integration

**Goal:** Implement death-penalty EXP loss on player death (`CMB-01` partial) using server-authoritative combat event handling.
**Demo:** Implement death-penalty EXP loss on player death (`CMB-01` partial) using server-authoritative combat event handling.

## Must-Haves

- Combat deaths remove exactly 1% of current-level EXP, floored at zero.
- EXP penalties are triggered only from server-side combat death handling.
- Death penalty application is idempotent per death event and inspectable in tests.

## Verification

- `pwsh -File scripts/test-filter.ps1 -Names DeathPenaltyServiceTests -Tail 60`
- `pwsh -File scripts/build.ps1 -Config Debug`
- Failure-path check: `DeathPenaltyServiceTests` proves duplicate processing for the same death event does not apply a second penalty and leaves the EXP state unchanged after the first mutation.

## Observability / Diagnostics

- Runtime signals: death penalty application is expressed as a deterministic service result containing penalty amount, pre/post EXP, and whether the event was skipped as duplicate.
- Inspection surfaces: focused xUnit coverage in `TWL.Tests/Server/Combat/DeathPenaltyServiceTests.cs`; build/test output via `pwsh -File scripts/test-filter.ps1` and `pwsh -File scripts/build.ps1`.
- Failure visibility: duplicate-event handling and EXP floor behavior are asserted directly so regressions fail with concrete expected/actual EXP values.
- Redaction constraints: diagnostics must not include secrets, credentials, or unrelated player PII beyond test fixture identifiers.

## Tasks

- [x] **T01: 10-combat-progression-integration 01**
  - Implement death-penalty EXP loss on player death (`CMB-01` partial) using server-authoritative combat event handling.

Purpose: Ensure combat deaths produce deterministic, policy-compliant progression penalties.
Output: DeathPenaltyService, ServerCharacter EXP penalty mutation, and focused regression tests.
- [x] **T02: 10-combat-progression-integration 02**
  - Implement durability and broken-state mechanics for equipped items to complete the non-EXP portion of `CMB-01` and all of `CMB-02`.

Purpose: Attach item wear to death penalties and enforce stat-disable semantics for broken gear.
Output: Item durability model, server durability mutation logic, and durability regression tests.
- [ ] **T03: 10-combat-progression-integration 03**
  - Implement per-instance daily run limits with UTC reset and entry rejection to satisfy `INST-01`, `INST-02`, and `INST-03`.

Purpose: Prevent unlimited instance farming by enforcing server-authoritative daily quotas.
Output: Run-counter persistence model, quota-aware instance admission, and quota regression tests.
- [ ] **T04: 10-combat-progression-integration 04**
  - Integrate full Phase 10 combat flow (`CMB-04`) so death penalties, pet AI, skill/status behavior, and utility movement seams operate coherently.

Purpose: Remove integration gaps between independent subsystems now required to function together in production combat loops.
Output: Combat/session integration updates and cross-system integration tests.
- [ ] **T05: 10-combat-progression-integration 05**
  - Finalize Phase 10 with requirement-mapped acceptance verification and traceable evidence output.

Purpose: Ensure the phase is execution-ready and auditable against all roadmap requirements.
Output: Phase acceptance suite and verification artifact documenting requirement coverage.

## Files Likely Touched

- `TWL.Server/Simulation/Managers/CombatManager.cs`
- `TWL.Server/Simulation/Networking/ServerCharacter.cs`
- `TWL.Server/Services/Combat/DeathPenaltyService.cs`
- `TWL.Server/Simulation/ServerWorker.cs`
- `TWL.Tests/Server/Combat/DeathPenaltyServiceTests.cs`
- `TWL.Shared/Domain/Models/Item.cs`
- `TWL.Server/Persistence/PlayerSaveData.cs`
- `TWL.Server/Simulation/Networking/ServerCharacter.cs`
- `TWL.Server/Services/Combat/DeathPenaltyService.cs`
- `TWL.Tests/Server/Equipment/DurabilitySystemTests.cs`
- `TWL.Server/Persistence/PlayerSaveData.cs`
- `TWL.Server/Simulation/Networking/ServerCharacter.cs`
- `TWL.Server/Services/InstanceService.cs`
- `TWL.Server/Services/World/Actions/Handlers/EnterInstanceActionHandler.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Tests/Server/Instances/InstanceRunLimitTests.cs`
- `TWL.Server/Simulation/Managers/CombatManager.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Server/Services/PetService.cs`
- `TWL.Server/Services/Combat/DeathPenaltyService.cs`
- `TWL.Server/Simulation/Managers/StandardCombatResolver.cs`
- `TWL.Tests/Server/Combat/CombatFlowIntegrationTests.cs`
- `TWL.Tests/Server/QuestCombatIntegrationTests.cs`
- `TWL.Tests/Server/Combat/CombatProgressionPhaseAcceptanceTests.cs`
- `TWL.Tests/Server/Combat/CombatFlowIntegrationTests.cs`
- `TWL.Tests/Server/Instances/InstanceRunLimitTests.cs`
- `TWL.Tests/Server/QuestCombatIntegrationTests.cs`
- `.planning/phases/10-combat-progression-integration/10-VERIFICATION.md`
