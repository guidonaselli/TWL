# T05: 08-compound-system 05

**Slice:** S08 — **Milestone:** M001

## Description

Finalize compound client integration and phase-level verification coverage.

Purpose: This closes the loop so Phase 8 is executable end-to-end and validated against CMP-01..CMP-06.
Output: Client server-driven compound flow integration plus integration/acceptance tests.

## Must-Haves

- [ ] "Client compound UI/logic reflects server-authoritative outcomes instead of running local-only forge RNG"
- [ ] "Player sees updated inventory/equipment enhancement state after compound responses"
- [ ] "Phase-level compound requirements are covered by end-to-end acceptance tests"

## Files

- `TWL.Client/Presentation/Crafting/ForgeSystem.cs`
- `TWL.Client/Presentation/Crafting/EquipmentData.cs`
- `TWL.Client/Presentation/Managers/GameClientManager.cs`
- `TWL.Client/Presentation/Managers/ClientInventoryManager.cs`
- `TWL.Client/Presentation/Networking/NetworkClient.cs`
- `TWL.Tests/Compound/CompoundClientIntegrationTests.cs`
- `TWL.Tests/Compound/CompoundPhaseAcceptanceTests.cs`
