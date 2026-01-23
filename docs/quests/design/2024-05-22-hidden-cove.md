# Design: The Hidden Cove Arc
**Date:** 2024-05-22
**Author:** Jules
**Status:** Draft

## Overview
Following the successful repair of the Radio Tower (Quest 1305), the survivor picks up a mysterious signal loop emanating from a previously inaccessible part of the island: The Hidden Cove. This arc introduces advanced gathering (Sulfur, Charcoal) and crafting (Black Powder) to clear obstacles, opening up a new sub-region.

## Progression
**Prerequisites:** Quest 1305 (First Contact) completed.

### Quest Chain: Echoes from the Sea
1.  **1401: Echoes from the Sea**
    *   **Description:** The radio is picking up a signal loop from the North-East cliffs. It's not coming from off-island.
    *   **Objective:** Interact with `RepairedRadioTower` to get coordinates.
    *   **Objective:** Travel to `NorthEastCliff`.
    *   **Reward:** 100 EXP.

2.  **1402: Blocked Passage**
    *   **Description:** A rockfall blocks the path down to the cove. You need something explosive to clear it.
    *   **Objective:** Interact with `HeavyRocks`.
    *   **Objective:** Collect 1 `Sulfur` from `SulfurVent` (Volcanic fringe).
    *   **Objective:** Collect 1 `Charcoal` from `BurntTree` (Jungle burn).
    *   **Objective:** Craft `BlackPowder` at `AlchemyTable`.
    *   **Reward:** 200 EXP, 1 Black Powder.

3.  **1403: Clearing the Way**
    *   **Description:** Use the Black Powder to blast the rocks.
    *   **Objective:** Interact with `HeavyRocks` (consumes Black Powder).
    *   **Reward:** 300 EXP, Access to Hidden Cove.

4.  **1404: The Hidden Dock**
    *   **Description:** The path is clear. Investigate the cove.
    *   **Objective:** Interact with `HiddenDock`.
    *   **Reward:** 500 EXP, 1 `OldKey` (Item 9004).

### Sidequest: Master Fisherman
1.  **2401: Master Fisherman**
    *   **Prerequisites:** 2011 (First Catch).
    *   **Description:** The cove waters are teeming with rare life. Catch a Rare Blue Fish.
    *   **Objective:** Catch 1 `RareBlueFish` from `FishingSpot`.
    *   **Reward:** 150 EXP, 50 Gold.

## New Items
*   **Sulfur (9001):** Material.
*   **Charcoal (9002):** Material.
*   **Black Powder (9003):** Item/Quest.
*   **Old Key (9004):** Quest Item.
*   **Rare Blue Fish (9005):** Material.

## New Interactions
*   **RepairedRadioTower:** Triggers 1401.
*   **NorthEastCliff:** Progress 1401.
*   **HeavyRocks:** Progress 1402 (Fail), 1403 (Success).
*   **SulfurVent:** Gather Sulfur.
*   **BurntTree:** Gather Charcoal.
*   **HiddenDock:** Complete 1404.
