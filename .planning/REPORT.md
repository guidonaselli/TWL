1) TITLE: Implement Server-Side Proximity Validation for Interactions (SEC-001)
2) TYPE: REPORT
3) SCOPE (IN):
- TWL.Server/Features/Interactions/InteractHandler.cs
- TWL.Server/Simulation/Networking/ClientSession.cs
- TWL.Server/Simulation/Managers/SpawnManager.cs (or equivalent entity registry)
4) OUT-OF-SCOPE:
- Modifying client-side interaction UI/logic.
- Changes to `ReplayGuard` or `RateLimiter` policies.
- Adding new quests or items.
5) ACCEPTANCE CRITERIA (DoD):
- `InteractCommand` is updated to receive both `Character` and target position (or target entity).
- `InteractHandler` calculates the Euclidean distance between `Character` and target entity.
- If the distance exceeds a defined constant (e.g., `MaxInteractDistance = 5.0f`), the interaction is rejected.
- A security event is logged via `SecurityLogger` upon rejection.
6) REQUIRED TESTS / VALIDATIONS:
- Implement `SecurityTests.InteractRequest_OutOfRange_ShouldReject` verifying that interaction fails when distance > 5.0.
- Verify `SecurityTests.InteractRequest_InRange_ShouldSucceed` verifying normal functionality.
7) RISKS:
- Risk: Legitimate players unable to interact due to sync issues. Mitigation: Add a small tolerance to `MaxInteractDistance` and log rejections clearly for debugging.
- Risk: Missing entity lookup in `InteractHandler`. Mitigation: Ensure `SpawnManager` or `WorldEntityRegistry` exposes a reliable `GetEntityPosition(string name)` method.
8) NEXT: Enforce Strict Replay Protection (SEC-002)