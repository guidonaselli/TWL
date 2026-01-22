# Quest Design: Improvised Defense

## Overview
**Date:** 2024-05-22
**Objective:** Introduce interaction mechanics (searching/collecting) and basic weapon acquisition.
**Level Range:** 1-5
**Prerequisites:** Quest 1003 (Reporting In)

## Progression
This quest serves as the first "equipment" quest, giving the player a weapon to start combat training.

## Quest Chain

### 1004: Improvised Defense
*   **Start NPC:** Blacksmith (Village)
*   **Description:** "The wilds are dangerous. I can make you a weapon, but I need materials. Find some driftwood on the beach."
*   **Objectives:**
    1.  **Talk** to **Blacksmith**: "Ask about a weapon."
    2.  **Collect** (Search) **Driftwood**: "Find suitable wood on the beach." (Interact with "DriftwoodPile")
    3.  **Talk** to **Blacksmith**: "Hand over the materials."
*   **Rewards:**
    *   **Exp:** 50
    *   **Gold:** 0
    *   **Items:**
        *   `Wooden Sword` (ID: 103, Qty: 1)

## Technical Implementation
*   **New Interaction:** Uses `InteractRequest` opcode.
*   **Logic:**
    *   "Talk" objective checks `InteractRequest { TargetName: "Blacksmith" }`.
    *   "Collect" objective checks `InteractRequest { TargetName: "DriftwoodPile" }` (simulated collection).
*   **Validation:** Server-authoritative `TryProgress` in `PlayerQuestComponent`.
