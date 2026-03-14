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

## Steps

1. **DTO Definition**: Define `RebirthRequestDto` and `RebirthResponseDto` in `TWL.Shared`.
2. **Domain Persistence**: Update `PlayerSaveData` to include `RebirthCount`, `RebirthPoints`, and `RebirthAuditLog`.
3. **Formula Implementation**: Implement the 20/15/10/5 diminishing returns formula in `RebirthManager`.
4. **Service Implementation**: Implement `RebirthManager.PerformRebirthAsync` with atomic state mutation.
5. **Network Wiring**: Add `CMSG_REBIRTH_REQUEST` and `SMSG_REBIRTH_RESPONSE` opcodes and handle them in `ClientSession`.
6. **Regression Testing**: Implement `CharacterRebirthTransactionTests` covering success, failure, and formula accuracy.

## Observability Impact

- **Audit Logs**: Every rebirth event is recorded in the character's save data, visible via character inspection or database queries.
- **Opcode Tracing**: Rebirth requests and responses can be traced via network logs using the new opcodes.
- **Exception handling**: `PerformRebirthAsync` will throw specific exceptions on validation failure (e.g., `InsufficientLevelException`), providing clear feedback in server logs.

## Files

- `TWL.Server/Simulation/Managers/IRebirthService.cs`
- `TWL.Server/Simulation/Managers/RebirthManager.cs`
- `TWL.Shared/Domain/DTO/RebirthDTOs.cs`
- `TWL.Server/Persistence/PlayerSaveData.cs`
- `TWL.Shared/Net/Network/Opcode.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Server/Simulation/Program.cs`
- `TWL.Tests/Rebirth/CharacterRebirthTransactionTests.cs`
