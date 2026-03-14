# T01: 09-pet-system-completion 01

**Slice:** S09 — **Milestone:** M001

## Description

Harden pet combat AI to satisfy PET-01 with deterministic, intelligent action selection.

Purpose: Ensure pet turns are strategically chosen based on battle context, not random behavior.
Output: Pet battle policy abstraction, AI integration updates, and pet-focused decision regression tests.

## Must-Haves

- [ ] "Pet combat AI chooses actions based on ally HP, party status effects, and elemental advantage rather than random targeting"
- [ ] "Pet AI behavior is deterministic for equivalent combat state inputs"
- [ ] "Pet AI decisions remain server-authoritative within combat turn flow"

## Files

- `TWL.Server/Simulation/Managers/PetBattlePolicy.cs`
- `TWL.Server/Simulation/Managers/AutoBattleManager.cs`
- `TWL.Server/Simulation/Managers/CombatManager.cs`
- `TWL.Tests/PetTests/PetCombatAiTests.cs`
