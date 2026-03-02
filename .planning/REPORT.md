1) WEEKLY TOP 3
1. **SEC-001: Implement Server-Side Proximity Validation for Interactions (Security P0)**
   - **DoD**: `InteractHandler` checks euclidean distance between character and target. Interaction rejected if distance > `MaxInteractDistance` (e.g., 5.0).
   - **Tests**: `SecurityTests.InteractRequest_OutOfRange_ShouldReject`.
2. **CORE-001: Implement Protocol Schema Validation (Security P0)**
   - **DoD**: `NetMessage` includes nullable `int? SchemaVersion`. `ClientSession` disconnects if version mismatch or null. Integration test verifies rejection.
   - **Tests**: `SecurityTests.Connect_WithInvalidSchema_ShouldDisconnect`.
3. **SEC-002: Enforce Strict Replay Protection (Security P1)**
   - **DoD**: `ReplayGuardOptions.StrictMode` set to `true`. Legacy/malicious packets lacking `nonce` and `timestampUtc` are rejected.
   - **Tests**: `ClientSessionReplayProtectionTests.HandleMessage_WithoutNonce_ShouldRejectInStrictMode`.

2) BLOCKERS
- **Missing Proximity Validation on Interactions (SEC-001)**: Malicious clients can trigger interactions (quests, chests) from anywhere on the map.
- **Protocol Schema Vulnerability (CORE-001)**: `ClientSession` trusts all packets without a version check, opening attack vectors.
- **ReplayGuard Exploitation (SEC-002)**: Lack of strict mode enforcement allows bypassing replay protection for critical packets.

3) BACKLOG CHANGES
- **REPRIORITIZATION**: Halted new content development (e.g., Pets, Market, Guilds) to address critical security vulnerabilities discovered by the auditor. Priority shifted to `SEC-001`, `CORE-001`, and `SEC-002` over `PET-003` and `ECO-001`.
- **ADDED**: `SEC-001` (Proximity Validation), `SEC-002` (Strict ReplayGuard), and `SEC-003` (Operation ID Hardening).
- **CLOSED**: `PERS-001a/b`, `PET-001`, `PET-002`, `PET-004` (already verified and marked done in previous weekly status).

4) RECOMMENDED NEXT DAY TASK
- Orchestrator: Assign SEC-001 (Proximity Validation) as P0 priority to prevent global interaction exploits.
