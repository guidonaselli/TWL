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

## Steps

1. Inspect the existing combat death and character EXP mutation paths in `CombatManager`, `ServerCharacter`, and related server wiring to locate the authoritative hook for player-death processing.
2. Implement `DeathPenaltyService` so it computes exactly 1% of current-level EXP, floors at zero, and reports whether a death event was applied or ignored as a duplicate.
3. Wire the service into server-side combat death handling so penalties are triggered from combat events only and cannot be applied through a client-request path.
4. Add focused regression tests covering exact 1% loss, floor-to-zero behavior, and idempotent duplicate-death suppression.
5. Run the slice verification commands, capture pass/fail status, and update slice/task artifacts with the concrete diagnostics surfaces introduced.

## Observability Impact

- The new service result must expose enough state for tests and future agents to inspect penalty amount, EXP before/after, and duplicate-event suppression without reproducing combat manually.
- Failure state becomes visible through focused regression tests that assert exact EXP transitions and unchanged state on duplicate handling.
- No secrets or player PII should be emitted; diagnostics stay limited to EXP values, event identity, and duplicate/applied flags.
