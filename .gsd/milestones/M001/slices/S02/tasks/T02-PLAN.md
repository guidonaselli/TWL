# T02: 02-security-hardening 02

**Slice:** S02 — **Milestone:** M001

## Description

Implement server-side movement validation to block speed-hack and teleport-style movement payloads.

Purpose: This plan delivers SEC-01 by moving movement trust fully to the server with deterministic checks.
Output: Movement validator with policy controls, ClientSession integration, and tests proving valid vs invalid movement behavior.

## Must-Haves

- [ ] "Move requests exceeding max allowed distance per tick are rejected"
- [ ] "Accepted move requests still update character position and trigger world systems"
- [ ] "Rejected movement attempts generate security telemetry for investigation"

## Files

- `TWL.Server/Security/MovementValidationOptions.cs`
- `TWL.Server/Security/MovementValidator.cs`
- `TWL.Shared/Domain/DTO/MoveDTO.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Server/Security/SecurityLogger.cs`
- `TWL.Tests/Security/MovementValidatorTests.cs`
- `TWL.Tests/Security/ClientSessionMovementValidationTests.cs`
