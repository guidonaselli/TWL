# Quest Design: Fishing Introduction (Region 1)

## Overview
**Type:** Sidequest Chain (Life Skill Intro)
**Level Range:** 5-8
**Prerequisites:** Quest 1004 (Improvised Defense)
**NPC:** Survivalist (Temporary placeholder)
**Goal:** Teach the player how to gather materials, craft a tool (Rod), and use it to gather food (Fish), then cook it.

## Quest Chain

### 1. Gone Fishing (ID: 2010)
**Context:** The Survivalist mentions that coconuts aren't enough protein. He suggests making a fishing rod.
**Objectives:**
*   **Collect** `BendableBranch` x2 (Found on trees/bushes).
*   **Collect** `VineString` x2 (Found on hanging vines).
*   **Craft** `SimpleFishingRod` x1 (At `OldWorkbench`).
**Rewards:**
*   Exp: 50
*   Item: `SimpleFishingRod` (ID: 303) - *Note: The craft consumes materials and gives the item, the quest reward is just XP, but we might ensure they have it.*

### 2. First Catch (ID: 2011)
**Context:** Now that you have a rod, go to the water's edge and catch something.
**Prerequisite:** 2010
**Objectives:**
*   **Collect** `RawFish` x3 (Interact with `FishingSpot`).
    *   *System Note:* Interaction with `FishingSpot` should require/check for `SimpleFishingRod` conceptually (implemented via `ConsumeRequiredItems: false` or just trust the player has it for now).
**Rewards:**
*   Exp: 100
*   Item: `RawFish` x2 (Bonus catch).

### 3. Grilling (ID: 2012)
**Context:** Raw fish is gross and dangerous. Cook it!
**Prerequisite:** 2011
**Objectives:**
*   **Craft** `CookedFish` x1 (At `Campfire`).
    *   *Recipe:* 1 `RawFish` -> 1 `CookedFish`.
**Rewards:**
*   Exp: 150
*   Item: `CookedFish` x5 (ID: 305).

## Technical Requirements
*   **Items:**
    *   301: Bendable Branch (Gatherable)
    *   302: Vine String (Gatherable)
    *   303: Simple Fishing Rod (Craftable)
    *   304: Raw Fish (Gatherable)
    *   305: Cooked Fish (Craftable/Consumable)
*   **Interactions:**
    *   `BendableBranch` (Type: Gather)
    *   `VineString` (Type: Gather)
    *   `FishingSpot` (Type: Gather)
    *   `OldWorkbench` (Update to include Rod recipe)
    *   `Campfire` (Type: Craft - Fish recipe)
