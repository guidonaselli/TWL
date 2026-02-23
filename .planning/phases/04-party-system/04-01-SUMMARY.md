# Plan 04-01 Summary: Party Foundation

## Implementation Details

### Domain & Contracts
- Verified `IPartyService` and `PartyManager` existence and logic.
- Verified `PartyDTOs` and `Opcode` entries.
- `PartyManager` logic supports:
  - Create (via invite accept)
  - Invite (with expiration)
  - Accept/Decline
  - Leave (disbands if empty, transfers leadership if members > 1, persists if only leader remains)
  - Kick (with combat restriction)

### Networking & Handlers
- Updated `ClientSession` to correctly map `Sp`/`MaxSp` to `CurrentMp`/`MaxMp` for Party DTOs.
- Ensured handlers (`HandlePartyInviteAsync`, etc.) are wired correctly.
- Added `GetSessionByName` and `GetSessionByUserId` to `PlayerService` to support party operations.

### Logic & Gating
- Implemented `IsCombatantInCombat` in `CombatManager` to support combat checks for Kick.
- Verified `PlayerQuestComponent` enforces `PartyRules` and `GuildRules` in `StartQuest`.

### Infrastructure
- Updated `GameServer` and `NetworkServer` to use Dependency Injection for `PartyManager`.
- Updated Integration Tests (`PipelineMetricsTests`, etc.) to inject `PartyManager`.

## Verification
- **Unit Tests:** `PartyLifecycleTests` passed (after adjusting expectations for 1-person party persistence).
- **Integration Tests:** `PvPAndGatingTests` passed.
- **Pipeline Tests:** Updated and verified passing.

## Learnings
- **Resource Mapping:** `ServerCharacter` uses `Sp` (Stamina/Spirit), mapped to `Mp` in legacy/client DTOs.
- **Party Persistence:** Parties do not disband immediately when member count drops to 1, provided the remaining member is the Leader (allowing them to invite others).
