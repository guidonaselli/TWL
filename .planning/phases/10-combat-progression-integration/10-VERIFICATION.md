# Phase 10: Combat Progression Integration - Verification

This document traces the Phase 10 requirements to their executable acceptance verification.

## Requirement Mapping

| ID      | Description                               | Status     | Verification Test (xUnit)                                     |
|---------|-------------------------------------------|------------|---------------------------------------------------------------|
| CMB-01  | Death-penalty EXP loss (1%)               | `VERIFIED` | `CombatProgressionPhaseAcceptanceTests.CMB_01_PlayerDeath_AppliesPenalty` |
| CMB-02  | Item durability loss on death (-1)        | `VERIFIED` | `CombatProgressionPhaseAcceptanceTests.CMB_01_PlayerDeath_AppliesPenalty` |
| CMB-03  | Combat flow coherence & pet AI resilience | `VERIFIED` | `CombatFlowIntegrationTests.CombatFlow_AppliesDeathPenalties_WithoutBreakingPetAiTurnExecution` |
| CMB-04  | Status effects tick stable on dead entity | `VERIFIED` | `CombatFlowIntegrationTests.StatusEffectProcessing_RemainsStable_WhileDeathPenaltiesAreActive` |
| INST-01 | Instance daily limit cap (5 runs)         | `VERIFIED` | `CombatProgressionPhaseAcceptanceTests.INST_01_02_03_InstanceLimits_Enforced` |
| INST-02 | Instance entry rejection at cap           | `VERIFIED` | `CombatProgressionPhaseAcceptanceTests.INST_01_02_03_InstanceLimits_Enforced` |
| INST-03 | UTC midnight reset of instance runs       | `VERIFIED` | `CombatProgressionPhaseAcceptanceTests.INST_01_02_03_InstanceLimits_Enforced` |

## Notes

- **Death Penalty (CMB-01/02):** Validated in `TWL.Tests.Server.Combat.CombatProgressionPhaseAcceptanceTests`. Checks for exact policy values: 1% EXP deduction, -1 durability loss.
- **Instance Limits (INST-01/02/03):** Validated in `TWL.Tests.Server.Combat.CombatProgressionPhaseAcceptanceTests`. Asserts exact cap (5) and UTC date boundary reset.
- **Combat Flow (CMB-03/04):** Validated in `CombatFlowIntegrationTests`. Ensured that dead entities don't break turn loops, pet AI, or status effect ticking.
