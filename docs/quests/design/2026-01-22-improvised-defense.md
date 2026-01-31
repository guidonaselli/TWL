# Design: Equipment Introduction - Defensa Improvisada (Quest 1004)

> **JULES-CONTEXT**: This document details the first equipment quest. It introduces the
> crafting-via-gathering pattern: collect materials -> NPC crafts item -> equip reward.
> Reference: `2026-01-22-intro-arc.md` Quest 1004 for canonical definition.

**Quest ID:** 1004
**Mechanic Focus:** Equipment acquisition, BindOnAcquire, first weapon.

---

## Design Intent

This quest is the player's first contact with the equipment system. It must:
1. Teach that NPCs can craft items from gathered materials
2. Introduce the equipment slot concept (weapon)
3. Gate combat content behind having a weapon
4. Set up the pet sidequest branch (Quest 1008)

## Implementation Notes

### Server-Side
- Reward item `Wooden Sword` (ID: 103) must have:
  - `BindOnAcquire: true` (cannot be traded)
  - `UniquePerCharacter: true` (idempotent - re-completing doesn't grant duplicate)
  - `SlotType: Weapon`
  - `Stats: { ATK: 3 }`
- Validate via `PlayerQuestComponent.TryProgress` using `InteractRequest` opcode
- Collecting driftwood uses `Collect` objective type with `item_driftwood` target

### Client-Side
- After claiming reward, auto-open equipment panel showing the new weapon
- Play equip animation/sound feedback
- Show tooltip: "Press E to open Equipment"

### Interaction Flow
```
Player -> Talk to Ruk (npc_ruk) -> Quest accepted
Player -> Gather 5x Driftwood from beach objects
Player -> Talk to Ruk again -> Ruk "crafts" the sword
Server -> Grant Wooden Sword (103) -> Quest complete
Client -> Show equip tutorial prompt
```
