# T01: 01-infrastructure-foundation 01

**Slice:** S01 — **Milestone:** M001

## Description

Set up EF Core 10 infrastructure with GameDbContext, entity configurations, NpgsqlDataSource connection pooling, and initial database migration.

Purpose: Creates the foundation layer that Plan 02 builds on. Without GameDbContext and entity mappings, no repository can use EF Core for writes or share the connection pool with Dapper for reads.

Output: GameDbContext registered in DI, entity configurations for Players and Accounts, initial migration generated, NpgsqlDataSource singleton providing pooled connections.

## Must-Haves

- [ ] "EF Core GameDbContext connects to PostgreSQL and can create/migrate schema"
- [ ] "NpgsqlDataSource provides pooled connections shared by EF Core and future Dapper queries"
- [ ] "EF Core migration tracks schema version and can roll forward/backward"
- [ ] "Player entity maps complex nested data (inventory, equipment, pets, skills, quests) to JSONB columns"

## Files

- `TWL.Server/TWL.Server.csproj`
- `TWL.Server/Persistence/Database/GameDbContext.cs`
- `TWL.Server/Persistence/Database/Configurations/PlayerConfiguration.cs`
- `TWL.Server/Persistence/Database/Configurations/AccountConfiguration.cs`
- `TWL.Server/Simulation/Program.cs`
