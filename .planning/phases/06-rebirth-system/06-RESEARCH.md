# Phase 6: Rebirth System - Research

**Researched:** 2026-02-17
**Domain:** Character and pet prestige progression (rebirth), transactional safety, and visibility
**Confidence:** High

## Summary

Phase 6 is partially scaffolded but not implemented as a complete system. The server already persists `RebirthLevel` for characters and supports a basic one-time pet rebirth path, but there is no server-authoritative character rebirth operation, no diminishing-return schedule for repeated rebirth, no explicit rebirth history/audit model, and no client-visible prestige display path.

The strongest implementation anchor is existing server-side mutation and safety patterns in `ServerCharacter`, `EconomyManager`, and `TradeManager` (lock discipline, idempotent operation patterns, and explicit audit logging). Rebirth should follow these patterns to satisfy `REB-05` and `REB-06` instead of introducing ad-hoc state updates.

Pet rebirth currently allows only a single rebirth (`HasRebirthed`) with fixed percentage scaling. Phase 6 should upgrade this to generation-based rebirth (`10/8/5` diminishing), enforce quest-vs-capturable eligibility (`PET-03`), and expose operation outcomes over network contracts.

## Current State Findings

### 1. Character rebirth state exists, operation does not

Observed:
- `ServerCharacter` contains `RebirthLevel` persisted through `GetSaveData()` / `LoadSaveData()`.
- No dedicated character rebirth service/manager or opcode handler exists in `ClientSession`.
- Shared `PlayerCharacter.Rebirth()` exists but uses fixed `+10` stat boosts and does not represent server transactional behavior.

Implication:
- Need a server-authoritative character rebirth workflow with gating, formula, and persistence guarantees.

### 2. Rebirth gating hooks already exist in quests

Observed:
- `PlayerQuestComponent.CanStartQuest` checks `RequiredRebirthLevel`.
- Existing gating tests (`PvPAndGatingTests`) validate party/guild parity behavior.

Implication:
- Rebirth changes must preserve quest gating integrity and include regression coverage for rebirth-level gate checks.

### 3. Pet rebirth foundation exists but is under-scoped for phase requirements

Observed:
- `PetService.TryRebirth()` delegates to `ServerPet.TryRebirth()`.
- `ServerPet.TryRebirth()` currently enforces level 100 and one-time rebirth with ~10% stat scaling.
- `PetActionRequest`/`PetActionType` lacks explicit rebirth action and `ClientSession` pet handler only supports switch/dismiss utility paths.

Implication:
- Must evolve pet rebirth model and expose network operations for rebirth/evolution outcomes.

### 4. Persistence supports extension for audit/history

Observed:
- `PlayerSaveData`/`ServerCharacterData` already serialize core progression and pets.
- No rebirth-history record structure exists for rollback diagnostics.

Implication:
- Add explicit rebirth history entries (timestamp, actor, old/new stats/levels, operation id/result) to meet `REB-06`.

### 5. Client has no prestige surface yet

Observed:
- Login and character payloads (`LoginResponseDto`, `PlayerCharacterData`) do not carry rebirth count.
- Gameplay HUD (`UiGameplay`) has level/HP/SP/XP but no rebirth display.

Implication:
- Add server payload fields + client model/UI updates for visible prestige (`REB-03`).

## Recommended Planning Shape

Create 4 executable plans:

1. `06-01` Character rebirth transaction foundation: server service, diminishing formula, persistence history/audit, and network request/response baseline.
2. `06-02` Character rebirth eligibility + prestige visibility: optional quest/item requirement checks, skill/equipment retention invariants, and character info/nameplate/HUD rebirth display.
3. `06-03` Pet rebirth policy completion: quest-vs-capturable eligibility, generation-based diminishing bonuses (`10/8/5`), and pet rebirth action routing.
4. `06-04` End-to-end rebirth verification and rollback safety tests: cross-system regression suite validating character + pet requirements and failure rollback behavior.

Wave recommendation:
- Wave 1: `06-01`
- Wave 2: `06-02`, `06-03` (parallel, both depend on rebirth foundation contracts)
- Wave 3: `06-04` (depends on both feature branches being integrated)

## Verification Targets (Phase-level)

- Character rebirth requires level 100+, applies `20/15/10/5` schedule, and resets level to 1 atomically.
- Optional rebirth prerequisites (quest flag/item) are enforced server-side.
- Rebirth count is persisted and surfaced to client character info/nameplate/HUD.
- Character retains skills and equipped items while allowing gear use at level 1 post-rebirth.
- Rebirth writes auditable history records with operation-level traceability and rollback context.
- Pet rebirth allows quest pets/evolution-eligible pets only; capturable pets are rejected.
- Pet rebirth applies diminishing bonus schedule (`10/8/5`) and evolution transitions.

## Risks and Mitigations

- Risk: non-atomic rebirth updates could corrupt progression state.
  - Mitigation: encapsulate rebirth in single service operation with explicit pre/post snapshots and rollback path.

- Risk: rebirth formula drift between character and pet implementations.
  - Mitigation: centralize formula rules in reusable policy helpers and test schedules directly.

- Risk: client/server display mismatch for rebirth prestige.
  - Mitigation: add explicit payload fields and serialization tests for rebirth count propagation.

- Risk: pet eligibility regressions (capturable vs quest differentiation).
  - Mitigation: codify eligibility in tests using existing pet definition/capture rule fixtures.

## Conclusion

Phase 6 should be planned as a transactional progression subsystem, not a stat tweak. The implementation must prioritize server-authoritative rebirth operations, auditable history, and explicit client visibility, while upgrading existing pet rebirth logic to match requirement-level differentiation and diminishing-return behavior.
