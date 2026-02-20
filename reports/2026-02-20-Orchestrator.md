# Daily Task - 2026-02-20

## 1) TITLE: [SKL-004] Strengthen Skill Validator & Cleanup Content
## 2) TYPE: PR
## 3) SCOPE (IN):
- **Testing**:
    - `TWL.Tests/ContentValidationTests.cs`: Enforce stricter rules for Stage Upgrades (Rank 6/12) and Control Limits.
- **Content**:
    - `TWL.Client/Content/Data/skills.json`: DELETE (Stale/Duplicate file).
- **Core**:
    - `TWL.Shared/Domain/ContentRules.cs`: (Optional) Adjust rules if validation exposes issues.

## 4) OUT-OF-SCOPE:
- Implementing new skills (`SKL-005`+).
- Fixing `Localization` or `ReplayGuard` failures (unless blocking).
- Client-side rendering changes.

## 5) ACCEPTANCE CRITERIA (DoD):
- [ ] `TWL.Client/Content/Data/skills.json` is deleted.
- [ ] `ContentValidationTests` enforces RankThreshold 6 (Stage 1->2) and 12 (Stage 2->3) for Core skills.
- [ ] `ContentValidationTests` enforces `Duration >= 1` for Hard Control effects.
- [ ] `ContentValidationTests` enforces `MinSp <= MaxSp` sanity check on `ContentRules`.
- [ ] `dotnet test TWL.Tests` passes `ContentValidationTests`.

## 6) REQUIRED TESTS / VALIDATIONS:
- **Unit Tests**: `TWL.Tests` -> `ContentValidationTests` must pass.
- **Verification**: Verify `skills.json` loaded by tests is the root one (via successful test run after client file deletion).

## 7) RISKS:
- **Client Breakage**: If the client relies on the local `skills.json` instead of the copied one.
  - *Mitigation*: The client csproj `Content` item uses `Link` to copy from root `Content/Data`. Deleting the local file should confirm it uses the linked one.
- **Test Failures**: Stricter validation might break existing valid content.
  - *Mitigation*: Adjust content or rules if the "breakage" reveals a valid edge case, but stick to standards (6/12 ranks) otherwise.

## 8) NEXT: [SKL-005] Implement Earth Tier 1 Skills
