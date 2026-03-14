# T01: 05-guild-system 01

**Slice:** S05 — **Milestone:** M001

## Description

Implement the guild lifecycle foundation: guild creation, membership invite flow, leave/kick flow, and core network contracts.

Purpose: This establishes GLD-01, GLD-02, and GLD-03 while creating the protocol backbone needed for rank, chat, and storage plans.
Output: Guild service + guild DTOs/opcodes + server session handlers + deterministic lifecycle tests.

## Must-Haves

- [ ] "Players can create guilds with unique names and configured creation fee enforcement"
- [ ] "Players can invite others to guild and receive accept/decline outcomes"
- [ ] "Guild members can leave and authorized guild members can kick members"

## Files

- `TWL.Server/Simulation/Managers/IGuildService.cs`
- `TWL.Server/Simulation/Managers/GuildManager.cs`
- `TWL.Shared/Domain/DTO/GuildDTOs.cs`
- `TWL.Shared/Net/Messages/ClientMessageType.cs`
- `TWL.Shared/Net/Messages/ServerMessageType.cs`
- `TWL.Shared/Net/Network/Opcode.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Server/Simulation/Program.cs`
- `TWL.Tests/Guild/GuildLifecycleTests.cs`
