# T02: 09-pet-system-completion 02

**Slice:** S09 — **Milestone:** M001

## Description

Complete PET-02 by finalizing starter-region pet roster and capture-world linkage.

Purpose: Ensure pet content is not only numerous but also progression-ready and obtainable through gameplay.
Output: Updated pet and monster data, strengthened content validation, and roster coverage tests.

## Must-Haves

- [ ] "Starter-region pet roster is complete and gameplay-usable with 20+ populated pet definitions"
- [ ] "Starter-region capturable monsters map to valid pet definitions for normal acquisition flow"
- [ ] "Roster quality constraints are enforced by automated content validation tests"

## Files

- `Content/Data/pets.json`
- `Content/Data/monsters.json`
- `TWL.Tests/ContentIntegrationTests.cs`
- `TWL.Tests/ContentValidationTests.cs`
- `TWL.Tests/PetTests/PetRosterCoverageTests.cs`

## Steps

1. **Test Baseline**: Create or update `PetRosterCoverageTests.cs` to assert that 20+ pets exist and are linked to monsters.
2. **Content Expansion**: Populate `pets.json` with additional starter-region pets, ensuring elemental variety.
3. **Capture Linkage**: Update `monsters.json` to set `PetId` for capturable entities and ensure they match existing `pets.json` entries.
4. **Validation Logic**: Enhance `ContentValidationTests.cs` to ensure no orphaned capturable monsters exist.
5. **Final Verify**: Run `pwsh -File scripts\verify.ps1` to ensure all tests pass.

## Observability Impact

- **Test Feedback**: `PetRosterCoverageTests` provides a direct metric of roster readiness in CI/logs.
- **Validation Logs**: `ContentValidationTests` will fail with specific IDs if data integrity is violated (e.g., "Monster 2001 is capturable but Pet 1005 does not exist").
- **Schema Enforcement**: Errors in JSON structure will be caught during test initialization via deserialization failures.
