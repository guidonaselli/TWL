# S08 Replanning Report

## Blocker Analysis

A blocker was discovered during the execution of task **T03: Implement compound success-rate and outcome engine**.

The root cause is an architectural constraint: the `CompoundManager` service does not have the authority to modify a character's inventory. `ServerCharacter.Inventory` is an `IReadOnlyList<Item>`, which correctly prevents services from directly manipulating the collection. However, this leaves the `CompoundManager` unable to perform its core function: consuming materials and updating equipment upon compound success or failure.

## Plan Changes

To resolve this blocker, the slice plan has been modified to introduce a dedicated refactoring task. This aligns with the "Server is the single source of truth" and "Inject dependencies explicitly" engineering rules. Instead of giving the manager a writable list (which would violate encapsulation), we will refactor the service to operate on the `ServerCharacter` entity itself, using the character's own methods to perform state changes.

### Task Modifications

-   **[NEW] T04: Refactor `CompoundManager` for Authoritative Inventory Mutation.**
    -   This new task directly addresses the blocker from T03. It involves changing the `ICompoundService` and `CompoundManager` method signatures to accept a `ServerCharacter` instance. The manager will then use this instance to call authoritative inventory modification methods (e.g., `RemoveItem`, `UpdateItemEnhancement`). This task absorbs the remaining incomplete work from T03.

-   **[MODIFIED] T05 (was T04): Integrate non-refundable compound fee economics.**
    -   This task is preserved but renumbered. It now builds upon the refactored architecture from T04, which simplifies fee deduction by providing direct access to the character's context.

-   **[MODIFIED] T06 (was T05): Finalize compound client integration.**
    -   This task is also renumbered and remains the final end-to-end verification step for the slice.

-   **[REMOVED] Original T04, T05:** These tasks are not removed but are renumbered and their implementation details adjusted to fit the new architecture.

## Risk Assessment

The primary risk is the slightly larger change surface area of the `ICompoundService` interface. All call sites must be updated. However, this risk is well-contained within the server and is mitigated by the fact that the primary call site (`ClientSession`) is already being modified. The resulting architecture will be more robust and maintainable.
