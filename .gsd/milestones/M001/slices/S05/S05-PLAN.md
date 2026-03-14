# S05: Guild System

**Goal:** Implement the guild lifecycle foundation: guild creation, membership invite flow, leave/kick flow, and core network contracts.
**Demo:** Implement the guild lifecycle foundation: guild creation, membership invite flow, leave/kick flow, and core network contracts.

## Must-Haves


## Tasks

- [x] **T01: 05-guild-system 01**
  - Implement the guild lifecycle foundation: guild creation, membership invite flow, leave/kick flow, and core network contracts.

Purpose: This establishes GLD-01, GLD-02, and GLD-03 while creating the protocol backbone needed for rank, chat, and storage plans.
Output: Guild service + guild DTOs/opcodes + server session handlers + deterministic lifecycle tests.
- [x] **T02: 05-guild-system 02**
  - Implement guild rank hierarchy and permission enforcement for privileged guild operations.

Purpose: This delivers GLD-04 and provides the security policy baseline required by chat moderation and guild storage access.
Output: Permission service + rank DTOs + promote/demote handlers + deterministic rank/permission tests.
- [x] **T03: 05-guild-system 03**
  - Implement guild communication and roster visibility, including offline-safe chat persistence and client sync.

Purpose: This delivers GLD-05 and GLD-09 so guilds become an operational social channel rather than a hidden server-only structure.
Output: Guild chat + roster services, DTO contracts, client guild UI integration, and deterministic roster/chat tests.
- [x] **T04: 05-guild-system 04**
  - Implement guild shared storage with permission-gated, tenure-safe withdrawals and durable audit logging.

Purpose: This plan completes GLD-06, GLD-07, and GLD-08 with safe shared-storage behavior for production guild operations.
Output: Guild storage service, withdrawal tenure/policy enforcement, audit log flow, and deterministic storage regression tests.

## Files Likely Touched

- `TWL.Server/Simulation/Managers/IGuildService.cs`
- `TWL.Server/Simulation/Managers/GuildManager.cs`
- `TWL.Shared/Domain/DTO/GuildDTOs.cs`
- `TWL.Shared/Net/Messages/ClientMessageType.cs`
- `TWL.Shared/Net/Messages/ServerMessageType.cs`
- `TWL.Shared/Net/Network/Opcode.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Server/Simulation/Program.cs`
- `TWL.Tests/Guild/GuildLifecycleTests.cs`
- `TWL.Server/Simulation/Managers/GuildPermissionService.cs`
- `TWL.Server/Simulation/Managers/GuildManager.cs`
- `TWL.Shared/Domain/DTO/GuildRankDTOs.cs`
- `TWL.Shared/Net/Network/Opcode.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Server/Simulation/Networking/ServerCharacter.cs`
- `TWL.Tests/Guild/GuildRankHierarchyTests.cs`
- `TWL.Tests/Guild/GuildPermissionsTests.cs`
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
