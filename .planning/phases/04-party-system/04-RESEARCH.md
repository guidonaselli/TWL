# Phase 4: Party System - Research

**Researched:** 2026-02-17
**Domain:** Multiplayer party orchestration (server authority, sync, rewards, chat, tactical formation)
**Confidence:** High

## Summary

Phase 4 requires building a mostly missing party system on top of existing combat/network infrastructure. The codebase already has partial signals of intended support (party message enums, `ServerCharacter.PartyId`, quest `PartyRules`), but lacks core server services, network opcode handlers, DTOs, and client UI/state for parties.

A targeted baseline test run for party-related tests currently fails:
- `dotnet test TWL.Tests/TWL.Tests.csproj --filter "FullyQualifiedName~Party|FullyQualifiedName~party"`
- Result: `1 failed, 2 passed`
- Failing test: `TWL.Tests.Quests.PvPAndGatingTests.Should_FailStartQuest_When_PartyRequired_And_NoParty`

This failure is consistent with code structure: `CanStartQuest` checks `PartyRules`, but `StartQuest` currently bypasses that branch.

## Current State Findings

### 1. Protocol intent exists, transport implementation does not

Observed:
- `TWL.Shared/Net/Messages/ClientMessageType.cs` includes party actions (invite/accept/decline/leave/kick).
- `TWL.Shared/Net/Messages/ServerMessageType.cs` includes party update events.
- `TWL.Shared/Net/Network/Opcode.cs` has no dedicated party opcodes.
- `TWL.Server/Simulation/Networking/ClientSession.cs` has no party request handlers.

Implication:
- Message enums alone are not wired into runtime packet handling.

### 2. Party domain model is minimal

Observed:
- `TWL.Server/Simulation/Networking/ServerCharacter.cs` has `PartyId` and `GuildId`.
- No `PartyManager` / `PartyService` exists in `TWL.Server/Simulation/Managers`.
- No party DTO/payload contracts exist in `TWL.Shared/Domain/DTO` or `TWL.Shared/Net/Payloads`.

Implication:
- Party lifecycle state (leader, members, invites, loot mode, formation grid) needs first-class server model.

### 3. Quest system already depends on party semantics

Observed:
- `QuestDefinition.PartyRules` supports `MustBeInParty`.
- `PlayerQuestComponent.CanStartQuest` checks party/guild rules.
- `PlayerQuestComponent.StartQuest` does not enforce the same party/guild checks.
- `TWL.Tests/Quests/PvPAndGatingTests.cs` validates this behavior and currently exposes the mismatch.

Implication:
- Party phase should include quest-gating parity fix so party-required quests are consistently enforced.

### 4. Combat/reward systems are single-player-oriented

Observed:
- `CombatManager` resolves combat and death events, but no party XP/loot split logic.
- `SpawnManager` and movement systems already track map/location data that can be reused for proximity checks.

Implication:
- PTY-04/PTY-05 can be implemented by adding party-aware reward distributor logic using existing map/position state.

### 5. Client currently lacks explicit party UX surfaces

Observed:
- `NetworkClient` publishes net messages through `EventBus`, enabling extension.
- Existing gameplay HUD (`UiGameplay`) has no party list/invite/formation panels.

Implication:
- PTY-06/PTY-07/PTY-08 require explicit client state + UI additions, not just server changes.

## Recommended Planning Shape

Create 4 executable plans:

1. `04-01` Party foundation + invite/accept/decline/leave/kick lifecycle (server-authoritative), plus quest-gating parity fix.
2. `04-02` Party XP and loot sharing with same-map/proximity enforcement and deterministic tests.
3. `04-03` Party sync + private party chat channel + client party HUD/member status updates.
4. `04-04` Tactical 3x3 formation model integrated into combat flow and party UI interactions.

Wave recommendation:
- Wave 1: `04-01`
- Wave 2: `04-02`, `04-03` (parallel, both depend on party foundation)
- Wave 3: `04-04` (depends on party foundation and client/server sync primitives)

## Verification Targets (Phase-level)

- Invite/accept/decline/leave/kick flows work end-to-end with leader and membership rules.
- Kick is blocked during combat/boss encounters.
- Party XP and loot share only apply when map + distance conditions pass.
- Party UI shows real-time member HP/MP/status and updates on membership changes.
- Party chat visibility is restricted to party members.
- Formation updates are persisted in party state and influence combat targeting/row semantics.

## Risks and Mitigations

- Risk: Protocol fragmentation between `ClientMessageType`, `ServerMessageType`, and `Opcode`.
  - Mitigation: define canonical party opcode+payload contracts in one plan and test serialization boundaries.

- Risk: Reward duplication/abuse in shared XP/loot.
  - Mitigation: centralize party reward distribution and cover idempotent distribution scenarios with tests.

- Risk: UI/server drift for party state.
  - Mitigation: treat server as source of truth and emit snapshot + delta update messages.

- Risk: Tactical formation over-scopes combat rewrite.
  - Mitigation: implement minimal 3x3 assignment and row effect hooks first; defer advanced AI tactics.

## Conclusion

Phase 4 should be executed as foundational social infrastructure with strict server authority. The architecture is prepared for extension but not implemented; plans should prioritize party lifecycle correctness first, then reward/chat/sync, and finally tactical formation integration.
