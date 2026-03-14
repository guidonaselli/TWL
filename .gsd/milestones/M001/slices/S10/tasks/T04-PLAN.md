# T04: 10-combat-progression-integration 04

**Slice:** S10 — **Milestone:** M001

## Description

Integrate full Phase 10 combat flow (`CMB-04`) so death penalties, pet AI, skill/status behavior, and utility movement seams operate coherently.

Purpose: Remove integration gaps between independent subsystems now required to function together in production combat loops.
Output: Combat/session integration updates and cross-system integration tests.

## Must-Haves

- [ ] "Combat flow applies death penalties without breaking pet AI turn execution"
- [ ] "Status effect processing remains stable while death/durability penalties are active"
- [ ] "Movement and pet utility seams stay coherent with combat progression state"

## Files

- `TWL.Server/Simulation/Managers/CombatManager.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Server/Services/PetService.cs`
- `TWL.Server/Services/Combat/DeathPenaltyService.cs`
- `TWL.Server/Simulation/Managers/StandardCombatResolver.cs`
- `TWL.Tests/Server/Combat/CombatFlowIntegrationTests.cs`
- `TWL.Tests/Server/QuestCombatIntegrationTests.cs`
