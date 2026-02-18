# Phase 5: Guild System - Research

**Researched:** 2026-02-17
**Domain:** Multiplayer guild orchestration (membership, permissions, chat, storage, audit)
**Confidence:** High

## Summary

Phase 5 should be planned as a foundational social-system build similar to Phase 4, but with stronger persistence and abuse-resistance requirements because guild storage and rank permissions have economy impact.

The current codebase has only early guild signals (`ServerCharacter.GuildId`, quest `GuildRules`, and guild quest progress hooks) and no actual guild runtime service, protocol contracts, or client guild UX. This means planning should treat Guild as a net-new subsystem with explicit server authority.

A focused baseline run of `PvPAndGatingTests` currently fails both party-required and guild-required start checks, which confirms `CanStartQuest` and `StartQuest` gating parity is still broken in this repo state. Since Phase 5 depends on Phase 4, Guild plans should assume that parity fix lands in Phase 4 and keep guild quest-progress wiring explicit.

## Current State Findings

### 1. Protocol intent exists for party, not guild

Observed:
- `TWL.Shared/Net/Messages/ClientMessageType.cs` and `ServerMessageType.cs` include party verbs only.
- `TWL.Shared/Net/Network/Opcode.cs` has no party or guild opcodes.
- `TWL.Server/Simulation/Networking/ClientSession.cs` has no party/guild handlers.

Implication:
- Guild requires full opcode + DTO + handler surface from scratch.

### 2. Guild runtime/service layer is missing

Observed:
- `TWL.Server/Simulation/Managers` has no guild manager/service/chat/storage manager.
- `TWL.Server/Simulation/Program.cs` and `NetworkServer.cs` DI/session wiring include no guild dependencies.
- `ServerCharacter` only contains `GuildId` with no guild-role/rank state.

Implication:
- Plan must create authoritative guild domain services before UI or storage features.

### 3. Persistence is file-save centric and guild data is absent

Observed:
- Active persistence path still uses `IPlayerRepository` + `FilePlayerRepository`.
- `PlayerSaveData` / `ServerCharacterData` do not contain guild membership metadata beyond runtime `GuildId`.
- No guild repository/table/aggregate exists in `TWL.Server/Persistence`.

Implication:
- Guild membership, ranks, storage contents, withdrawal gates, and audit trail need explicit persistence model additions.

### 4. Security patterns exist that guild bank should reuse

Observed:
- `EconomyManager` and `TradeManager` already implement idempotency, lock discipline, and explicit audit/security logging patterns.
- `ServerCharacter` inventory/bank mutation methods are lock-protected and support exact item policy matching.

Implication:
- Guild storage and withdrawal should reuse the same patterns (operation ids, append-only audit records, exact item mutation, compensation behavior on partial failures).

### 5. Quest hooks already expect guild actions

Observed:
- `PlayerQuestComponent.HandleGuildAction` exists.
- Guild-required quest tests exist and currently fail due to start-gating parity issue.

Implication:
- Guild lifecycle operations should emit consistent quest action hooks (create/join/leave/promote/storage actions where relevant) once phase dependencies are satisfied.

## Recommended Planning Shape

Create 4 executable plans:

1. `05-01` Guild foundation: create/invite/accept/leave/kick lifecycle, unique-name + creation-fee enforcement, baseline guild protocol contracts.
2. `05-02` Rank hierarchy and permission enforcement: promote/demote role flow, permission checks centralized for invite/kick/storage actions.
3. `05-03` Guild communication and roster visibility: guild chat routing with offline persistence plus roster sync (online status, last login, rank).
4. `05-04` Guild storage and compliance controls: shared storage deposit/withdraw, withdrawal time gate for new members, append-only withdrawal audit log.

Wave recommendation:
- Wave 1: `05-01`
- Wave 2: `05-02`
- Wave 3: `05-03`, `05-04` (parallel, both depend on foundation + permission model)

## Verification Targets (Phase-level)

- Guild creation enforces unique name and configurable creation fee.
- Invite/join/leave/kick flows are server-authoritative and rank-gated.
- Rank permission matrix controls invite/promote/kick/storage withdrawal actions.
- Guild chat is visible only to guild members and persists for offline catch-up.
- Guild roster exposes member, rank, online status, and last login.
- Guild storage deposit/withdraw obeys permissions and member-age withdrawal gate.
- Withdrawal operations write immutable audit entries (who/what/when).

## Risks and Mitigations

- Risk: chat/storage/guild membership drift across runtime and persistence.
  - Mitigation: canonical guild domain model + explicit snapshot/update DTOs and deterministic tests.

- Risk: guild bank theft or abuse.
  - Mitigation: rank-gated withdraw, join-age gate, operation ids, audit log assertions in tests.

- Risk: over-coupling to unfinished party-phase artifacts.
  - Mitigation: depend on phase outputs by interface/contract, keep guild plans scoped to guild subsystem files.

- Risk: excessive scope in one plan.
  - Mitigation: keep each plan at 2-3 tasks and split communication from storage.

## Conclusion

Phase 5 should be planned as a security-conscious social subsystem with clear server authority, explicit protocol contracts, and test-backed permission/storage invariants. The highest-risk work is guild storage and rank permission enforcement, so plans should establish lifecycle and permission foundations first, then build chat/roster and storage controls in parallel.
