# Daily Quest Design: Brisa Island Continuation
**Date:** 2026-01-22
**Author:** Jules

## Objective
Expand the "Isla de la Brisa" tutorial arc by introducing a "Bonding" sidequest chain for the Monkey pet and a transition quest towards the interior of the island (Old Hermit).

## New Quests

### 1013: Monkey's Favorite Snack
*   **Type:** Sidequest (Pet Bonding)
*   **Prerequisite:** 1010 (Nursing to Health - Pet Unlock)
*   **Start NPC:** Survivalist
*   **Description:** The monkey seems restless. The Survivalist suggests feeding it its favorite fruit found in the palm groves.
*   **Objectives:**
    *   Collect 5 Bananas (Target: "BananaCluster")
*   **Rewards:**
    *   Exp: 50
    *   Item: 105 (Small Potion) x 2

### 1014: Playtime
*   **Type:** Sidequest (Pet Bonding)
*   **Prerequisite:** 1013
*   **Start NPC:** Survivalist
*   **Description:** The monkey is full and happy. Now it wants to play! Spend some time with it to strengthen your bond.
*   **Objectives:**
    *   Interact with Monkey (Target: "MyMonkey")
*   **Rewards:**
    *   Exp: 100
    *   Amity Increase (Conceptual - just EXP/Text for now)

### 1015: The Old Hermit
*   **Type:** Main Quest (Transition)
*   **Prerequisite:** 1012 (The Bully)
*   **Start NPC:** Captain
*   **Description:** With the Giant Crab defeated, the path inland is open. The Captain wants you to seek out the Old Hermit who lives near the ruins. He might know where we are.
*   **Objectives:**
    *   Talk to OldHermit (Target: "OldHermit")
*   **Rewards:**
    *   Exp: 200
    *   Gold: 50
    *   Unlock: "Map of Inland" (Conceptual)

## Technical Requirements
*   **Targets:** Ensure "BananaCluster" and "OldHermit" are valid targets in the client/map (or mocked in server check).
*   **Validation:** Use existing `Collect` and `Talk` / `Interact` types.
