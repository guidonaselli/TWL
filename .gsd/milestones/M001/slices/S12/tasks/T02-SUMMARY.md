# T02: 12-quest-expansion 02

## Summary
Created main story quest chains for region 5 (Isla Volcana) covering levels 45-60 and region 6 (Cascada Eterna) covering levels 60-75.

## Details
- Created `Content/Data/quests-volcana.json` with 5 quests (IDs 4010-4014) requiring killing various Volcanic monsters (Gale Orc, Golems, Harpies, Volcana Boss).
- Created `Content/Data/quests-cascada.json` with 5 quests (IDs 5010-5014) requiring killing various Cascada monsters (Gale Harpy, Treants, Lizardmen, Cascada Boss).

## Validation
- `dotnet build -c Release && dotnet test TWL.Tests/TWL.Tests.csproj --filter ContentValidationTests --no-build -c Release` passes successfully, verifying schema correctness, no ID collisions, and no broken references.
