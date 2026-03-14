# T03: 01-infrastructure-foundation 03

**Slice:** S01 — **Milestone:** M001

## Description

Migrate the in-memory guild state (`GuildManager.cs`, `GuildStorageService.cs`) to PostgreSQL using the EF Core + Dapper hybrid pattern established in Phase 1.

Purpose: Fixes the critical GAP where server restarts wipe all guilds and bank items. Ensures guild operations (creation, ranks, storage deposits/withdrawals) are durable and transaction-safe.

Output: `DbGuildRepository`, EF Core `GuildEntity` and mapping configuration, updated `GameDbContext`, and refactored `GuildManager`/`GuildStorageService` to load/save state instead of relying purely on concurrent dictionaries.

## Must-Haves

- [ ] "Guild state is persisted in PostgreSQL instead of memory"
- [ ] "DbGuildRepository uses EF Core for writes and Dapper for reads"
- [ ] "GuildManager and GuildStorageService delegate to DbGuildRepository for state mutation"
- [ ] "Race conditions on bank storage are protected by row-level locking or optimistic concurrency via EF Core"

## Files

- `TWL.Server/Persistence/Database/Entities/GuildEntity.cs`
- `TWL.Server/Persistence/Database/Configurations/GuildConfiguration.cs`
- `TWL.Server/Persistence/Database/GameDbContext.cs`
- `TWL.Shared/Domain/Guilds/IGuildRepository.cs`
- `TWL.Server/Persistence/Repositories/DbGuildRepository.cs`
- `TWL.Server/Persistence/Repositories/Queries/GuildQueries.cs`
- `TWL.Server/Simulation/Managers/GuildManager.cs`
- `TWL.Server/Simulation/Managers/GuildStorageService.cs`
- `TWL.Server/Simulation/Program.cs`
