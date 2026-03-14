# S09: Pet System Completion

**Goal:** Harden pet combat AI to satisfy PET-01 with deterministic, intelligent action selection.
**Demo:** Harden pet combat AI to satisfy PET-01 with deterministic, intelligent action selection.

## Must-Haves


## Tasks

- [ ] **T01: 09-pet-system-completion 01**
  - Harden pet combat AI to satisfy PET-01 with deterministic, intelligent action selection.

Purpose: Ensure pet turns are strategically chosen based on battle context, not random behavior.
Output: Pet battle policy abstraction, AI integration updates, and pet-focused decision regression tests.
- [ ] **T02: 09-pet-system-completion 02**
  - Complete PET-02 by finalizing starter-region pet roster and capture-world linkage.

Purpose: Ensure pet content is not only numerous but also progression-ready and obtainable through gameplay.
Output: Updated pet and monster data, strengthened content validation, and roster coverage tests.
- [ ] **T03: 09-pet-system-completion 03**
  - Complete PET-05 and PET-06 by formalizing amity KO impact and bond-tier rewards.

Purpose: Make amity and bonding systems explicit, tunable, and test-verified rather than incidental side effects.
Output: Bond metadata model updates, runtime bond mechanics, and focused amity/bonding regression tests.
- [ ] **T04: 09-pet-system-completion 04**
  - Implement end-to-end riding system flow for PET-07.

Purpose: Close the current utility/riding gap by connecting request handling, mount-state effects, and client-visible movement behavior.
Output: Utility action routing, riding movement integration, and riding behavior regression tests.
- [ ] **T05: 09-pet-system-completion 05**
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
