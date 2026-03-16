# T01 Summary: 10-combat-progression-integration 01

## Implemented
- Created `DeathPenaltyService` to handle exactly 1% EXP loss upon player death, floored at zero, idempotently.
- Wired `DeathPenaltyService` into `CombatManager` to apply death penalty automatically during combat death resolution.
- Added `DeathPenaltyService` to DI container in `TWL.Server/Simulation/Program.cs`.

## Files Changed
- `TWL.Server/Services/Combat/DeathPenaltyService.cs` (New)
- `TWL.Tests/Server/Combat/DeathPenaltyServiceTests.cs` (New)
- `TWL.Server/Simulation/Managers/CombatManager.cs` (Modified)
- `TWL.Server/Simulation/Program.cs` (Modified)

## Tests Added
- `DeathPenaltyServiceTests.ApplyExpPenalty_LosesExactlyOnePercent`
- `DeathPenaltyServiceTests.ApplyExpPenalty_FloorsAtZero`
- `DeathPenaltyServiceTests.ApplyExpPenalty_DuplicateEvent_IsIgnored`
- `DeathPenaltyServiceTests.ApplyExpPenalty_FractionalPercent_FloorsToNearestInt`

## Validation
- `dotnet test TWL.Tests/TWL.Tests.csproj --filter DeathPenaltyServiceTests -c Debug` passed successfully.
- Overall `TWL.Tests` suite run excluding Integration category passed successfully.
