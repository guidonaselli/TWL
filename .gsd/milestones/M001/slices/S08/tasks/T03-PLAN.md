# T03: 08-compound-system 03

**Slice:** S08 — **Milestone:** M001

## Description

Implement compound success-rate and outcome engine.

Purpose: This delivers CMP-03, CMP-04, and CMP-05 with deterministic server-side compound resolution.
Output: Rate policy component, compound outcome application in manager, and outcome-focused regression tests.

## Must-Haves

- [ ] "Success chance is calculated server-side from enhancement level and selected material bonuses"
- [ ] "Successful compound attempts apply permanent enhancement bonuses to the target equipment"
- [ ] "Failed attempts consume materials but preserve the base equipment item"

## Files

- `TWL.Server/Simulation/Managers/CompoundRatePolicy.cs`
- `TWL.Server/Simulation/Managers/CompoundManager.cs`
- `TWL.Server/Simulation/Networking/ServerCharacter.cs`
- `TWL.Tests/Compound/CompoundOutcomeTests.cs`

## Steps

1.  [ ] Create `TWL.Server/Simulation/Managers/CompoundRatePolicy.cs` with initial success rate logic.
2.  [ ] Create `TWL.Tests/Compound/CompoundOutcomeTests.cs` with placeholder tests for success and failure.
3.  [ ] Modify `TWL.Server/Simulation/Managers/CompoundManager.cs` to use `CompoundRatePolicy`.
4.  [ ] Implement success and failure outcomes in `CompoundManager.cs`.
5.  [ ] Update tests in `CompoundOutcomeTests.cs` to cover success (+enhancement) and failure (-materials).
6.  [ ] Integrate the compound result handling in `TWL.Server/Simulation/Networking/ServerCharacter.cs`.
7.  [ ] Build and run tests to verify all checks pass.

## Observability Impact

-   **Log Source:** `CompoundManager`
-   **Key Signals:**
    -   `CompoundSuccessRateCalculated`: Logged with the calculated success chance, item level, and material bonuses.
    -   `CompoundAttemptSuccess`: Logged when an item is successfully enhanced, including the new `EnhancementLevel`.
    -   `CompoundAttemptFailure`: Logged when a compound attempt fails, preserving the base item.
-   **Inspection Surfaces:** `ServerCharacter.Inventory` items will now have an `EnhancementLevel` property that can be inspected to verify successful compounds.
-   **Redaction:** No PII involved.
