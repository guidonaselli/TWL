# Quest Design: Signs of Life (Region 2)

**Date:** 2024-05-22
**Arc:** Signs of Life
**Region:** Region 2 (Jungle Fringe / Hidden Ruins) -> Region 3 (Transition)
**Level Range:** 25-30

## Overview
After activating the hologram in the Hidden Ruins (`1207`), the player decipher the data to reveal a distress signal from a distant survivor group. This arc focuses on the "Castaway" vs "Survivor" theme, moving from isolation to finding others.

## Progression Goals
*   **System:** Introduction of "Radio/Tech" interaction (repair mechanics).
*   **Narrative:** The player is not alone. There is a larger group of survivors.
*   **Feel:** WLO-like "get items to fix vehicle/tech" loop.

## Main Quests

### 1301 - Deciphering the Map
*   **Prerequisite:** 1207 (The Hologram)
*   **Start NPC:** Ruins Console (Interaction)
*   **Objective:** The hologram displays a map with a blinking light. Download/Copy the map data.
*   **Action:** Interact with `HologramConsole` again.
*   **Reward:** Item `8110` (Map Data), EXP.

### 1302 - The Signal
*   **Prerequisite:** 1301
*   **Start NPC:** Auto/System (on item get)
*   **Objective:** The map points to a high peak in the jungle. Travel there.
*   **Action:** Interact with `SignalPeak` (Location marker).
*   **Reward:** EXP.

### 1303 - Radio Silence
*   **Prerequisite:** 1302
*   **Start NPC:** Signal Peak (System)
*   **Objective:** You find an old radio tower, broken and rusted. Inspect it.
*   **Action:** Interact with `BrokenRadioTower`.
*   **Reward:** EXP, Gold.

### 1304 - Repair Job
*   **Prerequisite:** 1303
*   **Start NPC:** Broken Radio Tower (Interaction)
*   **Objective:** The radio needs parts. Scavenge nearby piles and find a power source (Battery Core) from local electric wildlife.
*   **Action:**
    *   Collect 3 `Broken Radio Part` (`8111`) from `ScrapPile`.
    *   Collect 1 `Battery Core` (`8112`) from `Electric Eel` (Mob drop/Kill).
*   **Reward:** EXP.

### 1305 - First Contact
*   **Prerequisite:** 1304
*   **Start NPC:** Broken Radio Tower
*   **Objective:** Repair the radio and send a signal.
*   **Action:** Interact with `BrokenRadioTower` (consumes items).
*   **Reward:** EXP, Gold, "Region 3" unlock hint.

## Sidequests

### 2301 - Scavenger Hunt
*   **Trigger:** Found a note in the scrap pile? Or just exploration.
*   **Objective:** Find the hidden supply cache mentioned in a torn logbook.
*   **Action:** Gather `HiddenSupplies` (`8113`).
*   **Reward:** Consumables (Potions).

### 2302 - Battery Power
*   **Trigger:** Survivalist (if exists) or Self-start after finding first Battery Core.
*   **Objective:** The electric eels are dangerous but useful. Hunt them down.
*   **Action:** Kill 5 `Electric Eel`.
*   **Reward:** Gold, XP.

## Technical Requirements
*   **Items:** Map Data (`8110`), Radio Part (`8111`), Battery Core (`8112`), Hidden Supplies (`8113`).
*   **Mobs:** Electric Eel (ID `9005`).
*   **Interactions:** `HologramConsole` (update/reuse), `SignalPeak`, `BrokenRadioTower`, `ScrapPile`, `HiddenSuppliesLocation`.
