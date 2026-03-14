# S03: Content Quality

**Goal:** Stabilize Hidden Ruins and Ruins Expansion content so quest chains are internally consistent and regression-safe.
**Demo:** Stabilize Hidden Ruins and Ruins Expansion content so quest chains are internally consistent and regression-safe.

## Must-Haves


## Tasks

- [x] **T01: 03-content-quality 01**
  - Stabilize Hidden Ruins and Ruins Expansion content so quest chains are internally consistent and regression-safe.

Purpose: This plan covers the highest-risk content branch in QUAL-01 by making quest data and interaction behavior explicit, test-backed, and resilient to drift.
Output: Updated quest/interaction content for the 1301-1307 branch plus stronger targeted tests and validator checks.
- [x] **T02: 03-content-quality 02**
  - Stabilize Hidden Cove chain content and interaction contracts to prevent progression and crafting regressions.

Purpose: This plan completes the second high-risk branch in QUAL-01 and ensures cove-specific content remains executable end-to-end.
Output: Updated hidden-cove quest and interaction data with stronger regression checks for 1401-1404 and 2401.
- [x] **T03: 03-content-quality 03**
  - Close localization gaps for Phase 3 quest arcs and codify arc-specific key coverage as a regression guardrail.

Purpose: This plan completes QUAL-01 by ensuring content updates remain localizable and validation-safe across required languages.
Output: Updated resource bundles, tightened localization audit configuration, and dedicated arc-scoped localization coverage tests.

## Files Likely Touched

- `Content/Data/quests.json`
- `Content/Data/interactions.json`
- `TWL.Tests/HiddenRuinsQuestTests.cs`
- `TWL.Tests/QuestRuinsExpansionTests.cs`
- `TWL.Tests/Quests/QuestValidationTests.cs`
- `Content/Data/quests.json`
- `Content/Data/interactions.json`
- `TWL.Tests/HiddenCoveTests.cs`
- `TWL.Tests/Quests/QuestValidationTests.cs`
- `TWL.Client/Resources/Strings.resx`
- `TWL.Client/Resources/Strings.en.resx`
- `config/localization-audit-allowlist.json`
- `TWL.Tests/Localization/LocalizationValidationTests.cs`
- `TWL.Tests/Localization/LocalizationArcCoverageTests.cs`
