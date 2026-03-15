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

## Observability Impact

- **Log Signals**:
    - `InteractionManager`: `Compound NPC interaction triggered (PlayerId: {id}, NpcId: {id})` on access.
    - `ClientSession`: `Compound item selection validation failed (PlayerId: {id}, Reason: {reason})` on invalid request.
- **Inspection Surfaces**:
    - `Content/Data/interactions.json`: A new entry with `Type: "Compound"` can be inspected to see which NPCs are configured for compounding.

## Steps

1.  **Opcode Definition**: Add `CMSG_COMPOUND_REQUEST_START` and `SMSG_COMPOUND_REQUEST_START_ACK` to `Opcode.cs`.
2.  **Interaction Type**: Add `Compound` to `InteractionType.cs` enum and update `InteractionDefinition.cs` to include it.
3.  **Content Definition**: Add a new interaction in `Content/Data/interactions.json` with `Type: "Compound"` linked to an NPC.
4.  **Server Handler Stub**: Add a placeholder handler in `ClientSession.cs` for `CMSG_COMPOUND_REQUEST_START`.
5.  **Access Test**: Create `CompoundNpcAccessTests.cs` and write a test to verify a player can trigger the compound interaction from an NPC.
6.  **Selection Validation**: Implement logic in the `ClientSession` handler to validate that the items in the compound request exist in the player's inventory.
7.  **Validation Test**: Add a test to `CompoundNpcAccessTests.cs` to verify that invalid selections are rejected.
8.  **Finalize Handler**: Complete the server handler to return success (`SMSG_COMPOUND_REQUEST_START_ACK`) on valid access.

## Files

- `TWL.Shared/Net/Network/Opcode.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Shared/Domain/Interactions/InteractionDefinition.cs`
- `TWL.Server/Simulation/Managers/InteractionManager.cs`
- `Content/Data/interactions.json`
- `TWL.Tests/Compound/CompoundNpcAccessTests.cs`
