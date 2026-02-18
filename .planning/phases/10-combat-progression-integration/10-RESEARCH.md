# Phase 10: Combat & Progression Integration - Research

**Researched:** 2026-02-17  
**Domain:** Death penalty, durability and broken-state behavior, instance lockout quotas, and full combat-flow integration  
**Confidence:** High

## Summary

Phase 10 has partial foundations but is not requirement-complete for `CMB-01..04` and `INST-01..03`.

What exists:
- Combat death events are already emitted (`CombatManager.OnCombatantDeath`).
- Player and quest systems already react to combat deaths through `ClientSession` and `PlayerQuestComponent`.
- Instance lockout data is persisted (`ServerCharacter.InstanceLockouts` and `ServerCharacterData.InstanceLockouts`).
- Pet combat AI, status effects, and turn flow are operational from prior phases.

What is missing for this phase goal:
- No explicit player death penalty implementation for EXP loss.
- No durability model on `Item` and no broken-state semantics.
- No per-instance daily run counter with 5/day cap and UTC-midnight reset.
- Instance entry path (`EnterInstanceActionHandler` -> `InstanceService.StartInstance`) currently does not enforce quotas.
- Full combat flow does not yet wire death penalties, durability updates, and related response propagation as a coherent feature slice.

Recommended planning shape:
- 5 plans across 4 waves:
  - Wave 1: death penalty foundation + durability system
  - Wave 2: instance lockout quotas and entry gate
  - Wave 3: full combat-flow integration (death + pet + status + movement/utility seams)
  - Wave 4: phase acceptance and cross-system regression coverage

## Current State Findings

### 1. Death events exist, but death penalties do not

Observed:
- `TWL.Server/Simulation/Managers/CombatManager.cs` invokes `OnCombatantDeath` when target HP reaches 0.
- `TWL.Server/Simulation/Networking/ClientSession.cs` subscribes to this event for quest progression.
- `TWL.Server/Services/PetService.cs` subscribes for pet KO/amity handling.

Gap:
- No server-side module currently applies "lose 1% current-level EXP" on player death.
- No durability loss is attached to death resolution.

### 2. Durability and broken-state mechanics are absent

Observed:
- `TWL.Shared/Domain/Models/Item.cs` currently contains ID, name, type, stack, quantity, forge bonus, and bind metadata only.
- `ServerCharacter` equipment/inventory snapshots persist these fields only.

Gap:
- No durability fields, no repair lifecycle, and no "Broken" state.
- No requirement-level behavior disabling stats from broken equipment.

### 3. Instance lockout persistence exists, but quota policy does not

Observed:
- `ServerCharacter.InstanceLockouts` exists and is persisted in `PlayerSaveData`.
- Quest gating checks instance lockout timestamps in `PlayerQuestComponent.CheckGating`.

Gap:
- No "5 runs per character per day per instance" tracking.
- No UTC-midnight reset system.
- No entry rejection logic in `EnterInstanceActionHandler` or `InstanceService.StartInstance`.

### 4. Instance service is still an infrastructure stub

Observed:
- `TWL.Server/Services/InstanceService.cs` logs start/complete/fail events and dispatches quest completion/failure.

Gap:
- No admission control or run accounting.
- No rejection response path for lockout-exceeded players.

### 5. Combat-flow integration has key seams still open

Observed:
- Combat turn loop, status engine, and pet AI are present.
- Pet utility mount toggles `MoveSpeedModifier` in `PetService`.
- `ClientSession.HandlePetActionAsync` does not process `PetActionType.Utility`.
- `ClientSession.HandleMoveAsync` currently applies raw `dx/dy` without `MoveSpeedModifier`.

Gap:
- Full flow required by Phase 10 should include coherent integration across death penalty, durability effects, pet/status behavior, and movement utility seams.

## Recommended Plan Waves

- Wave 1
  - `10-01`: Death penalty service foundation and combat death integration.
  - `10-02`: Durability and broken-state model integration.
- Wave 2
  - `10-03`: Instance run quota model, UTC reset, and entry rejection path.
- Wave 3
  - `10-04`: Full combat flow integration across death penalties, pet/status systems, and movement/utility seams.
- Wave 4
  - `10-05`: Phase acceptance suite and end-to-end verification for `CMB` and `INST` requirements.

## Verification Targets

- `CMB-01`: Player death deducts exactly 1% of current-level EXP and 1 durability from all equipped items.
- `CMB-02`: Items at durability 0 become broken and their stat effects do not apply.
- `INST-01/02/03`: Per-character per-instance daily run count capped at 5; reset at 00:00 UTC; entry rejected at 5/5.
- `CMB-04`: Combat path remains stable with pet AI and status effects while applying death penalties.

## Risks and Mitigations

- Risk: introducing durability fields breaks existing save compatibility.
  - Mitigation: backward-compatible defaults in `Item` and save-load migration guards.

- Risk: death penalties trigger multiple times for a single death event.
  - Mitigation: idempotent death processing guard keyed by encounter + combatant + tick window.

- Risk: run counter reset uses local time instead of UTC and causes drift.
  - Mitigation: centralize reset computation using `DateTime.UtcNow` and explicit next-reset timestamp.

- Risk: full-flow changes regress existing pet/status/quest combat tests.
  - Mitigation: add dedicated phase acceptance tests plus targeted integration coverage on existing suites.

## Conclusion

Phase 10 should deliver as a coordinated integration phase rather than isolated patches. The architecture already exposes key extension points (combat death event, instance entry handler, persistence model), so completion is primarily about implementing missing progression rules and locking them down with requirement-mapped tests.

