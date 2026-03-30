# S10: Combat & Progression Integration (Summary)

**Milestone:** M001

## Goal Achieved
Implemented the Phase 10 Combat and Progression Integration, including death penalties (EXP and Durability), broken gear mechanics, daily instance limits, and pet combat AI turn integration.

## Key Deliverables
- Fully functioning `DeathPenaltyService` dropping 1% EXP and -1 Durability.
- Enforcement of `Broken` status disabling gear stats.
- `InstanceService` locking entry to 5 runs per UTC day.
- Robust cross-subsystem acceptance tests proving compliance with `CMB-01`, `CMB-02`, `CMB-03`, `CMB-04`, and `INST-**` rules.

## Artifacts
- Source code in `TWL.Server/Services/Combat/DeathPenaltyService.cs`
- Source code in `TWL.Server/Services/InstanceService.cs`
- Verifications logged in `.planning/phases/10-combat-progression-integration/10-VERIFICATION.md`
