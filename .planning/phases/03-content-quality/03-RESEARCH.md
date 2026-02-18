# Phase 3: Content Quality - Research

**Researched:** 2026-02-17
**Domain:** Quest content integrity and localization key hygiene
**Confidence:** High

## Summary

Phase 3 should be planned as a content hardening phase with explicit regression coverage for Hidden Ruins, Ruins Expansion, and Hidden Cove quest arcs, plus localization key parity checks.

Current targeted baseline is green in this repository state:
- `HiddenCoveTests`
- `HiddenRuinsQuestTests`
- `QuestRuinsExpansionTests`
- `ContentValidationTests`
- `LocalizationValidationTests`

Focused run (`dotnet test` with those filters) passed `29/29` tests on 2026-02-17. This means the phase should optimize for reliability and drift prevention, not only "fix currently failing tests".

## Current State Findings

### 1. Arc-specific quest tests exist and are executable

Observed:
- `TWL.Tests/HiddenRuinsQuestTests.cs`
- `TWL.Tests/QuestRuinsExpansionTests.cs`
- `TWL.Tests/HiddenCoveTests.cs`

These already encode end-to-end chain expectations and interaction targets.

Planning implication:
- Use these tests as executable contracts and expand them with negative/gating assertions where coverage is thin.

### 2. Quest design docs and production IDs are not perfectly aligned

Observed:
- `docs/quests/design/2024-05-24-hidden-ruins.md` documents hidden ruins as `1201-1204`
- tests currently validate hidden ruins progression with `1301-1304`
- `docs/quests/design/2024-05-22-signs-of-life.md` uses `1301-1305` for radio/signal arc

Planning implication:
- Include explicit normalization task so `Content/Data/quests.json` and tests agree on canonical IDs for this branch.
- Treat design docs as intent, tests + content as executable truth.

### 3. Localization audit is already CI-oriented and fail-closed for errors

Observed:
- `TWL.Tests/Localization/LocalizationValidationTests.cs`
- `TWL.Tests/Localization/Audit/LocalizationAuditor.cs`
- `docs/core/localization/AUDIT_RULES.md`
- `config/localization-audit-allowlist.json`

Rules:
- Missing keys: `ERROR`
- Format mismatch (`Loc.TF` placeholders): `ERROR`
- Orphans / naming convention / hardcoded UI literals: mostly `WARN`

Planning implication:
- Phase 3 should add phase-specific key coverage for quest arcs so new content does not silently drift.
- Keep allowlist constrained and justified, avoid broad suppressions.

### 4. Content quality depends on both `quests.json` and `interactions.json`

Observed from arc tests:
- progression uses quest objectives from `Content/Data/quests.json`
- item grants and interaction behavior rely on `Content/Data/interactions.json`

Planning implication:
- plans must include both files for each quest arc stabilization task.
- verification should run both quest-arc tests and generic content/localization validators.

## Recommended Planning Shape

Use 3 plans:

1. `03-01` Hidden Ruins + Ruins Expansion stabilization (`1301-1307`, sidequest coverage), with stronger regression tests.
2. `03-02` Hidden Cove chain stabilization (`1401-1404`, `2401`), including interaction and crafting/consumption behaviors.
3. `03-03` Localization closure for these arcs (`Strings.resx` + `Strings.en.resx`) plus arc-specific localization regression checks.

Parallelization:
- Wave 1: `03-01` and `03-02` (arc-specific and mostly independent)
- Wave 2: `03-03` (depends on finalized keys from both arc plans)

## Verification Targets (Phase-level)

- Hidden Ruins and Ruins Expansion chains complete end-to-end in tests.
- Hidden Cove chain and sidequest complete end-to-end in tests.
- Quest objective target names and interaction identifiers remain aligned.
- All quest/localization keys used by these arcs exist in base and English resource files.
- Localization audit returns zero `ERROR` findings.

## Risks and Mitigations

- Risk: design-doc to content ID drift causes accidental regressions.
  - Mitigation: codify canonical IDs in tests and keep docs references in plan context.

- Risk: interaction behavior changes break progression without compile-time signal.
  - Mitigation: strengthen interaction-path assertions in arc tests and run targeted filters.

- Risk: localization keys added in one language only.
  - Mitigation: run localization audit and add phase-specific key-coverage test for both required languages.

## Conclusion

Phase 3 should be executed as a contract-hardening phase that keeps story arcs and localization synchronized. Even though baseline tests currently pass, the plan must preserve pass state while reducing future breakage risk in quest data and localization resources.
