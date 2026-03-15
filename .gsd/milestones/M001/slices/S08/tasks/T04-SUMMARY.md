---
task_id: T04
status: incomplete
blocker_discovered: true
---

## Summary

This task focused on refactoring the `CompoundManager` to ensure it performs authoritative inventory mutations, resolving an architectural blocker where item operations were performed on copied data structures, leading to state inconsistencies.

**Completed Work:**

1.  **`ServerCharacter` API Extension**:
    - Added `bool RemoveItemByInstanceId(Guid instanceId, int quantity)` to provide a safe, authoritative way to remove a specific item instance from a character's inventory.
    - Added `bool EnhanceItem(Guid instanceId, int levelsToAdd)` to authoritatively handle the enhancement of a specific item instance.

2.  **`CompoundManager` Refactoring**:
    - Modified `ProcessCompoundRequest` to use the new `RemoveItemByInstanceId` and `EnhanceItem` methods on the `ServerCharacter` instance.
    - This ensures that all inventory changes (material consumption, enhancement level increase) are correctly persisted.

**Blocker Discovered:**

Verification of these changes is currently blocked by widespread, unrelated test failures across the solution. After implementing the refactoring, running the full test suite (`scripts/verify.ps1`) revealed numerous build errors originating from `InteractionTests.cs`, `EscortQuestTests.cs`, and others, all related to a breaking change in the `InteractionType` enum.

The core changes for this task are complete and were locally validated by updating the `CompoundOutcomeTests.py`. However, these test fixes were reverted to get the project back into a buildable state. The unrelated failures in the test suite prevent a full-project `dotnet test` from passing, which is a requirement for marking this task as complete.

**Next Steps:**

A separate effort is required to fix the broken test suite. Once the tests related to the `InteractionType` enum are fixed, the changes from this task can be re-verified and the task can be marked as complete.
