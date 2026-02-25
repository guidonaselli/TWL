# Plan 04-02 Summary: Party Rewards & Proximity Rules

## Implementation Details

### Domain & Contracts
- Added `NextLootMemberIndex` to `Party` class in `IPartyService.cs` to support persistent round-robin loot distribution.
- Created `PartyRewardDistributor` service to handle XP splitting and loot assignment logic.

### Logic
- **XP Sharing:** XP is split evenly among eligible party members.
- **Proximity Rule:** Members are eligible ONLY if they are on the same Map AND within 30.0f units of the victim.
- **Loot Distribution:** Loot follows a round-robin strategy using the `NextLootMemberIndex`, cycling through eligible members.
- **Fallback:** If no party members are eligible (e.g., all far away), the beneficiary (killer/owner) receives the reward as a fallback to ensure rewards aren't lost in edge cases.

### Infrastructure
- Registered `PartyRewardDistributor` as a Singleton in `Program.cs`.
- Updated `GameServer.cs` to instantiate `PartyRewardDistributor` and pass it to `CombatManager`.
- Updated `CombatManager` to inject `PartyRewardDistributor` (optionally, to preserve test compatibility) and trigger distribution on `UseSkill` when a target dies.

## Verification
- **Unit Tests:**
  - `PartyRewardDistributionTests`: Verified solo rewards, XP splitting, and round-robin loot rotation.
  - `PartyProximityRulesTests`: Verified that members on different maps or outside the distance threshold (30.0f) are excluded from rewards.
  - *Note:* Adjusted tests to award 50 XP (instead of 100) to avoid triggering a Level Up in `ServerCharacter`, which resets `Exp` to 0 and caused assertion failures.

## Learnings
- **Level Up Side Effects:** Testing XP accumulation requires awareness of level-up thresholds. `ServerCharacter` resets `Exp` to 0 upon leveling up.
- **Backward Compatibility:** Making new dependencies optional in constructors (e.g., `CombatManager`) saves significant effort by not breaking hundreds of existing tests that don't need the new functionality.
