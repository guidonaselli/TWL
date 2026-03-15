# S09: Pet System Completion

**Goal:** Harden pet combat AI to satisfy PET-01 with deterministic, intelligent action selection.
**Demo:** Harden pet combat AI to satisfy PET-01 with deterministic, intelligent action selection.

## Verification

- [x] `pwsh -File scripts\verify.ps1` passes (Build & All Tests)
- [x] `pwsh -File scripts\test-filter.ps1 -Names PetCombatAiTests` passes
- [x] `pwsh -File scripts\test-filter.ps1 -Names PetRosterCoverageTests` passes
- [x] `pwsh -File scripts\test-filter.ps1 -Names PetBondingMechanicsTests` passes
- [x] `pwsh -File scripts\test-filter.ps1 -Names PetRidingSystemTests` passes
- [x] `pwsh -File scripts\test-filter.ps1 -Names PetPhaseAcceptanceTests` passes
- [x] Diagnostic: Verify `PetBattlePolicy` logs its decision-making process when log level is set to Debug/Trace.

## Observability / Diagnostics
- **Runtime Signals**: Logs from `PetBattlePolicy` indicating the chosen action and the primary factor (e.g., "Healing ally due to low HP", "Attacking elemental weakness").
- **Inspection Surfaces**: Unit tests in `PetCombatAiTests` use a mockable `IPetBattlePolicy` to verify decisions.
- **Failure Visibility**: If AI selection fails, a default "Defend" or "Basic Attack" is chosen and a warning is logged.

## Must-Haves


## Tasks

- [x] **T01: 09-pet-system-completion 01**
  - Harden pet combat AI to satisfy PET-01 with deterministic, intelligent action selection.

Purpose: Ensure pet turns are strategically chosen based on battle context, not random behavior.
Output: Pet battle policy abstraction, AI integration updates, and pet-focused decision regression tests.
- [x] **T02: 09-pet-system-completion 02**
  - Complete PET-02 by finalizing starter-region pet roster and capture-world linkage.

Purpose: Ensure pet content is not only numerous but also progression-ready and obtainable through gameplay.
Output: Updated pet and monster data, strengthened content validation, and roster coverage tests.
- [x] **T03: 09-pet-system-completion 03**
  - Complete PET-05 and PET-06 by formalizing amity KO impact and bond-tier rewards.

Purpose: Make amity and bonding systems explicit, tunable, and test-verified rather than incidental side effects.
Output: Bond metadata model updates, runtime bond mechanics, and focused amity/bonding regression tests.
- [x] **T04: 09-pet-system-completion 04**
  - Implement end-to-end riding system flow for PET-07.

Purpose: Close the current utility/riding gap by connecting request handling, mount-state effects, and client-visible movement behavior.
Output: Utility action routing, riding movement integration, and riding behavior regression tests.
- [x] **T05: 09-pet-system-completion 05**
  - Finalize Phase 9 with acceptance-grade verification across all PET requirements.

Purpose: Ensure pet systems are execution-ready and stable across AI, content, bonding, and riding flows.
Output: Phase-level acceptance suite and client/server integration tests with cross-system regression coverage.

## Files Likely Touched

- `TWL.Server/Simulation/Managers/PetBattlePolicy.cs`
- `TWL.Server/Simulation/Managers/AutoBattleManager.cs`
- `TWL.Server/Simulation/Managers/CombatManager.cs`
- `TWL.Tests/PetTests/PetCombatAiTests.cs`
- `Content/Data/pets.json`
- `Content/Data/monsters.json`
- `TWL.Tests/ContentIntegrationTests.cs`
- `TWL.Tests/ContentValidationTests.cs`
- `TWL.Tests/PetTests/PetRosterCoverageTests.cs`
- `TWL.Shared/Domain/Characters/PetDefinition.cs`
- `TWL.Server/Simulation/Networking/ServerPet.cs`
- `TWL.Server/Services/PetService.cs`
- `TWL.Tests/PetTests/PetBondingMechanicsTests.cs`
- `TWL.Tests/PetTests/PetAmityKoTests.cs`
- `TWL.Shared/Domain/Requests/PetActionRequest.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Server/Services/PetService.cs`
- `TWL.Server/Simulation/Networking/ServerCharacter.cs`
- `TWL.Client/Presentation/Managers/GameClientManager.cs`
- `TWL.Client/Presentation/Scenes/SceneGameplay.cs`
- `TWL.Tests/PetTests/PetRidingSystemTests.cs`
- `TWL.Tests/PetTests/PetActionUtilityHandlerTests.cs`
- `TWL.Tests/PetTests/PetPhaseAcceptanceTests.cs`
- `TWL.Tests/PetTests/PetClientServerIntegrationTests.cs`
- `TWL.Tests/ContentIntegrationTests.cs`
- `TWL.Tests/Server/QuestCombatIntegrationTests.cs`
