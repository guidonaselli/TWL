# Crash Site Expansion & Pet System Foundation

## Overview
This update introduces the first true main quest chain in the "Crash Site" region and lays the technical foundation for the Pet System (Capturable and Quest-Unique).

## Quest Chain: "Survival Basics"

### 1. Report In (ID: 1010)
*   **Prerequisites:** None.
*   **Objective:** Talk to the Captain.
*   **Rewards:** 50 Exp.
*   **Purpose:** Intro to dialogue and locating NPCs.

### 2. A New Friend (ID: 1012)
*   **Prerequisites:** Completed "Report In" (1010).
*   **Objective:** Approach the "Robinson's Parrot".
*   **Rewards:** 100 Exp, Pet Unlock: **Robinson's Parrot** (ID 101).
*   **Purpose:** Intro to Pets (Quest Unique type).

## Pet System Implementation

### Data Structure
*   `PetDefinition`: Enhanced with flags for `IsCapturable`, `IsQuestUnique`, `RecoveryServiceEnabled`, `RebirthEligible`.
*   `ServerPet`: New server-side runtime class tracking `InstanceId`, `Exp`, `Level`, `Amity`, `IsDead`.
*   `PlayerSaveData`: Updated to persist list of `ServerPetData` instead of simple IDs.

### Pets Introduced
1.  **Thief Monkey (ID 100)**
    *   Type: Capturable (Mob).
    *   Stats: Balanced Agi/Str.
    *   Source: Combat Capture (To be implemented).

2.  **Robinson's Parrot (ID 101)**
    *   Type: Quest Unique.
    *   Stats: Int/Wis/Agi focused.
    *   Source: Quest 1012 Reward.
    *   Features: Recovery Service Enabled (can be revived if run away).

## Technical Notes
*   `PetManager`: Added to handle loading of `pets.json`.
*   `PlayerQuestComponent`: Fixed `RequiredFlags` validation and persistence.
*   `ClientSession`: Updated `ClaimReward` to instantiate `ServerPet`s from definitions.
