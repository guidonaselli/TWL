# Design: Jungle Access & Training

**Date:** 2024-05-23
**Focus:** Region Transition (Jungle), Tool Gating, Training System Introduction
**Status:** In Progress

## Overview
This update implements the transition from the "Beach" (Initial Hub) to the "Jungle Fringe" (Next Zone). It gates this transition behind a tool check (Axe) used on an environmental obstacle (Vines). It also introduces the concept of "Training" via a sidequest to build a Training Dummy.

## Systems Introduced
*   **Tool Gating:** Interacting with objects requiring specific items *without* consuming them (e.g., cutting vines with an axe).
*   **Crafting Sidequest:** A dedicated crafting quest that doesn't just progress the main story.

## Quests

### 1017: The Way Out (Main)
*   **Prerequisites:** 1016 (Primitive Tools)
*   **NPC:** Old Hermit
*   **Description:** "The Hermit says the path to the jungle is blocked by thick overgrowth. He suggests using your new Stone Axe to clear a path."
*   **Objectives:**
    *   Talk to Old Hermit (Context).
*   **Rewards:** EXP.

### 1018: Cutting the Path (Main)
*   **Prerequisites:** 1017
*   **NPC:** (Auto/Environment)
*   **Description:** "Cut the Thick Vines blocking the path inland."
*   **Objectives:**
    *   **Craft** "ThickVines" (Requires Stone Axe). *Note: Uses "Craft" type to trigger checking of required items, but sets `ConsumeRequiredItems: false`.*
*   **Rewards:**
    *   EXP: 100
    *   Item: 204 (Jungle Fruit - Flavor item, first taste of new region).

### 2001: Training Basics (Side)
*   **Prerequisites:** 1016
*   **NPC:** Survivalist
*   **Description:** "You need to keep your skills sharp. The Survivalist suggests building a Training Dummy."
*   **Objectives:**
    *   **Collect** "DrySticks" x5.
    *   **Collect** "DriftwoodPile" x2.
    *   **Craft** "TrainingDummy" at "OldWorkbench".
*   **Rewards:**
    *   EXP: 150
    *   Item: 205 (Training Manual - flavor).

## Interactions

### ThickVines
*   **Type:** Craft (with `ConsumeRequiredItems: false`)
*   **RequiredQuestId:** 1018 (Must be on the quest to clear them)
*   **RequiredItems:** Stone Axe (203)
*   **RewardItems:** None (Quest gives reward).

### TrainingDummy
*   **Type:** Craft
*   **RequiredItems:** DrySticks (201, wait need to check ID) x5, Driftwood (20x) x2.
*   **RewardItems:** Training Dummy Item (206? or just quest trigger).
    *   *Correction:* In WLO, crafting usually puts an item in inventory or places furniture. Here, we'll give a "Training Dummy" furniture item (206).
*   **RequiredQuestId:** 2001.

## Technical Changes
*   Added `ConsumeRequiredItems` boolean to `InteractionDefinition` to support "Tool Checks".
