# Jungle Fringe Quest Design (2024-05-23)

## Overview
**Level Range:** 10-14
**Theme:** Entering the dangerous jungle, encountering poisonous flora and predatory fauna.
**Prerequisite:** Crash Site Arc (Quests 1001-1018)

## Quests

### 1101: Into the Green
*   **Type:** Main
*   **Start NPC:** OldHermit
*   **Prerequisite:** 1018 (Cutting the Path)
*   **Description:** The vines are cleared. Confirm with the Hermit and step into the Jungle Fringe.
*   **Objectives:**
    *   Talk to OldHermit ("Path is clear?")
    *   Interact with JungleEntrance ("Travel to Jungle Fringe")
*   **Rewards:** 100 EXP

### 1102: Poisonous Flora
*   **Type:** Main
*   **Start NPC:** Auto-start (or Doctor via Radio/Note) - *For now, chain from 1101*.
*   **Description:** The jungle plants look dangerous. Collect samples of Poison Ivy for the Doctor to study.
*   **Objectives:**
    *   Collect 3 PoisonIvySample (from PoisonIvyBush)
*   **Rewards:** 200 EXP, 50 Gold, 1x Detoxification Potion (ID 7523)

### 1103: Jungle Predator
*   **Type:** Main
*   **Start NPC:** Survivalist
*   **Description:** A Jaguar has been stalking the new path. It must be dealt with to ensure safety.
*   **Objectives:**
    *   Kill 1 Jaguar
*   **Rewards:** 300 EXP, 100 Gold, 1x Raptor Tooth (ID 8003)

### 2101: Rare Herbs (Sidequest)
*   **Type:** Side
*   **Start NPC:** Doctor
*   **Prerequisite:** 1010 (Nursing to Health)
*   **Description:** The Doctor needs rare herbs found only deep in the foliage for better medicine.
*   **Objectives:**
    *   Collect 5 RareJungleHerb (from RareHerbPatch)
*   **Rewards:** 150 EXP, 20 Gold

## New Items
*   **PoisonIvySample** (ID 8001): Quest Item.
*   **RareJungleHerb** (ID 8002): Quest Item / Material.
*   **RaptorTooth** (ID 8003): Material / Trophy.

## New Interactables
*   **JungleEntrance**: Teleport trigger (simulated).
*   **PoisonIvyBush**: Gatherable.
*   **RareHerbPatch**: Gatherable.

## New Mobs
*   **Jaguar**: Level 12 Aggressive Mob.
