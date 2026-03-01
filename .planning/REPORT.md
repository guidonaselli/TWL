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
