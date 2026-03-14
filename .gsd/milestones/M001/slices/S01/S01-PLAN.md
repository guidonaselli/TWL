# S01: Infrastructure Foundation

**Goal:** Set up EF Core 10 infrastructure with GameDbContext, entity configurations, NpgsqlDataSource connection pooling, and initial database migration.
**Demo:** Set up EF Core 10 infrastructure with GameDbContext, entity configurations, NpgsqlDataSource connection pooling, and initial database migration.

## Must-Haves


## Tasks

- [x] **T01: 01-infrastructure-foundation 01**
  - Set up EF Core 10 infrastructure with GameDbContext, entity configurations, NpgsqlDataSource connection pooling, and initial database migration.

Purpose: Creates the foundation layer that Plan 02 builds on. Without GameDbContext and entity mappings, no repository can use EF Core for writes or share the connection pool with Dapper for reads.

Output: GameDbContext registered in DI, entity configurations for Players and Accounts, initial migration generated, NpgsqlDataSource singleton providing pooled connections.
- [x] **T02: 01-infrastructure-foundation 02**
  - Implement DbPlayerRepository with hybrid EF Core (writes) and Dapper (reads), swap DI registration from FilePlayerRepository, and remove the file-based implementation.

Purpose: Completes the PostgreSQL migration. After this plan, all player persistence uses ACID transactions via PostgreSQL instead of JSON files. The hybrid approach gives atomic writes (EF Core) with high-performance reads (Dapper), preparing for future market/guild features that need concurrent transaction safety.

Output: DbPlayerRepository handling all save/load operations, FilePlayerRepository removed, DI wired to new implementation.
- [x] **T03: 01-infrastructure-foundation 03**
  - Migrate the in-memory guild state (`GuildManager.cs`, `GuildStorageService.cs`) to PostgreSQL using the EF Core + Dapper hybrid pattern established in Phase 1.

Purpose: Fixes the critical GAP where server restarts wipe all guilds and bank items. Ensures guild operations (creation, ranks, storage deposits/withdrawals) are durable and transaction-safe.

Output: `DbGuildRepository`, EF Core `GuildEntity` and mapping configuration, updated `GameDbContext`, and refactored `GuildManager`/`GuildStorageService` to load/save state instead of relying purely on concurrent dictionaries.

## Files Likely Touched

- `TWL.Server/TWL.Server.csproj`
- `TWL.Server/Persistence/Database/GameDbContext.cs`
- `TWL.Server/Persistence/Database/Configurations/PlayerConfiguration.cs`
- `TWL.Server/Persistence/Database/Configurations/AccountConfiguration.cs`
- `TWL.Server/Simulation/Program.cs`
- `TWL.Server/Persistence/Repositories/DbPlayerRepository.cs`
- `TWL.Server/Persistence/Repositories/Queries/PlayerQueries.cs`
- `TWL.Server/Persistence/IPlayerRepository.cs`
- `TWL.Server/Persistence/FilePlayerRepository.cs`
- `TWL.Server/Simulation/Program.cs`
- `TWL.Server/Persistence/Database/Entities/GuildEntity.cs`
- `TWL.Server/Persistence/Database/Configurations/GuildConfiguration.cs`
- `TWL.Server/Persistence/Database/GameDbContext.cs`
- `TWL.Shared/Domain/Guilds/IGuildRepository.cs`
- `TWL.Server/Persistence/Repositories/DbGuildRepository.cs`
- `TWL.Server/Persistence/Repositories/Queries/GuildQueries.cs`
- `TWL.Server/Simulation/Managers/GuildManager.cs`
- `TWL.Server/Simulation/Managers/GuildStorageService.cs`
- `TWL.Server/Simulation/Program.cs`
