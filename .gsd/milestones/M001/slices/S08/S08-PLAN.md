# S08: Compound System

**Goal:** Create Phase 8 compound foundation contracts and persistence metadata.
**Demo:** Create Phase 8 compound foundation contracts and persistence metadata.

## Must-Haves


## Tasks

- [ ] **T01: 08-compound-system 01**
  - Create Phase 8 compound foundation contracts and persistence metadata.

Purpose: This establishes the server-authoritative base required for all compound NPC, formula, and fee mechanics.
Output: Compound service interface/manager, shared DTOs, enhancement item fields, DI wiring, and contract coverage tests.
- [ ] **T02: 08-compound-system 02**
  - Implement compound NPC access and inventory selection validation pipeline.

Purpose: This delivers CMP-01 and CMP-02 by exposing a server-authoritative entry path for compound requests.
Output: Compound opcodes, session handlers, interaction definitions, and NPC-access/selection validation tests.
- [ ] **T03: 08-compound-system 03**
  - Implement compound success-rate and outcome engine.

Purpose: This delivers CMP-03, CMP-04, and CMP-05 with deterministic server-side compound resolution.
Output: Rate policy component, compound outcome application in manager, and outcome-focused regression tests.
- [ ] **T04: 08-compound-system 04**
  - Integrate non-refundable compound fee economics and anti-arbitrage safeguards.

Purpose: This delivers CMP-06 and hardens compound requests against replay and duplicate-charge inconsistencies.
Output: Economy fee extension, compound idempotency integration, session forwarding updates, and fee/idempotency tests.
- [ ] **T05: 08-compound-system 05**
  - Finalize compound client integration and phase-level verification coverage.

Purpose: This closes the loop so Phase 8 is executable end-to-end and validated against CMP-01..CMP-06.
Output: Client server-driven compound flow integration plus integration/acceptance tests.

## Files Likely Touched

- `TWL.Server/Simulation/Managers/ICompoundService.cs`
- `TWL.Server/Simulation/Managers/CompoundManager.cs`
- `TWL.Shared/Domain/DTO/CompoundDTOs.cs`
- `TWL.Shared/Domain/Models/Item.cs`
- `TWL.Server/Simulation/Networking/ServerCharacter.cs`
- `TWL.Server/Simulation/Program.cs`
- `TWL.Tests/Compound/CompoundContractTests.cs`
- `TWL.Shared/Net/Network/Opcode.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Shared/Domain/Interactions/InteractionDefinition.cs`
- `TWL.Server/Simulation/Managers/InteractionManager.cs`
- `Content/Data/interactions.json`
- `TWL.Tests/Compound/CompoundNpcAccessTests.cs`
- `TWL.Server/Simulation/Managers/CompoundRatePolicy.cs`
- `TWL.Server/Simulation/Managers/CompoundManager.cs`
- `TWL.Server/Simulation/Networking/ServerCharacter.cs`
- `TWL.Tests/Compound/CompoundOutcomeTests.cs`
- `TWL.Server/Simulation/Managers/IEconomyService.cs`
- `TWL.Server/Simulation/Managers/EconomyManager.cs`
- `TWL.Server/Simulation/Managers/CompoundManager.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Shared/Domain/DTO/CompoundDTOs.cs`
- `TWL.Tests/Compound/CompoundFeeIdempotencyTests.cs`
- `TWL.Client/Presentation/Crafting/ForgeSystem.cs`
- `TWL.Client/Presentation/Crafting/EquipmentData.cs`
- `TWL.Client/Presentation/Managers/GameClientManager.cs`
- `TWL.Client/Presentation/Managers/ClientInventoryManager.cs`
- `TWL.Client/Presentation/Networking/NetworkClient.cs`
- `TWL.Tests/Compound/CompoundClientIntegrationTests.cs`
- `TWL.Tests/Compound/CompoundPhaseAcceptanceTests.cs`
