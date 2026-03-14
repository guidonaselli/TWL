# T03: 03-content-quality 03

**Slice:** S03 — **Milestone:** M001

## Description

Close localization gaps for Phase 3 quest arcs and codify arc-specific key coverage as a regression guardrail.

Purpose: This plan completes QUAL-01 by ensuring content updates remain localizable and validation-safe across required languages.
Output: Updated resource bundles, tightened localization audit configuration, and dedicated arc-scoped localization coverage tests.

## Must-Haves

- [ ] "Localization resources include all keys required by Hidden Ruins, Ruins Expansion, and Hidden Cove quest content"
- [ ] "Localization validation reports zero ERROR findings after phase-specific content updates"
- [ ] "Arc-specific localization regressions fail immediately when keys are missing in base or English resources"

## Files

- `TWL.Client/Resources/Strings.resx`
- `TWL.Client/Resources/Strings.en.resx`
- `config/localization-audit-allowlist.json`
- `TWL.Tests/Localization/LocalizationValidationTests.cs`
- `TWL.Tests/Localization/LocalizationArcCoverageTests.cs`
