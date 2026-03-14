---
id: T02
parent: S04
milestone: M001
provides: []
requires: []
affects: []
key_files: []
key_decisions: []
patterns_established: []
observability_surfaces: []
drill_down_paths: []
duration:
verification_result: passed
completed_at:
blocker_discovered: false
---
# T02: 04-party-system 02

## Summary

### Plan 04-02 — Party Rewards & Proximity Rules
Implemented party XP and loot sharing with same-map and proximity enforcement.

#### Domain & Contracts
- Added `NextLootMemberIndex` to the party model for persistent round-robin loot distribution.
- Created `PartyRewardDistributor` to handle XP splitting and loot assignment.

#### Logic
- XP is split evenly among eligible party members.
- Members are eligible only if they are on the same map and within `30.0f` units of the victim.
- Loot uses round-robin distribution through `NextLootMemberIndex`.
- If no members are eligible, rewards fall back to the beneficiary so nothing is lost.

#### Infrastructure
- Registered `PartyRewardDistributor` in `Program.cs`.
- Updated `GameServer.cs` and `CombatManager` to route reward distribution on kill resolution.

#### Verification
- `PartyRewardDistributionTests` verified solo rewards, XP splitting, and round-robin loot rotation.
- `PartyProximityRulesTests` verified map and distance eligibility.
- Tests were tuned to avoid false failures from level-up side effects resetting `Exp`.
