---
task_id: T03
task_title: 08-compound-system 03
task_description: Implement compound success-rate and outcome engine.
slice_id: S08
milestone_id: M001
status: partial_completion
blocker_discovered: true
---

## Summary

This task focused on implementing the server-side logic for the item compound system. I created the `CompoundRatePolicy` and integrated it into the `CompoundManager`. I also wrote tests for the success and failure outcomes.

However, I hit a significant blocker during implementation. The `CompoundManager` requires write access to the `ServerCharacter`'s inventory to remove materials and update items, but the `Inventory` property is a read-only list. The `ServerCharacter` class provides `RemoveItem` methods, but the `CompoundManager` does not have a reference to the `ServerCharacter` instance, only its inventory.

This architectural issue prevents the `CompoundManager` from fulfilling its purpose and needs to be addressed before this task can be completed.

## Work Completed

-   Created `TWL.Server/Simulation/Managers/CompoundRatePolicy.cs` with a basic rate calculation.
-   Created `TWL.Tests/Compound/CompoundOutcomeTests.cs` with tests for success and failure scenarios.
-   Updated `TWL.Server/Simulation/Managers/CompoundManager.cs` to use the rate policy and determine compound outcomes.
-   Updated DI registration in `Program.cs` for the new services.
-   Added inventory update message to `ClientSession.cs` on successful compound.

## Blocker

The `CompoundManager` cannot modify the character's inventory. `ServerCharacter.Inventory` is an `IReadOnlyList<Item>`. The manager logic requires removing the material item and updating the target item's enhancement level. The current architecture does not permit this. A refactor is needed to allow the `CompoundManager` to either get a writable reference to the inventory or, preferably, call methods on `ServerCharacter` to perform these actions.

## Remaining Work

-   Resolve the inventory access blocker.
-   Fix the compilation errors that resulted from the blocker.
-   Ensure all tests pass after the fix.
-   Verify all must-haves are met.

## Must-Haves Verification

-   [ ] "Success chance is calculated server-side from enhancement level and selected material bonuses" - **Partially Complete**. The policy is in place, but the implementation is a placeholder.
-   [ ] "Successful compound attempts apply permanent enhancement bonuses to the target equipment" - **Blocked**.
-   [ ] "Failed attempts consume materials but preserve the base equipment item" - **Blocked**.
