# T01: 10-combat-progression-integration 01

**Slice:** S10 — **Milestone:** M001

## Description

Implement death-penalty EXP loss on player death (`CMB-01` partial) using server-authoritative combat event handling.

Purpose: Ensure combat deaths produce deterministic, policy-compliant progression penalties.
Output: DeathPenaltyService, ServerCharacter EXP penalty mutation, and focused regression tests.

## Must-Haves

- [ ] "Player death applies exactly 1% EXP loss based on current-level EXP, clamped at floor 0"
- [ ] "Death penalties are server-authoritative and tied to combat death events, not client requests"
- [ ] "Death processing is idempotent per death event and does not double-penalize"

## Files

- `TWL.Server/Simulation/Managers/CombatManager.cs`
- `TWL.Server/Simulation/Networking/ServerCharacter.cs`
- `TWL.Server/Services/Combat/DeathPenaltyService.cs`
- `TWL.Server/Simulation/ServerWorker.cs`
- `TWL.Tests/Server/Combat/DeathPenaltyServiceTests.cs`
