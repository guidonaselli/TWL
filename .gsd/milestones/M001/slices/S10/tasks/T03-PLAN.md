# T03: 10-combat-progression-integration 03

**Slice:** S10 — **Milestone:** M001

## Description

Implement per-instance daily run limits with UTC reset and entry rejection to satisfy `INST-01`, `INST-02`, and `INST-03`.

Purpose: Prevent unlimited instance farming by enforcing server-authoritative daily quotas.
Output: Run-counter persistence model, quota-aware instance admission, and quota regression tests.

## Must-Haves

- [ ] "Instance runs are tracked per character per instance with daily cap of 5"
- [ ] "Run counters reset at 00:00 UTC and do not use local server timezone"
- [ ] "Instance entry is rejected when daily quota is exhausted (5/5)"

## Files

- `TWL.Server/Persistence/PlayerSaveData.cs`
- `TWL.Server/Simulation/Networking/ServerCharacter.cs`
- `TWL.Server/Services/InstanceService.cs`
- `TWL.Server/Services/World/Actions/Handlers/EnterInstanceActionHandler.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Tests/Server/Instances/InstanceRunLimitTests.cs`
