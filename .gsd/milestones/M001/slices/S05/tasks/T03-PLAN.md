# T03: 05-guild-system 03

**Slice:** S05 — **Milestone:** M001

## Description

Implement guild communication and roster visibility, including offline-safe chat persistence and client sync.

Purpose: This delivers GLD-05 and GLD-09 so guilds become an operational social channel rather than a hidden server-only structure.
Output: Guild chat + roster services, DTO contracts, client guild UI integration, and deterministic roster/chat tests.

## Must-Haves

- [ ] "Guild chat is visible only to guild members and survives offline member periods"
- [ ] "Guild roster shows member rank, online status, and last-login metadata"
- [ ] "Client guild UI reflects roster and chat updates without stale cross-guild leakage"

## Files

- `TWL.Server/Simulation/Managers/GuildChatService.cs`
- `TWL.Server/Simulation/Managers/GuildRosterService.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Server/Persistence/Services/PlayerService.cs`
- `TWL.Shared/Domain/DTO/GuildChatDTOs.cs`
- `TWL.Shared/Domain/DTO/GuildRosterDTOs.cs`
- `TWL.Client/Presentation/Managers/GameClientManager.cs`
- `TWL.Client/Presentation/UI/UiGuildWindow.cs`
- `TWL.Tests/Guild/GuildChatChannelTests.cs`
- `TWL.Tests/Guild/GuildRosterSyncTests.cs`
