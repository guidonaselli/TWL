# Daily Task - 2026-02-19

## 1) TITLE: [PERS-001b] Verify & Test DbPlayerRepository
## 2) TYPE: PR
## 3) SCOPE (IN):
- **Testing**:
    - `TWL.Tests/Persistence/DbPlayerRepositoryTests.cs`: Create unit tests for `DbPlayerRepository`.
    - `TWL.Tests/Mocks/MockDbConnectionFactory.cs`: (Optional) Helper to mock Dapper connection.
- **Refactoring (for Testability)**:
    - `TWL.Server/Persistence/Repositories/DbPlayerRepository.cs`: Decouple `NpgsqlDataSource` if needed to allow mocking (e.g., via `IDbConnectionFactory` or virtual methods).
    - `TWL.Server/Simulation/Program.cs`: Update registration if constructor changes.
- **Verification**:
    - Verify `InitialPlayerSchema` matches `PlayerEntity` properties (visual check or test).
    - Verify `JSONB` serialization for complex types in tests.

## 4) OUT-OF-SCOPE:
- `PERS-002` (Dirty flags / Batch flushing).
- `PERS-003` (Economic transactions).
- Modifying `PlayerSaveData` structure.
- Live database integration tests (if environment disallows Docker/Testcontainers, fallback to Unit Tests with Mocks).

## 5) ACCEPTANCE CRITERIA (DoD):
- [ ] `DbPlayerRepository` is covered by Unit Tests (Load/Save/Mapping).
- [ ] JSON serialization of `Inventory`, `Pets`, `Quests`, etc., is verified correct.
- [ ] `DbPlayerRepository` gracefully handles missing players (returns null).
- [ ] `DbPlayerRepository` updates existing players correctly (Save).
- [ ] Server startup logs verify Database connection (via `DbService`).

## 6) REQUIRED TESTS / VALIDATIONS:
- **Unit Tests**: `TWL.Tests` must pass `DbPlayerRepositoryTests`.
- **Serialization**: Verify `PlayerEntity` <-> `PlayerSaveData` roundtrip.
- **Build**: `dotnet build TWL.Server` and `TWL.Tests` must succeed.

## 7) RISKS:
- **Environment**: `Testcontainers` might fail in restricted sandbox.
  - *Mitigation*: Prioritize "Mocking" approach (`Moq` + `IDbConnection`) over real DB tests if Docker is unavailable.
- **Dapper Mocking**: Mocking extension methods like `QueryFirstOrDefaultAsync` is hard.
  - *Mitigation*: Wrap Dapper calls in a facade (`IDapperService` or similar) or use a wrapper around `IDbConnection` that can be mocked.

## 8) NEXT: [PERS-002] Dirty Flags & Persistence Optimization
