# WEEKLY REPORT: 2026-02-25

## 1) WEEKLY TOP 3
1.  **CORE-001: Protocol Schema Validation (Security P0)**
    *   **DoD**: `NetMessage` includes `SchemaVersion`. `ClientSession` disconnects if version mismatch. Integration test verifies rejection.
    *   **Tests**: `SecurityTests.Connect_WithInvalidSchema_ShouldDisconnect`.
2.  **PET-003: Wire Pet Capture System (Gameplay P1)**
    *   **DoD**: `CombatManager` processes `SkillEffectTag.Capture`, calls `PetService.CaptureEnemy`. Returns success/fail message.
    *   **Tests**: `CombatPetTests.CaptureSkill_ShouldCallService_AndAddPet`.
3.  **ECO-001: Alchemy System Foundation (Economy P1)**
    *   **DoD**: Implement `Opcode.CompoundRequest` and `AlchemyManager` with tier-based logic.
    *   **Tests**: `AlchemyTests.Compound_ShouldConsumeItems_AndCreateResult`.

## 2) BLOCKERS
*   **Security Vulnerability (CORE-001)**: `ClientSession` currently trusts all packets without version check.
*   **Unreachable Feature (PET-003)**: Pet Capture logic exists in `PetService` but is not hooked into `CombatManager`.
*   **Reliability Regressions**: 5 Failing tests (Shutdown, Observability, QuestIntegration).

## 3) BACKLOG CHANGES
*   **COMPLETED (Mark as Done)**:
    *   `PERS-001a/b`: Persistence Infrastructure & Repository (Verified: `DbPlayerRepository` active, `FilePlayerRepository` removed).
    *   `PET-001`: Persistent Pet Model (JSONB in Player).
    *   `PET-002`: Pet Combat AI (Verified: `CombatManager` calls `AutoBattleManager` for pets).
    *   `PET-004`: Quest Unique Pet Rewards (Verified: `PlayerQuestComponent.ClaimReward` handles `PetUnlockId`).
*   **UPDATED**:
    *   `CORE-001`: Priority raised to P0 (Security).
    *   `ECO-005`: Marketplace split into `ECO-005a` (Listing Core) and `ECO-005b` (Search/Buy).

## 4) RECOMMENDED NEXT DAY TASK
*   `Orchestrator: Assign CORE-001 (Protocol Security) as P0 priority to secure the transport layer.`
