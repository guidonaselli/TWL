# Design: Camp Establishment - Fuego y Refugio (Quest 1005)

> **JULES-CONTEXT**: This document details the camp establishment quest. It introduces
> multi-objective quests and cross-zone exploration. The camp is a narrative checkpoint,
> not a mechanical housing unlock (housing comes later in Puerto Roca).
> Reference: `2026-01-22-intro-arc.md` Quest 1005 for canonical definition.

**Quest ID:** 1005
**Mechanic Focus:** Multi-objective quests, zone transitions, gathering from specific maps.

---

## Design Intent

This quest teaches players that:
1. Quests can have multiple objectives across different maps
2. Different zones contain different resources
3. The camp is a safe haven that grows with the player's actions
4. Exploration is rewarded with narrative progress

## Objective Breakdown

| # | Type | Target | Count | Map | Notes |
|---|------|--------|-------|-----|-------|
| 1 | Interact | `obj_stream` | 1 | 0003 | Fresh water source in jungle edge |
| 2 | Collect | `item_kindling` | 3 | 0003 | Dry wood from dead trees |
| 3 | Collect | `item_flint` | 2 | 0002 | Flint stones from rocky shore |

## Implementation Notes

### Camp Progression (Narrative Flag)
- On completion, set flag `camp_established = true`
- This flag changes the visual state of Map 0001:
  - Campfire appears (was just a pit before)
  - Survivor NPCs change dialogue to reference the camp
  - New interaction objects become available (cooking pot, workbench)
- This is purely cosmetic/narrative - no mechanical housing unlock yet

### Server-Side
- All objectives must be completed before turn-in is allowed
- Objective order does not matter (player can do them in any sequence)
- Items are consumed on quest completion (removed from inventory)

### Client-Side
- Quest tracker should show all 3 objectives with individual progress
- Map markers on both Map 0002 and 0003 for active objectives
- On completion: camera pan to campfire igniting (cinematic moment)
