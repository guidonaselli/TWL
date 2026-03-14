# T01: 06-rebirth-system 01

**Slice:** S06 — **Milestone:** M001

## Description

Implement character rebirth transactional foundation, including formula policy, atomic state mutation, persistence history, and network entry points.

Purpose: This delivers REB-01, REB-05, REB-06, and REB-07 as a secure base for all remaining rebirth functionality.
Output: Rebirth service + DTO contracts + opcode/session wiring + transaction and formula regression tests.

## Must-Haves

- [ ] "Character rebirth at level 100+ resets level to 1 and grants permanent stat bonuses using 20/15/10/5 diminishing returns"
- [ ] "Character rebirth executes atomically and does not leave partial state on failure"
- [ ] "Character rebirth writes auditable history records for debugging and rollback analysis"

## Files

- `TWL.Server/Simulation/Managers/IRebirthService.cs`
- `TWL.Server/Simulation/Managers/RebirthManager.cs`
- `TWL.Shared/Domain/DTO/RebirthDTOs.cs`
- `TWL.Server/Persistence/PlayerSaveData.cs`
- `TWL.Shared/Net/Network/Opcode.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Server/Simulation/Program.cs`
- `TWL.Tests/Rebirth/CharacterRebirthTransactionTests.cs`
