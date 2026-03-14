# T05: 10-combat-progression-integration 05

**Slice:** S10 — **Milestone:** M001

## Description

Finalize Phase 10 with requirement-mapped acceptance verification and traceable evidence output.

Purpose: Ensure the phase is execution-ready and auditable against all roadmap requirements.
Output: Phase acceptance suite and verification artifact documenting requirement coverage.

## Must-Haves

- [ ] "All phase requirements CMB-01/02/03/04 and INST-01/02/03 are represented by executable acceptance tests"
- [ ] "Acceptance tests validate exact policy values (1% EXP loss, -1 durability, 5/day cap, UTC reset)"
- [ ] "Phase-level verification artifacts clearly map requirement IDs to passing checks"

## Files

- `TWL.Tests/Server/Combat/CombatProgressionPhaseAcceptanceTests.cs`
- `TWL.Tests/Server/Combat/CombatFlowIntegrationTests.cs`
- `TWL.Tests/Server/Instances/InstanceRunLimitTests.cs`
- `TWL.Tests/Server/QuestCombatIntegrationTests.cs`
- `.planning/phases/10-combat-progression-integration/10-VERIFICATION.md`
