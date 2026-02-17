# Daily Task - 2026-02-18

## 1) TITLE: [PERS-001a] Setup EF Core & Database Infrastructure
## 2) TYPE: PR
## 3) SCOPE (IN):
- **Infrastructure**:
    - `TWL.Server/TWL.Server.csproj`: Add `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design`.
- **Persistence Layer**:
    - `TWL.Server/Persistence/Database/GameDbContext.cs`: Create DbContext with `PlayerEntity`.
    - `TWL.Server/Persistence/Database/PlayerEntity.cs`: Define schema based on `PlayerSaveData` (minimal subset for now if needed, or full map).
    - `TWL.Server/Persistence/Database/DbService.cs`: Update to handle Context/Migration verification.
- **Configuration**:
    - `TWL.Server/Simulation/Program.cs`: Register `GameDbContext` in DI.
- **Migrations**:
    - `TWL.Server/Migrations/`: Generate `InitialPlayerSchema`.

## 4) OUT-OF-SCOPE:
- `DbPlayerRepository` implementation (PERS-001b).
- Removing `FilePlayerRepository`.
- Any client-side logic.
- Complex migrations beyond Initial Create.

## 5) ACCEPTANCE CRITERIA (DoD):
- [ ] `GameDbContext` compiles and maps `PlayerEntity` correctly.
- [ ] `InitialPlayerSchema` migration is generated.
- [ ] Server starts and `DbService` validates/migrates the database on startup (or logs connection success).
- [ ] `dotnet build` passes with no warnings.
- [ ] `dotnet ef migrations add` works (requires tool installed, or I generate it).

## 6) REQUIRED TESTS / VALIDATIONS:
- **Build**: `dotnet build TWL.Server` matches `TreatWarningsAsErrors`.
- **Smoke Test**: Run server, verify logs show "Connected to Database" or similar.
- **Migration**: Verify generated SQL contains `CREATE TABLE` for players.

## 7) RISKS:
- **Environment**: PostgreSQL connection string might be missing in CI or other devs' machines.
  - *Mitigation*: Fallback or clear error message in `DbService`.
- **Complexity**: Mapping `PlayerSaveData` (nested objects like Inventory/Pets) to Relational DB.
  - *Mitigation*: For `PERS-001a`, we might use JSONB for complex nested lists (Inventory, Pets) or simple separate tables. Given "Minimum", JSONB for lists is often a good start for rapid prototyping, or strictly normalized tables. I will use JSONB for nested collections to match `PlayerSaveData` structure easily for now, or defined relationships if strictly required. *Decision*: Use JSONB for `Inventory`, `Equipment`, `Pets`, `Quests` to speed up Phase A, unless Relational is strictly requested. (Roadmap just says "Entities"). I will start with JSONB for complex fields to keep it simple and aligned with the "Load/Save" document model of `PlayerSaveData`.

## 8) NEXT: [PERS-001b] Implement DbPlayerRepository
