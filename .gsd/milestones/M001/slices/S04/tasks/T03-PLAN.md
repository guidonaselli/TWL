# T03: 04-party-system 03

**Slice:** S04 — **Milestone:** M001

## Description

Deliver real-time party state synchronization and private party chat with client-side party HUD support.

Purpose: This plan covers PTY-06 and PTY-07 by connecting server party events to user-visible party state and communication surfaces.
Output: Party chat service, party sync handlers, party UI panel, and deterministic sync/chat tests.

## Must-Haves

- [ ] "Party members see real-time member roster and HP/MP/status updates in gameplay UI"
- [ ] "Party chat messages are visible only to members of the same party"
- [ ] "Party membership changes are reflected on client UI without stale members"

## Files

- `TWL.Server/Simulation/Managers/PartyChatService.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Shared/Domain/DTO/PartyChatDTOs.cs`
- `TWL.Client/Presentation/Managers/GameClientManager.cs`
- `TWL.Client/Presentation/UI/UiPartyWindow.cs`
- `TWL.Client/Presentation/UI/UiGameplay.cs`
- `TWL.Tests/Party/PartyChatChannelTests.cs`
- `TWL.Tests/Party/PartyStateSyncTests.cs`
