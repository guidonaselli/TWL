# Design: Secrets of the Ruins (Region 2)

## Overview
Continuing the exploration of the "Hidden Ruins" discovered in Region 2. The player has just entered the ruins. This arc focuses on the mystery of the ruins and finding signs of previous inhabitants.

## Objectives
*   **Progression:** Advance the Region 2 main story.
*   **Mechanics:** Introduce "Collect" quests inside dungeons (Gathering from specific props).
*   **Atmosphere:** Spooky, ancient, technological mystery.

## Main Quest Chain: Secrets of the Ruins

### Quest 1205: Dark Corridors
*   **Prerequisite:** 1204 (Inside the Ruins) - Actually 1204 is "enter the ruins", so this picks up immediately.
*   **Start:** RuinsConsole (New interaction at entrance) or OldHermit (via radio/outside). Let's go with **RuinsConsole** to emphasize the tech.
*   **Description:** "The ruins are dark and full of sounds. The console near the entrance flickers. It seems to want you to clear the area."
*   **Objectives:**
    *   Kill 3 Ruins Bat
    *   Kill 1 Giant Spider
*   **Rewards:** EXP, Gold.

### Quest 1206: Ancient Power
*   **Prerequisite:** 1205
*   **Start:** RuinsConsole
*   **Description:** "The console stabilizes but indicates low power. It points to a glowing crystal cluster deeper in."
*   **Objectives:**
    *   Collect 1 Power Crystal (from "PowerCrystalSource")
*   **Rewards:** EXP, Gold.

### Quest 1207: The Hologram
*   **Prerequisite:** 1206
*   **Start:** RuinsConsole
*   **Description:** "With power restored, the main projector is ready. Activate it."
*   **Objectives:**
    *   Interact with HologramProjector
*   **Rewards:** EXP, Gold, **Data Chip** (Item 8109 - "Signs of Life").

## Sidequests

### Quest 2203: Bat Wings (Alchemy)
*   **Prerequisite:** 1205 (After seeing bats)
*   **Start:** Doctor (Region 1 Hub - assuming radio contact or return trip)
*   **Description:** "The bats in the ruins have unique wings useful for potions. Collect some."
*   **Objectives:**
    *   Collect 5 Bat Wings (from "BatNest" - easier than loot drops for now).
*   **Rewards:** Potions, EXP.

### Quest 2204: Lost Toy (Flavor/Amity)
*   **Prerequisite:** 1010 (Have Monkey)
*   **Start:** Monkey (Interaction? Or just finding it triggers it? Let's make it a finding quest).
*   **Description:** "You spot something fluffy in the corner. The monkey seems interested."
*   **Objectives:**
    *   Collect 1 Stuffed Bear (from "StuffedBearLocation")
    *   Interact with MyMonkey (Give toy)
*   **Rewards:** Pet Affinity (Simulated via text), EXP.

## New Assets

### Items
*   8106: Bat Wing (Material)
*   8107: Stuffed Bear (Quest Item)
*   8108: Power Crystal (Quest Item)
*   8109: Data Chip (Quest Item)

### Mobs
*   9003: Ruins Bat (Weak, flying)
*   9004: Giant Spider (Stronger, poison?)

### Interactions
*   RuinsConsole (Quest Giver)
*   PowerCrystalSource (Gather -> 8108)
*   HologramProjector (Interact)
*   BatNest (Gather -> 8106)
*   StuffedBearLocation (Gather -> 8107)
