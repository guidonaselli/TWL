# Daily Task - 2026-02-21

## 1) TITLE: [CORE-002] Fix ReplayGuard Vulnerability & Memory Leak
## 2) TYPE: PR
## 3) SCOPE (IN):
- **Core**:
    - `TWL.Server/Simulation/Networking/ClientSession.cs`: Implement stable connection ID for pre-login replay protection.
- **Testing**:
    - `TWL.Tests/Security/ClientSessionReplayProtectionTests.cs`: Add reproduction test case.

## 4) OUT-OF-SCOPE:
- Implementing persistent nonce storage (DB).
- Changing `ReplayGuard` implementation (only usage in `ClientSession`).

## 5) ACCEPTANCE CRITERIA (DoD):
- [x] Pre-login packets with same nonce/content but different object instances are REJECTED.
- [x] Authenticated packets continue to use `UserId` for replay protection (cross-session security).
- [x] Session cleanup removes BOTH `UserId` and `_connectionId` keys from `ReplayGuard` to prevent memory leaks.
- [x] All Security tests pass.

## 6) REQUIRED TESTS / VALIDATIONS:
- **Unit Tests**: `TWL.Tests` -> `ClientSessionReplayProtectionTests` -> `HandleMessageAsync_PreLogin_DuplicateNonce_Rejected` MUST PASS.
- **Regression**: `TWL.Tests` -> `Security` filter MUST PASS.

## 7) RISKS:
- **Collision**: If `_connectionId` collides with `UserId`.
  - *Mitigation*: Use negative integers for `_connectionId` (atomic decrement from -1) and positive for `UserId`.
- **Memory Leak**: If `RemoveSession` is not called for `_connectionId`.
  - *Mitigation*: Call `RemoveSession(_connectionId)` in `finally` block of `ReceiveLoopAsync`.

## 8) NEXT: [QST-001] Quest System JSON Schema Validator
