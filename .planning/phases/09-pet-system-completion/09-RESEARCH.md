# Phase 9: Pet System Completion - Research

**Researched:** 2026-02-17  
**Domain:** Pet combat AI, roster/content completeness, amity/bonding progression, and riding utility integration  
**Confidence:** High

## Summary

Phase 9 has meaningful foundations already in place: pets are persisted server-side, combat uses `AutoBattleManager`, amity changes on pet death already happen (`ServerPet.Die()`), and utility logic exists in `PetService` (including mount toggling). However, critical completion gaps remain against PET-01/02/05/06/07:

- utility action routing is incomplete (`PetActionType.Utility` is defined but not handled in `ClientSession`)
- mount speed bonus is stored in character state but not integrated into movement flow
- starter roster breadth exists (`pets.json` has 30 entries) but most entries are under-specified for combat progression (`26/30` with empty `SkillSet`)
- monster capture mapping is sparse (`monsters.json` has only 2 capturable monsters with `PetTypeId`)

Primary implementation direction is to finish server-authoritative behavior first, then align content and client integration:
- tighten pet AI behavior and verify deterministic intelligent decisions
- complete starter roster + capture linkage and enforce via validation tests
- formalize bonding tiers (stat bonus and/or ability unlock outcomes)
- expose riding utility end-to-end through network handling and movement application

## Current State Findings

### 1. Pet AI architecture exists but needs phase-level hardening

Observed:
- `AutoBattleManager` already selects actions using ally HP, status cleanup, and elemental advantage.
- `CombatManager` uses AI turns for enemies and server pets.
- Multiple auto-battle tests exist, but phase-specific pet AI acceptance criteria are not consolidated.

Implication:
- PET-01 should be delivered through explicit pet-focused AI policy verification and regression coverage.

### 2. Roster count is sufficient, but progression completeness is uneven

Observed:
- `Content/Data/pets.json` contains 30 pets (meets 20+ numeric target).
- Only 4 pets have non-empty `SkillSet`; 26 are effectively incomplete for progression depth.
- Only one mount-oriented pet is currently configured with `Mount` utility.

Implication:
- PET-02 requires content completion beyond raw count: skills/evolution-ready progression and starter-region practical coverage.

### 3. Capture/world wiring is currently thin

Observed:
- `Content/Data/monsters.json` contains 15 monsters.
- Only 2 monsters are capturable with `PetTypeId`.

Implication:
- Starter roster cannot be experienced through normal gameplay capture loop without expanded capture mapping.

### 4. Amity KO logic exists; bonding needs explicit phase framing

Observed:
- `ServerPet.Die()` applies `ChangeAmity(-1)` (PET-05 foundation already present).
- `ServerPet.RecalculateStats()` includes high-amity and low-amity stat effects.

Implication:
- PET-05 and PET-06 should be completed by adding explicit bond-tier behavior and phase acceptance tests proving KO decrement + bonding rewards.

### 5. Riding utility exists in service logic but not in network gameplay loop

Observed:
- `PetService.UseUtility(PetUtilityType.Mount)` toggles `IsMounted` and `MoveSpeedModifier`.
- `ClientSession.HandlePetActionAsync` currently handles only `Switch` and `Dismiss`; utility is a TODO path.
- Movement handling does not currently apply `MoveSpeedModifier`.

Implication:
- PET-07 is not complete until mount utility is reachable via request path and visibly affects movement behavior.

## Recommended Planning Shape

Create 5 executable plans:

1. `09-01` Pet combat AI policy hardening and deterministic behavior tests.
2. `09-02` Starter roster/content completion and capture-world linkage expansion.
3. `09-03` Bonding and amity completion (KO decrement verification + bond-tier rewards).
4. `09-04` Riding system integration (utility routing + movement speed application + client flow).
5. `09-05` Phase-level end-to-end PET acceptance suite and integration verification.

Wave recommendation:
- Wave 1: `09-01`, `09-02`
- Wave 2: `09-03`
- Wave 3: `09-04`
- Wave 4: `09-05`

## Verification Targets (Phase-level)

- Pet AI chooses actions based on ally HP/party state, enemy status, and elemental advantage in deterministic server-side logic.
- `pets.json` starter roster is complete and gameplay-wired (20+ practical entries with usable progression metadata).
- Pet amity decreases by exactly 1 on KO/death event in combat path.
- Bonding mechanics grant measurable benefits (stat bonuses and/or unlock behavior) at defined amity thresholds.
- Riding utility can be invoked through pet action flow and results in real movement speed bonus behavior.

## Risks and Mitigations

- Risk: content-only roster expansion without gameplay mapping creates dead data.
  - Mitigation: pair pet roster updates with monster capture mappings and validation tests.

- Risk: riding appears toggled in state but does not impact runtime movement.
  - Mitigation: wire mount state into movement application path and test observable delta.

- Risk: AI logic regressions from policy tweaks.
  - Mitigation: lock behavior with pet-specific deterministic tests rather than generic combat smoke tests.

- Risk: bonding side effects create balance spikes.
  - Mitigation: explicit threshold caps, additive limits, and focused stat-regression tests.

## Conclusion

Phase 9 should focus on finishing integration seams, not inventing net-new pet architecture. The codebase already has most primitives; completion requires stronger content wiring, utility path closure, and acceptance-grade verification for PET-01/02/05/06/07.
