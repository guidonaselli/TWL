# Phase 10: Combat Progression Integration Verification

**Status**: Verified
**Date**: $(date -u +"%Y-%m-%d")
**Milestone**: M001
**Slice**: S10

This document tracks the acceptance criteria for Phase 10 (Combat Progression Integration) mapped to roadmap requirements. All requirements have executable test coverage.

## CMB-01 / CMB-02: Death Penalty (EXP & Durability Loss)
* **Requirement**: Player death results in 1% EXP loss and -1 Durability to all equipped items.
* **Acceptance Test**: `Requirement_CMB01_02_DeathPenaltyAppliesExactlyOnePercentExpAndMinusOneDurability`
* **Status**: PASS
* **Evidence**: The acceptance test explicitly verifies a 1% calculation, a -1 decrement to equipment durability, and state transitions to `IsBroken` for items reaching 0 durability.

## INST-01 / INST-02 / INST-03: Instance Daily Limits
* **Requirement**: Players are capped at 5 daily runs per instance. Resets at UTC midnight. Server-authoritative enforcement.
* **Acceptance Test**: `Requirement_INST01_02_03_InstanceDailyLimitsEnforcedWithUtcReset`
* **Status**: PASS
* **Evidence**: The acceptance test ensures `InstanceService.DailyLimit` is exactly 5. It confirms 5 successful entries and 1 rejection for the 6th attempt on the same UTC date. Simulating a UTC day rollover immediately permits entry again and clears the run counter.

## CMB-04: Combat Flow Integration
* **Requirement**: Integration of death penalties, pet AI, and status processing.
* **Acceptance Test**: `CombatFlowIntegrationTests` suite
* **Status**: PASS
* **Evidence**: Combat death successfully triggers penalties and removes players from turn calculation without crashing pet AI turns or status processing loops. Out-of-combat utility behaviors accurately track in-combat states.

All phase acceptance tests are passing, clearing the requirements for Phase 10 integration.
