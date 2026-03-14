# S02: Security Hardening

**Goal:** Implement packet replay protection using nonce + timestamp validation with a strict 30-second window.
**Demo:** Implement packet replay protection using nonce + timestamp validation with a strict 30-second window.

## Must-Haves


## Tasks

- [x] **T01: 02-security-hardening 01**
  - Implement packet replay protection using nonce + timestamp validation with a strict 30-second window.

Purpose: This plan delivers SEC-02 and establishes the packet trust boundary all later hardening depends on.
Output: NetMessage carries replay metadata, ClientSession validates replay/freshness before handler dispatch, and security tests prove duplicate/stale rejection.
- [x] **T02: 02-security-hardening 02**
  - Implement server-side movement validation to block speed-hack and teleport-style movement payloads.

Purpose: This plan delivers SEC-01 by moving movement trust fully to the server with deterministic checks.
Output: Movement validator with policy controls, ClientSession integration, and tests proving valid vs invalid movement behavior.
- [x] **T03: 02-security-hardening 03**
  - Establish shared idempotency and Serializable transaction foundations for valuable multi-party operations.

Purpose: This plan delivers SEC-03 and SEC-04 in a reusable way that Phase 7 market and Phase 5 guild-bank flows can consume without redesign.
Output: DbService transaction helper, shared idempotency validator, and manager integrations/tests proving duplicate-safe execution semantics.

## Files Likely Touched

- `TWL.Shared/Net/Network/NetMessage.cs`
- `TWL.Server/Security/ReplayGuard.cs`
- `TWL.Server/Security/ReplayGuardOptions.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Server/Simulation/Networking/NetworkServer.cs`
- `TWL.Server/Simulation/Program.cs`
- `TWL.Tests/Security/ReplayGuardTests.cs`
- `TWL.Tests/Security/ClientSessionReplayProtectionTests.cs`
- `TWL.Server/Security/MovementValidationOptions.cs`
- `TWL.Server/Security/MovementValidator.cs`
- `TWL.Shared/Domain/DTO/MoveDTO.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Server/Security/SecurityLogger.cs`
- `TWL.Tests/Security/MovementValidatorTests.cs`
- `TWL.Tests/Security/ClientSessionMovementValidationTests.cs`
- `TWL.Server/Persistence/Database/DbService.cs`
- `TWL.Server/Security/Idempotency/OperationRecord.cs`
- `TWL.Server/Security/Idempotency/IdempotencyValidator.cs`
- `TWL.Server/Simulation/Managers/EconomyManager.cs`
- `TWL.Server/Simulation/Managers/TradeManager.cs`
- `TWL.Server/Simulation/Managers/IEconomyService.cs`
- `TWL.Tests/Security/IdempotencyValidatorTests.cs`
- `TWL.Tests/Security/EconomyIdempotencyFlowTests.cs`
- `TWL.Tests/Security/SerializableTransactionPolicyTests.cs`
