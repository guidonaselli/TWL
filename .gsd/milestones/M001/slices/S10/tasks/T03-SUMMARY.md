# T03-SUMMARY

## What was implemented
- Implemented per-instance daily run limits to prevent unlimited farming.
- `InstanceDailyRuns` and `InstanceDailyResetUtc` were added to `ServerCharacter`, `PlayerSaveData`, `PlayerEntity`, and mapped in EF Core (`PlayerConfiguration`) and Dapper (`PlayerQueries`).
- Modified `InstanceService` to maintain a daily limit of 5. It handles checking the limit and correctly resets the counters when the `InstanceDailyResetUtc` date indicates a new day in UTC.
- `EnterInstanceActionHandler` now queries `CanEnterInstance`. If the limit is reached, it denies entry and sends a `SystemMessage` to the client. Otherwise, it calls `RecordInstanceRun` to increment the daily runs.

## Files changed
- `TWL.Server/Persistence/PlayerSaveData.cs`
- `TWL.Server/Simulation/Networking/ServerCharacter.cs`
- `TWL.Server/Persistence/Database/Entities/PlayerEntity.cs`
- `TWL.Server/Persistence/Database/Configurations/PlayerConfiguration.cs`
- `TWL.Server/Persistence/Repositories/Queries/PlayerQueries.cs`
- `TWL.Server/Persistence/Repositories/DbPlayerRepository.cs`
- `TWL.Server/Services/InstanceService.cs`
- `TWL.Server/Services/World/Actions/Handlers/EnterInstanceActionHandler.cs`
- `TWL.Tests/Server/Instances/InstanceRunLimitTests.cs` (Created)
- `TWL.Tests/Benchmarks/PlayerPersistenceBenchmark.cs`
- `TWL.Tests/Benchmarks/PlayerServicePerformanceTests.cs`
- `TWL.Tests/Persistence/PersistenceTests.cs`
- `TWL.Tests/Persistence/PlayerServiceReliabilityTests.cs`
- `TWL.Tests/Performance/GuildRosterPerformanceTests.cs`

## Tests added
- `InstanceRunLimitTests.cs` covering:
  - `CanEnterInstance_UnderCap_AllowsEntry`
  - `CanEnterInstance_AtCap_RejectsEntry`
  - `CanEnterInstance_PastResetDate_ResetsAndAllowsEntry`
  - `RecordInstanceRun_IncrementsCounter_MarksDirty`
