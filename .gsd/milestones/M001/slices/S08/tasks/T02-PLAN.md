# T02: 08-compound-system 02

**Slice:** S08 — **Milestone:** M001

## Description

Implement compound NPC access and inventory selection validation pipeline.

Purpose: This delivers CMP-01 and CMP-02 by exposing a server-authoritative entry path for compound requests.
Output: Compound opcodes, session handlers, interaction definitions, and NPC-access/selection validation tests.

## Must-Haves

- [ ] "Players can access compound flow from configured compound NPC targets"
- [ ] "Compound request rejects invalid base-item/material selections that do not exist in player inventory/equipment"
- [ ] "Compound entry and request payloads are network-addressable via explicit opcodes"

## Files

- `TWL.Shared/Net/Network/Opcode.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Shared/Domain/Interactions/InteractionDefinition.cs`
- `TWL.Server/Simulation/Managers/InteractionManager.cs`
- `Content/Data/interactions.json`
- `TWL.Tests/Compound/CompoundNpcAccessTests.cs`
