# S04: Party System

**Goal:** Implement the server-authoritative party lifecycle foundation (create/invite/accept/decline/leave/kick) and align quest gating with party membership rules.
**Demo:** Implement the server-authoritative party lifecycle foundation (create/invite/accept/decline/leave/kick) and align quest gating with party membership rules.

## Must-Haves


## Tasks

- [x] **T01: 04-party-system 01**
  - Implement the server-authoritative party lifecycle foundation (create/invite/accept/decline/leave/kick) and align quest gating with party membership rules.

Purpose: This establishes PTY-01, PTY-02, PTY-03, and PTY-09 as the base contract for all later party reward/chat/formation work.
Output: Party service + protocol DTOs + opcode handlers + deterministic lifecycle tests.
- [x] **T02: 04-party-system 02**
  - Implement party XP and loot sharing with strict same-map/proximity enforcement and deterministic anti-duplication behavior.

Purpose: This delivers PTY-04 and PTY-05 so party play affects progression and rewards correctly without exploit windows.
Output: Shared reward distributor integrated with combat outcomes and fully covered by focused reward/proximity tests.
- [ ] **T03: 04-party-system 03**
  - Deliver real-time party state synchronization and private party chat with client-side party HUD support.

Purpose: This plan covers PTY-06 and PTY-07 by connecting server party events to user-visible party state and communication surfaces.
Output: Party chat service, party sync handlers, party UI panel, and deterministic sync/chat tests.
- [x] **T04: 04-party-system 04**
  - Deliver tactical formation support with server validation and combat row integration.

Purpose: This plan completes PTY-08 by adding formation-state management, validation, and combat-facing row semantics.
Output: Formation DTOs, server validation, combat integration, and tactical formation regression tests.

## Files Likely Touched

- `TWL.Server/Simulation/Managers/IPartyService.cs`
- `TWL.Server/Simulation/Managers/PartyManager.cs`
- `TWL.Shared/Domain/DTO/PartyDTOs.cs`
- `TWL.Shared/Net/Network/Opcode.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Server/Simulation/Program.cs`
- `TWL.Server/Simulation/Networking/Components/PlayerQuestComponent.cs`
- `TWL.Tests/Party/PartyLifecycleTests.cs`
- `TWL.Tests/Quests/PvPAndGatingTests.cs`
- `TWL.Server/Simulation/Managers/PartyManager.cs`
- `TWL.Server/Simulation/Managers/PartyRewardDistributor.cs`
- `TWL.Server/Simulation/Managers/CombatManager.cs`
- `TWL.Server/Persistence/Services/PlayerService.cs`
- `TWL.Server/Simulation/Networking/ServerCharacter.cs`
- `TWL.Tests/Party/PartyRewardDistributionTests.cs`
- `TWL.Tests/Party/PartyProximityRulesTests.cs`
- `TWL.Server/Simulation/Managers/PartyChatService.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Shared/Domain/DTO/PartyChatDTOs.cs`
- `TWL.Client/Presentation/Managers/GameClientManager.cs`
- `TWL.Client/Presentation/UI/UiPartyWindow.cs`
- `TWL.Client/Presentation/UI/UiGameplay.cs`
- `TWL.Tests/Party/PartyChatChannelTests.cs`
- `TWL.Tests/Party/PartyStateSyncTests.cs`
