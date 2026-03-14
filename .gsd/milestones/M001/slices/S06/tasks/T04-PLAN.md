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

## Steps

1. [ ] **E2E Character Rebirth**: Implement `TWL.Tests/Rebirth/RebirthEndToEndTests.cs` to verify the full cycle: satisfy requirements (level, quest, items) -> trigger rebirth via manager -> verify character reset (level 1) + rebirth count increment + stat bonus application + history log.
2. [ ] **Rollback & Audit Safety**: Implement `TWL.Tests/Rebirth/RebirthRollbackAuditTests.cs`. Mock a persistence failure during the rebirth transaction and verify that the character state is rolled back (level/stats remain original) but a "Failure" record is still appended to `RebirthHistory`.
3. [ ] **Pet Integration Verification**: Implement `TWL.Tests/PetTests/PetRebirthIntegrationTests.cs`. Verify that pet rebirth state persists through server save/load cycles and that the client receives the updated `RebirthCount` in pet DTOs.
4. [ ] **Quest Gating Regression**: Implement `TWL.Tests/Quests/QuestGatingTests.cs` to verify that `QuestRequirement` can now check for `MinRebirthLevel`.
5. [ ] **Cross-System Verification**: Run all tests in `TWL.Tests` to ensure no regressions in combat, trade, or other core systems.

## Observability Impact

- **Audit Trail**: `RebirthHistory` in `PlayerSaveData` (visible via character inspection or database) is the definitive source for verifying all rebirth attempts.
- **Transaction Logs**: Server logs (grep "rebirth") will show correlation between client requests and internal transaction outcomes.
- **Fail-Safe Visibility**: Rollback tests will explicitly check for "Failure" reasons in the audit log, providing visibility into why a transaction didn't complete even if state was protected.

## Files

- `TWL.Tests/Rebirth/RebirthEndToEndTests.cs`
- `TWL.Tests/Rebirth/RebirthRollbackAuditTests.cs`
- `TWL.Tests/PetTests/PetRebirthIntegrationTests.cs`
- `TWL.Tests/Quests/QuestGatingTests.cs`
- `TWL.Server/Simulation/Managers/RebirthManager.cs`
- `TWL.Server/Services/PetService.cs`
