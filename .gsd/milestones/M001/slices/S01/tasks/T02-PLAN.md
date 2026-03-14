# T02: 01-infrastructure-foundation 02

**Slice:** S01 — **Milestone:** M001

## Description

Implement DbPlayerRepository with hybrid EF Core (writes) and Dapper (reads), swap DI registration from FilePlayerRepository, and remove the file-based implementation.

Purpose: Completes the PostgreSQL migration. After this plan, all player persistence uses ACID transactions via PostgreSQL instead of JSON files. The hybrid approach gives atomic writes (EF Core) with high-performance reads (Dapper), preparing for future market/guild features that need concurrent transaction safety.

Output: DbPlayerRepository handling all save/load operations, FilePlayerRepository removed, DI wired to new implementation.

## Must-Haves

- [ ] "Player data saves to PostgreSQL atomically using EF Core with transaction safety"
- [ ] "Player data loads from PostgreSQL using Dapper with high-performance single-query read"
- [ ] "FilePlayerRepository is removed and all player operations use PostgreSQL"
- [ ] "Existing PlayerService flush loop works unchanged with the new repository"
- [ ] "Complex nested data (inventory, equipment, pets, skills, quests) round-trips through JSONB without data loss"

## Files

- `TWL.Server/Persistence/Repositories/DbPlayerRepository.cs`
- `TWL.Server/Persistence/Repositories/Queries/PlayerQueries.cs`
- `TWL.Server/Persistence/IPlayerRepository.cs`
- `TWL.Server/Persistence/FilePlayerRepository.cs`
- `TWL.Server/Simulation/Program.cs`
