# T02: 05-guild-system 02

**Slice:** S05 — **Milestone:** M001

## Description

Implement guild rank hierarchy and permission enforcement for privileged guild operations.

Purpose: This delivers GLD-04 and provides the security policy baseline required by chat moderation and guild storage access.
Output: Permission service + rank DTOs + promote/demote handlers + deterministic rank/permission tests.

## Must-Haves

- [ ] "Guilds support hierarchical ranks with explicit permission sets"
- [ ] "Only authorized ranks can invite, promote, kick, and grant storage withdrawal access"
- [ ] "Rank changes are validated server-side and synchronized to guild members"

## Files

- `TWL.Server/Simulation/Managers/GuildPermissionService.cs`
- `TWL.Server/Simulation/Managers/GuildManager.cs`
- `TWL.Shared/Domain/DTO/GuildRankDTOs.cs`
- `TWL.Shared/Net/Network/Opcode.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Server/Simulation/Networking/ServerCharacter.cs`
- `TWL.Tests/Guild/GuildRankHierarchyTests.cs`
- `TWL.Tests/Guild/GuildPermissionsTests.cs`
