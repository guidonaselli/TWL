# T01: 04-party-system 01

**Slice:** S04 — **Milestone:** M001

## Description

Implement the server-authoritative party lifecycle foundation (create/invite/accept/decline/leave/kick) and align quest gating with party membership rules.

Purpose: This establishes PTY-01, PTY-02, PTY-03, and PTY-09 as the base contract for all later party reward/chat/formation work.
Output: Party service + protocol DTOs + opcode handlers + deterministic lifecycle tests.

## Must-Haves

- [ ] "Players can create a party, invite members, and receive accept/decline outcomes"
- [ ] "Players can leave parties and leaders can kick members"
- [ ] "Kick attempts are rejected when target or leader is in combat/boss encounter"
- [ ] "Party-required quest gating is enforced consistently in quest start flow"

## Files

- `TWL.Server/Simulation/Managers/IPartyService.cs`
- `TWL.Server/Simulation/Managers/PartyManager.cs`
- `TWL.Shared/Domain/DTO/PartyDTOs.cs`
- `TWL.Shared/Net/Network/Opcode.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Server/Simulation/Program.cs`
- `TWL.Server/Simulation/Networking/Components/PlayerQuestComponent.cs`
- `TWL.Tests/Party/PartyLifecycleTests.cs`
- `TWL.Tests/Quests/PvPAndGatingTests.cs`
