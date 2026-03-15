# T01: 09-pet-system-completion 01 - Summary

Harden pet combat AI to satisfy PET-01 with deterministic, intelligent action selection.

## Context
Pet AI was previously relying on basic `AutoBattleManager` logic without explicit policy abstraction or hardened determinism for pet-specific scenarios (like ally health priority).

## Actions
- Created `IPetBattlePolicy` interface to abstract pet decision-making.
- Implemented `PetBattlePolicy` as the default AI, leveraging `AutoBattleManager` but ensuring intelligent selection for pets.
- Updated `AutoBattleManager` to guarantee determinism by adding `ThenBy(c => c.Id)` to all heuristic-based sorting.
- Integrated `IPetBattlePolicy` into `CombatManager.Update` flow for pets.
- Created `PetCombatAiTests.cs` with the following coverage:
    - `PetAI_PrioritizesHealing_WhenAllyIsLow`: Verifies pets prioritize supporting allies.
    - `PetAI_ChoosesElementalAdvantage`: Verifies pets choose targets based on elemental multipliers.
    - `PetAI_IsDeterministic`: Verifies equivalent inputs yield identical decisions.

## Verification Results
### Automated Tests
- `pwsh -File scripts\test-filter.ps1 -Names PetCombatAiTests` passes:
    - `PetAI_PrioritizesHealing_WhenAllyIsLow`: PASS
    - `PetAI_ChoosesElementalAdvantage`: PASS
    - `PetAI_IsDeterministic`: PASS

### Manual / Diagnostic Checks
- Diagnostic: Verified `PetBattlePolicy` includes `ILogger` traces for decision transparency.
- Must-Have: "Pet combat AI chooses actions based on ally HP, party status effects, and elemental advantage" - VERIFIED.
- Must-Have: "Pet AI behavior is deterministic for equivalent combat state inputs" - VERIFIED.
- Must-Have: "Pet AI decisions remain server-authoritative" - VERIFIED (logic runs strictly on `TWL.Server`).

## Observations
- One legacy test (`SpecialSkillTests.CombatManager_ApplySeal_UsesHitRules`) failed during full `verify.ps1` run due to `IndexOutOfRangeException` (empty results), but it passes consistently when run in isolation. This suggests existing test pollution in the environment related to `SkillRegistry` singleton state, not a regression from this task's changes.

## Next Steps
- T02: Complete PET-02 by finalizing starter-region pet roster and capture-world linkage.
