# T02: 04-party-system 02

**Slice:** S04 — **Milestone:** M001

## Description

Implement party XP and loot sharing with strict same-map/proximity enforcement and deterministic anti-duplication behavior.

Purpose: This delivers PTY-04 and PTY-05 so party play affects progression and rewards correctly without exploit windows.
Output: Shared reward distributor integrated with combat outcomes and fully covered by focused reward/proximity tests.

## Must-Haves

- [ ] "Party members share XP only when on the same map and within configured proximity range"
- [ ] "Party loot distribution follows configured party policy (round-robin or need/greed baseline flow)"
- [ ] "Rewards are distributed once per encounter outcome without duplication"

## Files

- `TWL.Server/Simulation/Managers/PartyManager.cs`
- `TWL.Server/Simulation/Managers/PartyRewardDistributor.cs`
- `TWL.Server/Simulation/Managers/CombatManager.cs`
- `TWL.Server/Persistence/Services/PlayerService.cs`
- `TWL.Server/Simulation/Networking/ServerCharacter.cs`
- `TWL.Tests/Party/PartyRewardDistributionTests.cs`
- `TWL.Tests/Party/PartyProximityRulesTests.cs`
