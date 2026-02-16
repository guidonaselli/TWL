# GSD Orchestrator Report - 2026-02-16

1) TITLE: [INFRA-001] Setup EF Core & Database Infrastructure
2) TYPE: PR
3) SCOPE (IN):
- TWL.Server/TWL.Server.csproj (Add: Npgsql.EntityFrameworkCore.PostgreSQL, Microsoft.EntityFrameworkCore.Design, Dapper)
- TWL.Server/Persistence/Database/Entities/PlayerEntity.cs (New)
- TWL.Server/Persistence/Database/Entities/AccountEntity.cs (New)
- TWL.Server/Persistence/Database/Configurations/PlayerConfiguration.cs (New)
- TWL.Server/Persistence/Database/Configurations/AccountConfiguration.cs (New)
- TWL.Server/Persistence/Database/GameDbContext.cs (New)
- TWL.Server/Persistence/Database/Migrations/* (New)
- TWL.Server/Simulation/Program.cs (Add DI: NpgsqlDataSource, GameDbContext)
- TWL.Server/Persistence/Database/DbService.cs (Remove: "CREATE TABLE IF NOT EXISTS players")
4) OUT-OF-SCOPE:
- TWL.Server/Persistence/FilePlayerRepository.cs (Do NOT delete yet)
- TWL.Server/Persistence/IPlayerRepository.cs (Do NOT modify yet)
- TWL.Server/Persistence/Repositories/DbPlayerRepository.cs (Next task)
5) ACCEPTANCE CRITERIA (DoD):
- dotnet build passes with new packages.
- GameDbContext compiles and configurations are valid.
- Initial Migration `InitialPlayerSchema` generated.
- Migration `Up()` method verifies `accounts` table exists OR simply assumes it (Does NOT run CreateTable("accounts")).
- Migration `Down()` method does NOT run DropTable("accounts").
- dotnet ef database update runs successfully (or validates SQL script).
- Server starts and runs with both `DbService` and `GameDbContext` registered.
6) REQUIRED TESTS / VALIDATIONS:
- dotnet build
- dotnet ef migrations script (Verify SQL validity manually or via script)
- Verify `TWL.Server` starts without DI errors (run briefly).
7) RISKS:
- Risk 1: Migration tries to create `accounts` table which exists. Mitigation: Explicitly remove `Create/DropTable("accounts")` from generated migration file.
- Risk 2: DI conflict between `NpgsqlDataSource` and `DbService`. Mitigation: Use `AddDbContextFactory` for GameDbContext and keep `DbService` using its own connection logic (`new NpgsqlConnection`) for now.
8) NEXT: [INFRA-002] Implement DbPlayerRepository & Migration (Plan 02).
