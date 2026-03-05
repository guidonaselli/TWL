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
1) TITLE: Implementar Protocol Schema Validation (CORE-001)
# Security / Exploit Review Report

## RESULT: REPORT

*Due to the Anti-Collision Clause and to avoid interfering with ongoing parallel developments, this security review is provided as a detailed report rather than a direct PR.*

## SUMMARY
A targeted security and exploit review of the TWL server-authoritative architecture has been conducted. The current implementation includes several good foundational practices (e.g., `MovementValidator`, `ReplayGuard`, and central `EconomyManager`), but significant vulnerabilities remain, primarily related to lack of proximity validation on critical actions.

## THREATS (Prioritized)

### P0: Missing Proximity Validation on Interactions (Global Interaction Exploit)
*   **Vector:** `InteractRequest` via `ClientSession.cs` and `InteractHandler.cs`.
*   **Scenario:** A malicious client can send an `InteractRequest` specifying any valid `TargetName` (e.g., a quest NPC, a rare chest, or a resource node) regardless of the player's actual coordinates in the world. Since `InteractHandler` and `InteractionManager` only check if the character meets the quest or item requirements but do not verify physical proximity, the player can trigger interactions instantly from across the map or even from different zones. This bypasses exploration, trivializes content, and enables automated farming bots.

### P1: ReplayGuard Transitional Mode Exploitation (Missing strict enforcement)
*   **Vector:** `ReplayGuardOptions.cs` and `ReplayGuard.cs`.
*   **Scenario:** If `ReplayGuardOptions` has `StrictMode = false` (or defaults to it), legacy clients (or malicious scripts posing as such) that omit `nonce` and `timestampUtc` from their `NetMessage` payloads completely bypass replay protection. This allows an attacker to capture and repeatedly send high-value packets (e.g., `PurchaseGemsVerify`, `BuyShopItemRequest`, or `InteractRequest` for resources) if other idempotency measures fail or are absent for that specific opcode.

### P1: Client-side Control of Operation IDs in Economy Operations
*   **Vector:** `BuyShopItemRequest` via `ClientSession.cs` and `EconomyManager.cs`.
*   **Scenario:** `ClientSession` allows the client to pass an arbitrary `OperationId` in `BuyShopItemDTO`. While `EconomyManager` uses this for idempotency, an attacker could potentially manipulate this ID to bypass duplicate checks if the server expects unique IDs but the client intentionally reuses them or sends malformed ones, potentially causing state corruption if not strictly validated for format/length.

## MITIGATIONS

### Mitigation 1: Implement Server-Side Distance Checks (Addresses P0)
*   **Solution:** Introduce a proximity validation check in `InteractHandler.cs` (or before dispatching the command in `ClientSession.HandleInteractAsync`).
*   **Concrete Steps:**
    1.  The `InteractCommand` needs access to the target entity's position. Currently, it only receives `TargetName`.
    2.  Use the `SpawnManager` or a `WorldEntityRegistry` to look up the target entity by `TargetName` and retrieve its `X` and `Y` coordinates.
    3.  Calculate the Euclidean distance between `Character.X`/`Character.Y` and `Target.X`/`Target.Y`.
    4.  Reject the interaction if the distance exceeds a defined `MaxInteractDistance` constant (e.g., 5.0 units).
    5.  Emit a security event via `SecurityLogger` when an out-of-range interaction is attempted.

### Mitigation 2: Enforce Strict Replay Protection (Addresses P1)
*   **Solution:** Enforce `StrictMode = true` in `ReplayGuardOptions` to reject any packets lacking replay metadata.
*   **Concrete Steps:**
    1.  Update `ServerConfig.json` or `Program.cs` configuration to set `Security:ReplayGuard:StrictMode` to `true`.
    2.  Ensure all current clients correctly inject `nonce` and `timestampUtc` via `NetworkClient`.

### Mitigation 3: Server-Generated Operation IDs (Addresses P1)
*   **Solution:** Do not trust client-provided `OperationId` for critical economy actions unless verified against a server-issued intent token.
*   **Concrete Steps:**
    1.  Modify `BuyShopItemDTO` to either remove `OperationId` and rely on a server-generated nonce per transaction, or implement a 2-step process similar to real-money purchases (`Intent` -> `Verify`).
    2.  Alternatively, ensure `OperationId` is strictly validated (e.g., must be a valid GUID) and mapped strictly to the `UserId`.

## IMPLEMENTATION NOTES
*   **Modules to Touch:**
    *   `TWL.Server/Features/Interactions/InteractHandler.cs`: Add proximity logic.
    *   `TWL.Server/Simulation/Networking/ClientSession.cs`: Potentially enrich the `InteractCommand` creation if needed, and validate `OperationId` strings.
    *   `TWL.Server/Simulation/Managers/SpawnManager.cs` (or similar): Expose a method to query entity positions by name.
    *   `TWL.Server/Simulation/Program.cs` / `ServerConfig.json`: Update ReplayGuard configuration.

## NEXT ACTION ITEMS
1.  [TICKET-SEC-001] Implement server-side proximity validation for `InteractRequest`.
2.  [TICKET-SEC-002] Enable StrictMode for ReplayGuard and validate client compatibility.
3.  [TICKET-SEC-003] Audit and harden client-provided `OperationId` usage in economy endpoints.
1) RESULT: REPORT
2) SUMMARY:
The provided daily task ("Consistency Auditor Report") is missing the mandatory fields: SCOPE, OUT-OF-SCOPE, DoD, and TESTS. As per the strict rules, implementation is blocked until these are provided.
- Missing: SCOPE, OUT-OF-SCOPE, DoD (Acceptance Criteria), and TESTS/VALIDATIONS definitions.
- Options:
  A) Provide the missing definitions so the quest JSON fixes can be implemented.
  B) Discard this task and provide a new properly formatted task.
- Recommendation: Option A. Update the daily task ticket to include the missing fields so the quest ID and narrative fixes can be safely executed without expanding scope.
3) CHANGES:
- None. Implementation was aborted due to the malformed task ticket.
4) VALIDATION:
- No tests were run as no code changes were made. Verified the structure of the existing `.planning/REPORT.md` and confirmed the absence of the required fields.
5) FOLLOW-UPS:
- Update the GSD Orchestrator task ticket with SCOPE, OUT-OF-SCOPE, DoD, and TESTS.
- Re-run the daily execution agent once the ticket is fully compliant.
1) TITLE: Fix broken item references and logical inconsistencies in Quest rewards
2) TYPE: REPORT
3) SCOPE (IN):
- TWL.Shared/Constants/ProtocolConstants.cs
- TWL.Shared/Net/Network/NetMessage.cs
- TWL.Server/Simulation/Networking/ClientSession.cs
- TWL.Client/Net/NetworkClient.cs
- TWL.Tests/Security/SecurityTests.cs y actualizaciones en otros tests
4) OUT-OF-SCOPE:
- Modificaciones en la lógica interna de ReplayGuard o RateLimiter.
- Modificaciones en los handlers específicos de Opcodes.
- Sistemas de gameplay o contenido (quests, pets, etc.).
5) ACCEPTANCE CRITERIA (DoD):
- `NetMessage` incluye una propiedad `int? SchemaVersion`.
- `ClientSession.HandleMessageAsync` valida `SchemaVersion` en orden estricto: 1) ReplayGuard, 2) RateLimiter, 3) SchemaVersion, 4) Opcode Dispatch.
- Si `SchemaVersion` es null o distinto a `ProtocolConstants.CurrentSchemaVersion`, se desconecta la sesión inmediatamente (Fail-Closed).
- `NetworkClient` inyecta automáticamente `CurrentSchemaVersion` en los paquetes salientes antes de su envío.
6) REQUIRED TESTS / VALIDATIONS:
- Implementar `SecurityTests.Connect_WithInvalidSchema_ShouldDisconnect` y verificar que pase.
- Todos los tests existentes que instancian `NetMessage` deben ser actualizados asignando explícitamente `SchemaVersion = ProtocolConstants.CurrentSchemaVersion`.
7) RISKS:
- Riesgo: Fallo generalizado en la suite de tests por rechazos en ClientSession. Mitigación: Revisar y actualizar todas las instancias manuales de NetMessage en TWL.Tests.
- Riesgo: Desconexión inadvertida del cliente. Mitigación: Centralizar la inyección automática en el método base `NetworkClient.SendNetMessage`.
8) NEXT: CORE-002: Sesiones + sequence/nonce por cliente para protección anti-replay.

1) TITLE: Implementar validación de proximidad en interacciones (SEC-001)
2) TYPE: REPORT
3) SCOPE (IN):
- TWL.Server/Features/Interactions/InteractHandler.cs
- TWL.Server/Simulation/Networking/ClientSession.cs
- TWL.Server/Simulation/Managers/SpawnManager.cs (o equivalentes de búsqueda de entidades)
- TWL.Tests/Security/SecurityTests.cs
4) OUT-OF-SCOPE:
- Implementación o cambios en el protocolo de red (Opcodes).
- Lógica de quests, items o rewards.
- Modificación de otros mecanismos anti-cheat (ReplayGuard, RateLimiter).
5) ACCEPTANCE CRITERIA (DoD):
- Antes de procesar un `InteractRequest`, el servidor busca la entidad objetivo (ej. NPC/Cofre) en el mundo.
- Se calcula la distancia euclidiana entre las coordenadas del `Character` y del `Target`.
- Si la distancia supera `MaxInteractDistance` (ej. 5.0 o el valor configurado), la interacción se rechaza y no se procesa ninguna lógica adicional.
- Se registra un log de seguridad indicando un posible exploit de interacción global.
6) REQUIRED TESTS / VALIDATIONS:
- `SecurityTests.InteractRequest_OutOfRange_ShouldReject`: Comprobar que una interacción a gran distancia es rechazada por el servidor.
- Verificar el happy path (distancia válida).
7) RISKS:
- Riesgo de desincronización (lag) que provoque rechazos legítimos. Mitigación: Definir un `MaxInteractDistance` con un margen de tolerancia adecuado.
- Problemas de rendimiento si la búsqueda de entidades no está optimizada. Mitigación: Utilizar índices o cachés existentes en el `SpawnManager`.
8) NEXT: SEC-002: Enforce Strict Replay Protection (Security P1).
