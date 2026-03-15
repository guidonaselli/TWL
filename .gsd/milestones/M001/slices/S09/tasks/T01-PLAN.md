# T01: 09-pet-system-completion 01

**Slice:** S09 — **Milestone:** M001

## Description

Harden pet combat AI to satisfy PET-01 with deterministic, intelligent action selection.

Purpose: Ensure pet turns are strategically chosen based on battle context, not random behavior.
Output: Pet battle policy abstraction, AI integration updates, and pet-focused decision regression tests.

## Steps

1. Create `IPetBattlePolicy` interface and its default implementation `PetBattlePolicy.cs` in `TWL.Server/Simulation/Managers/`.
2. Implement logic in `PetBattlePolicy` to evaluate:
    - Ally HP (prioritize healing/buffing low-HP allies).
    - Status effects (cleanse or buff).
    - Elemental advantage (choose skill with highest damage multiplier against target).
3. Update `CombatManager.cs` or `AutoBattleManager.cs` to use `IPetBattlePolicy` for pet turns.
4. Ensure the decision process is deterministic by using a fixed priority list and removing random target selection where a better choice exists.
5. Create `PetCombatAiTests.cs` and implement tests for:
    - Healing priority when an ally is low.
    - Choosing elemental advantage attacks.
    - Determinism (same input state leads to same output action).
6. Verify all tests pass with `pwsh -File scripts\test-filter.ps1 -Names PetCombatAiTests`.

## Must-Haves

- [x] "Pet combat AI chooses actions based on ally HP, party status effects, and elemental advantage rather than random targeting"
- [x] "Pet AI behavior is deterministic for equivalent combat state inputs"
- [x] "Pet AI decisions remain server-authoritative within combat turn flow"

## Observability Impact

- **New Signals**: Added `ILogger` traces in `PetBattlePolicy` to show why a specific action was selected (e.g., "AI Decision: Cast Heal on [Ally] (HP: 20%)").
- **Inspection**: The `PetCombatAiTests` provide a direct way to inspect AI logic without running the full server.
- **Failure State**: If the policy cannot find a valid skill or target, it logs a warning and defaults to a safe action (Defend/Attack).

## Files

- `TWL.Server/Simulation/Managers/PetBattlePolicy.cs`
- `TWL.Server/Simulation/Managers/AutoBattleManager.cs`
- `TWL.Server/Simulation/Managers/CombatManager.cs`
- `TWL.Tests/PetTests/PetCombatAiTests.cs`
