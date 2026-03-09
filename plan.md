1. **Create `TWL.Shared/Domain/DTO/GuildDTOs.cs`**
   - Contains request/response models for guild creation, invitations, kicks, etc.
   - `CreateGuildRequest`, `CreateGuildResponse`, `GuildInviteRequest`, `GuildInviteResponse`, `GuildAcceptInviteRequest`, `GuildDeclineInviteRequest`, `GuildLeaveRequest`, `GuildKickRequest`.
   - `GuildMemberBroadcast`, etc.

2. **Add Guild OpCodes to `TWL.Shared/Net/Network/Opcode.cs`**
   - Add `GuildCreateRequest`, `GuildCreateResponse`, `GuildInviteRequest`, `GuildInviteResponse`, `GuildInviteReceived`, `GuildAcceptInvite`, `GuildDeclineInvite`, `GuildLeaveRequest`, `GuildKickRequest`, `GuildKickResponse`, `GuildUpdateBroadcast`.

3. **Add Guild MessageTypes**
   - In `TWL.Shared/Net/Messages/ClientMessageType.cs`: Add `CreateGuild`, `RequestGuildInvite`, `AcceptGuildInvite`, `DeclineGuildInvite`, `LeaveGuild`, `KickFromGuild`.
   - In `TWL.Shared/Net/Messages/ServerMessageType.cs`: Add `PlayerGuildUpdated`, `PlayerGuildInvite`, `PlayerGuildAccepted`, `PlayerGuildDeclined`, `PlayerGuildLeft`, `PlayerGuildKicked`.

4. **Create `TWL.Server/Simulation/Managers/IGuildService.cs` and `TWL.Server/Simulation/Managers/GuildManager.cs`**
   - Implement guild creation logic: validate unique name, check creation fee (e.g. 50000 gold).
   - Implement invite logic: check if target online, send invite. Add pending invite tracking.
   - Implement accept/decline logic: add to guild, broadcast updates. Set `Character.GuildId`.
   - Implement leave/kick logic: remove from guild, broadcast updates. Clear `Character.GuildId`.
   - Tie into `QuestComponent.HandleGuildAction` for creating/joining/leaving/kicking.

5. **Wire handlers in `TWL.Server/Simulation/Networking/ClientSession.cs`**
   - Add handlers for all new guild opcodes mapping to `GuildManager` methods.
   - Ensure quest hooks are called.

6. **Register dependencies in `TWL.Server/Simulation/Program.cs`**
   - Register `IGuildService` -> `GuildManager` as a Singleton.

7. **Create deterministic tests in `TWL.Tests/Guild/GuildLifecycleTests.cs`**
   - Test guild creation (success/insufficient gold/duplicate name).
   - Test invite/accept flow.
   - Test invite/decline flow.
   - Test leave flow.
   - Test kick flow.

8. **Pre-commit Steps**
   - Complete pre commit steps to ensure proper testing, verification, review, and reflection are done.

9. **Submit**
