# Localization Audit Rules

This document defines the rules enforced by the Localization Auditor to prevent localization drift and ensure key integrity.

## SSOT Authority
1. **Resource Bundles (.resx)** are the Single Source of Truth for localized strings.
   - `Resources/Strings.resx` (Base/Neutral)
   - `Resources/Strings.en.resx` (English)
2. **JSON Content** stores keys only (e.g., `TitleKey`, `DescriptionKey`), never UI strings.
3. **UI Code** uses `Loc.T("KEY")` or `Loc.TF("KEY", ...)` only.

## Enforced Checks

### 1. Missing Keys (ERROR)
- Any key used in Content (JSON) or Code (UI) MUST exist in the required language bundles (Base + English).
- **Severity:** ERROR (Fails CI).

### 2. Orphan Keys (WARN)
- Keys present in resources but NOT used in Content or Code.
- **Severity:** WARN (Report only, unless manual cleanup is performed).

### 3. Forbidden Hardcoded UI Strings (WARN -> ERROR)
- UI-facing strings should not be hardcoded in C# files.
- **Severity:** WARN (currently), may be promoted to ERROR.
- **Exceptions:** Log messages, internal IDs, debug strings, and strings in the allowlist.

### 4. Key Naming Convention (WARN)
- Keys should follow stable prefixes:
  - `UI_`
  - `ERR_`
  - `QUEST_`
  - `SKILL_`
  - `ITEM_`
  - `TUTORIAL_`
  - `ENEMY_`
  - `NPC_`
- **Severity:** WARN.

### 5. Format Safety (ERROR)
- Keys used with `Loc.TF` must match the number of placeholders in the resource string (e.g., `{0}`, `{1}`).
- **Severity:** ERROR.

## Process
- The audit runs as part of the CI pipeline via `LocalizationAuditRunner` test.
- Artifacts are generated in `artifacts/`:
  - `localization-index.json`: Inventory of all keys and usages.
  - `localization-report.json`: List of findings.
- **Fail-Closed:** Any ERROR finding causes the test to fail.
