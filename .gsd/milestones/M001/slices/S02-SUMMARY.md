---
id: S02
parent: M001
milestone: M001
provides: []
requires: []
affects: []
key_files: []
key_decisions: []
patterns_established: []
observability_surfaces: []
drill_down_paths: []
duration:
verification_result: passed
completed_at:
blocker_discovered: false
---
# S02: Security Hardening

## Summary

### Plan 02-01 — Packet Replay Protection
Packet replay protection was implemented on the server to prevent malicious clients from intercepting and retransmitting previously valid packets.

#### Modifications Made
1. **Network Message Envelope (`TWL.Shared`)**
   - Modified `NetMessage` to include optional `Nonce` (string) and `TimestampUtc` (DateTime).
   - Ensured backward-compatibility for deserializing messages from older clients.

2. **Replay Guard Service (`TWL.Server`)**
   - Created `ReplayGuard` to enforce freshness and nonce uniqueness per user/session.
   - Added `SessionNonceCache` logic for TTL-based eviction and session-bounded storage.

3. **Client Session Integration (`TWL.Server`)**
   - Hooked `ReplayGuard` into `ClientSession.HandleMessageAsync()` before the rate limiter and opcode dispatcher.
   - Added logging through `SecurityLogger` and observability metrics through `ServerMetrics.RecordValidationError`.

4. **Testing (`TWL.Tests`)**
   - Added `ReplayGuardTests` for fresh/stale/future/duplicate nonce handling.
   - Added `ClientSessionReplayProtectionTests` for end-to-end session behavior.

#### Success Criteria Met
- [x] Valid metadata allows message completion
- [x] Exact duplicate nonce within TTL fails
- [x] Stale timestamps (> 30s) fail
- [x] Session caches correctly clean up memory
- [x] Replay protection remains stable under test
- [x] `dotnet test --filter "FullyQualifiedName~Replay"` reports success

### Plan 02-02 — Server-Authoritative Movement Validation
Server-side movement validation was implemented, completing `SEC-01`.

#### Changes Made
- Added `MovementValidationOptions.cs` and `MovementValidator.cs` for deterministic movement limits.
- Intercepted movement in `ClientSession.cs` (`HandleMoveAsync`) before mutating authoritative coordinates.
- Rejected movements emit `MoveValidationRejected` events instead of silently mutating state.
- Hardened tests for axis speed, euclidean speed, and out-of-bounds payloads.
- Fixed older test fixtures that were missing new security dependencies added in 02-01.

#### Artifacts
- **Validator:** `TWL.Server/Security/MovementValidator.cs`
- **Session Integration:** `TWL.Server/Simulation/Networking/ClientSession.cs`
- **Tests:** `MovementValidatorTests.cs`, `ClientSessionMovementValidationTests.cs`

#### Status
- Speed-hack and teleport-style payloads are deterministically blocked by the server.
