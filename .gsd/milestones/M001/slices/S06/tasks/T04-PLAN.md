# T04: 06-rebirth-system 04

**Slice:** S06 — **Milestone:** M001

## Description

Consolidate Phase 6 with end-to-end verification, rollback safety checks, and cross-system regressions.

Purpose: This closes remaining integration risk so execution can proceed with high confidence and minimal rework.
Output: End-to-end and failure-path test suites proving character and pet rebirth correctness across networking, persistence, and quest gating.

## Must-Haves

- [ ] "Character and pet rebirth flows both satisfy phase requirements under realistic end-to-end scenarios"
- [ ] "Failed rebirth attempts leave no partial mutation and still produce auditable traces"
- [ ] "Quest and progression systems consume rebirth state consistently after implementation"

## Files

- `TWL.Tests/Rebirth/RebirthEndToEndTests.cs`
- `TWL.Tests/Rebirth/RebirthRollbackAuditTests.cs`
- `TWL.Tests/PetTests/PetRebirthIntegrationTests.cs`
- `TWL.Tests/Quests/QuestGatingTests.cs`
- `TWL.Server/Simulation/Managers/RebirthManager.cs`
- `TWL.Server/Services/PetService.cs`
