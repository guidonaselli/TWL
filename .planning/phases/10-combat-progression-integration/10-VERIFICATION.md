# Phase 10: Combat Progression Integration - Verification

This document traces the Phase 10 requirements to their verified test coverage.

## Requirement Coverage

### CMB-01: Death Penalty
**Requirement:** Combat deaths remove exactly 1% of current-level EXP, floored at zero. Equipped items lose 1 durability per death.
**Verification:**
- `TWL.Tests.Server.Combat.CombatProgressionPhaseAcceptanceTests.Phase10_CMB01_DeathPenalty_AppliesCorrectly`: Verifies the 1% EXP calculation and durability deduction across item types (destructible vs indestructible).
- `TWL.Tests.Server.Combat.DeathPenaltyServiceTests`: Covers idempotent application and EXP floor validation.
- `TWL.Tests.Server.Equipment.DurabilitySystemTests`: Covers the disabled stats behavior when items break.

### CMB-02: Item Durability & Broken Gear
**Requirement:** If an item reaches 0 durability, it becomes `Broken` (stats disabled).
**Verification:**
- `TWL.Tests.Server.Combat.CombatProgressionPhaseAcceptanceTests.Phase10_CMB01_DeathPenalty_AppliesCorrectly`: Checks `IsBroken` flags on items reaching 0 durability.
- `TWL.Tests.Server.Equipment.DurabilitySystemTests.BrokenItems_DoNotContributeToStats`: Verifies that broken items no longer contribute to the player's calculated stats.

### CMB-03: Phase 10 Core Combat Behavior
**Requirement:** Idempotent combat resolution, status effect stability after death, and pet utility availability when player dies.
**Verification:**
- `TWL.Tests.Server.Combat.CombatFlowIntegrationTests`: Verifies that `PlayerDeath_DoesNotBreakPetTurn` works correctly, `StatusEffect_RemainsStable_AfterDeath`, and `PetUtility_RemainsAvailable_AfterOwnerDeathPenalty`.

### CMB-04: Quest Integration
**Requirement:** Combat kills (including by pets) progress quest objectives correctly.
**Verification:**
- `TWL.Tests.Server.QuestCombatIntegrationTests`: Covers `CombatKill_ShouldProgressQuest` and `CombatKill_ByPet_ShouldProgressOwnerQuest`.

### INST-01, INST-02, INST-03: Instance Locking
**Requirement:** Enforce a server-authoritative daily quota of 5 instance entries per character, resetting at midnight UTC.
**Verification:**
- `TWL.Tests.Server.Combat.CombatProgressionPhaseAcceptanceTests.Phase10_INST01_InstanceLimit_AppliesCorrectly`: Validates the entry restriction at the exact limit of 5 and the UTC reset behavior.
- `TWL.Tests.Server.Instances.InstanceRunLimitTests`: Covers edge cases like being under cap, hitting the exact cap, and incrementing counters on valid entry.

## Acceptance Criteria Met

- [x] All phase requirements CMB-01/02/03/04 and INST-01/02/03 are represented by executable acceptance tests.
- [x] Acceptance tests validate exact policy values (1% EXP loss, -1 durability, 5/day cap, UTC reset).
- [x] Phase-level verification artifacts clearly map requirement IDs to passing checks.
