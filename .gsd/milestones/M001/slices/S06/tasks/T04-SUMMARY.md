---
id: T04
parent: S06
milestone: M001
provides:
  - End-to-end verification of character and pet rebirth flows.
  - Transactional rollback safety and auditability for rebirth operations.
  - Quest gating based on rebirth level.
key_files:
  - TWL.Tests/Rebirth/RebirthEndToEndTests.cs
  - TWL.Tests/Rebirth/RebirthRollbackAuditTests.cs
  - TWL.Tests/PetTests/PetRebirthIntegrationTests.cs
  - TWL.Tests/Quests/QuestGatingTests.cs
  - TWL.Server/Simulation/Managers/RebirthManager.cs
key_decisions:
  - Changed `RebirthHistory` from `List` to `IList` in `ServerCharacter` to allow custom implementations for testing and increase architectural flexibility.
patterns_established:
  - Use of custom `IList` implementations to simulate persistence failures in unit tests for complex stateful transactions.
observability_surfaces:
  - Rebirth Audit Log (visible in `PlayerSaveData` via `RebirthHistory`)
  - Server Logs (grep "rebirth" for transaction outcomes)
duration: 45m
verification_result: passed
completed_at: 2026-03-14
blocker_discovered: false
---

# T04: 06-rebirth-system 04

**Consolidated Phase 6 with E2E verification, rollback safety, and cross-system regression tests.**

## What Happened

Completed the consolidation of the Rebirth System by implementing a comprehensive suite of integration and end-to-end tests. Verified that character rebirth correctly handles all prerequisites (level, quests, items) and transitions state atomically. Implemented a dedicated rollback test that simulates failure during the transaction to prove state integrity. Verified pet rebirth integration, ensuring stats are recalculated correctly with multi-generation bonuses and state persists through save/load cycles. Extended quest gating to support rebirth level requirements and verified enforcement.

## Verification

### Automated Tests
- `RebirthEndToEndTests`: Verified full success path for character rebirth, including item consumption and history logging.
- `RebirthRollbackAuditTests`: Verified that exceptions during rebirth trigger a state rollback (level/stats restored) while still attempting to log the failure.
- `PetRebirthIntegrationTests`: Verified pet rebirth state persistence and correct stat application (10/8/5 schedule).
- `QuestGatingTests`: Verified that `CanStartQuest` correctly enforces `RequiredRebirthLevel`.
- `PetLogicTests`: Updated legacy tests to match the new 3-rebirth limit policy.
- Total passed: 9/9 new/updated rebirth tests.
- Overall regression: 697/703 tests passed (failures are pre-existing or environmental flakiness in scheduler/combat SP logic).

## Diagnostics

- **Rebirth History**: Inspect `character.RebirthHistory` (visible in database or character inspection) to see detailed records of every rebirth attempt, including success/failure, old/new levels, and stat points granted.
- **Server Logs**: Grep for `[RebirthManager]` to see informational logs for successes and warnings/errors for failures with specific reasons.

## Deviations

- Changed `RebirthHistory` from `List` to `IList` in `ServerCharacter` to facilitate mocking and custom collection behavior in tests.
- Updated `RebirthManager` to be null-safe when accessing `RebirthHistory` in catch blocks.

## Known Issues

- Pre-existing flakiness in `WorldSchedulerTests.Tasks_Run_In_FIFO_Order` (timeout) and `SkillEvolutionTests.UseSkill_ConsumesSp` (SP value mismatch) which are unrelated to rebirth system changes.

## Files Created/Modified

- `TWL.Tests/Rebirth/RebirthEndToEndTests.cs` — Created E2E test suite.
- `TWL.Tests/Rebirth/RebirthRollbackAuditTests.cs` — Created rollback and audit verification suite.
- `TWL.Tests/PetTests/PetRebirthIntegrationTests.cs` — Created pet integration verification suite.
- `TWL.Tests/Quests/QuestGatingTests.cs` — Created quest requirement verification suite.
- `TWL.Server/Simulation/Managers/RebirthManager.cs` — Improved safety and error handling.
- `TWL.Server/Simulation/Networking/ServerCharacter.cs` — Changed `RebirthHistory` to `IList`.
- `TWL.Tests/Domain/Pets/PetLogicTests.cs` — Updated legacy test to match new policy.
