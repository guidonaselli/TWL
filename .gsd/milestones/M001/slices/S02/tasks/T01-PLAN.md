# T01: 02-security-hardening 01

**Slice:** S02 — **Milestone:** M001

## Description

Implement packet replay protection using nonce + timestamp validation with a strict 30-second window.

Purpose: This plan delivers SEC-02 and establishes the packet trust boundary all later hardening depends on.
Output: NetMessage carries replay metadata, ClientSession validates replay/freshness before handler dispatch, and security tests prove duplicate/stale rejection.

## Must-Haves

- [ ] "A duplicate packet nonce for the same session is rejected and never reaches opcode handlers"
- [ ] "A packet older than 30 seconds is rejected"
- [ ] "A fresh packet with unique nonce still processes normally"
- [ ] "Replay rejections are logged with correlation metadata"

## Files

- `TWL.Shared/Net/Network/NetMessage.cs`
- `TWL.Server/Security/ReplayGuard.cs`
- `TWL.Server/Security/ReplayGuardOptions.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Server/Simulation/Networking/NetworkServer.cs`
- `TWL.Server/Simulation/Program.cs`
- `TWL.Tests/Security/ReplayGuardTests.cs`
- `TWL.Tests/Security/ClientSessionReplayProtectionTests.cs`
