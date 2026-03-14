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
