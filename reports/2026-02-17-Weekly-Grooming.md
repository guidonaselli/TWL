# Weekly Roadmap / Backlog Grooming - 2026-02-17

## 1. Weekly Top 3
1. **[PERS-001a] Setup EF Core & Database Infrastructure**
   - **DoD**: `GameDbContext` compiles, migrations generated (`InitialPlayerSchema`), `DbService` registers context, server starts.
   - **Tests**: `dotnet build`, manual migration verification (SQL script check), server startup smoke test.
2. **[PERS-001b] Implement DbPlayerRepository**
   - **DoD**: `DbPlayerRepository` implements `IPlayerRepository`, supports `SaveAsync`/`LoadAsync` for Players.
   - **Tests**: Integration tests validating data persistence to DB (or in-memory EF).
3. **[CORE-002] Packet Replay Protection**
   - **DoD**: `NetMessage` includes `Sequence` field. Server rejects packets with `Seq <= LastSeq` per session.
   - **Tests**: Unit tests simulating replay attacks and out-of-order packets.

## 2. Blockers
- **[PERS-001a] Infrastructure**: The lack of a database layer blocks all persistence work (`PERS-002`, `PERS-003`) and data migration.
  - **Action**: Complete `PERS-001a` immediately.

## 3. Backlog Changes
- **Split [PERS-001]**: Divided "Modelo de estado persistente mínimo" into:
  - `PERS-001a`: Infrastructure Setup (Entities, Context, Migrations).
  - `PERS-001b`: Repository Implementation (CRUD Logic).
  - Reason: `PERS-001` was too large for a single atomic PR/Task.
- **Prioritized [CORE-002]**: Elevated "Packet Replay Protection" to P0 (Security) immediately following persistence setup.

## 4. Recommended Next Day Task
- **[PERS-001a] Setup EF Core & Database Infrastructure**
