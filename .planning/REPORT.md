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
   - Content/Data/quests.json
   - Content/Data/quests_islabrisa_side.json
   - Content/Data/quests_messenger.json
4) OUT-OF-SCOPE:
   - Changes to C# server or client code.
   - Modifications to non-quest JSON data files.
   - Alterations to quest narratives or core logic.
5) ACCEPTANCE CRITERIA (DoD):
   - Quest 1015 rewards Item 6312 (was 8005).
   - Quest 9004 rewards Item 7373 (was 800).
   - Quests 1020 and 1021 reward Item 7316 (was 10001).
   - Quests 2003, 1051, 1100, and 5002 require/reward Item 4739 (was 3001).
   - JSON structure remains perfectly valid.
6) REQUIRED TESTS / VALIDATIONS:
   - `ContentValidationTests` must pass completely to verify schema and data integrity.
7) RISKS:
   - Risk: Breaking JSON formatting. Mitigation: Execute full test suite (`dotnet test`) before submission.
   - Risk: Affecting other quest IDs accidentally. Mitigation: Strict adherence to the scope and DoD items.
8) NEXT: Await explicit authorization to transition to 'PR' and apply the changes.
1) TITLE: Implement Protocol Schema Validation (CORE-001)
2) TYPE: REPORT
3) SCOPE (IN):
- `TWL.Shared/Constants/ProtocolConstants.cs`
- `TWL.Shared/Net/Network/NetMessage.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Client/Net/NetworkClient.cs`
- `TWL.Tests/Security/SecurityTests.cs` (y actualización de instanciación en otros tests)
4) OUT-OF-SCOPE:
- Modificación de la lógica interna de `ReplayGuard` o `RateLimiter`.
- Modificación de handlers de Opcodes específicos.
- Cualquier sistema de gameplay o contenido.
5) ACCEPTANCE CRITERIA (DoD):
- `ProtocolConstants.CurrentSchemaVersion` existe en el namespace `TWL.Shared.Constants`.
- `NetMessage` incluye una propiedad nullable `int? SchemaVersion`.
- `ClientSession.HandleMessageAsync` valida `SchemaVersion` en el orden estricto: 1) ReplayGuard, 2) RateLimiter, 3) SchemaVersion, 4) Opcode Dispatch.
- Si `SchemaVersion` es null o no coincide con `ProtocolConstants.CurrentSchemaVersion`, el servidor desconecta la sesión inmediatamente (Fail-Closed).
- `NetworkClient` inyecta automáticamente `CurrentSchemaVersion` en los paquetes salientes antes de su envío.
6) REQUIRED TESTS / VALIDATIONS:
- `SecurityTests.Connect_WithInvalidSchema_ShouldDisconnect` debe implementarse y pasar exitosamente.
- Los tests existentes que instancian `NetMessage` deben pasar (requerirá asignar explícitamente `SchemaVersion = ProtocolConstants.CurrentSchemaVersion` en los tests para evitar rechazos del `ClientSession`).
7) RISKS:
- Riesgo: Caída de la suite de tests por rechazos generalizados de `ClientSession`. Mitigación: Actualizar todas las instancias manuales de `NetMessage` en el proyecto de tests.
- Riesgo: Desconexiones del cliente por olvido de inyección. Mitigación: Centralizar la inyección únicamente en la capa base de `NetworkClient.SendNetMessage` de forma automática.
8) NEXT: PET-003: Wire Pet Capture System (Gameplay P1)
