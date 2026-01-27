# Puerto Roca City (Map 1000)

## Overview
The main hub city for the initial region. Contains key NPCs and services.

## Layout & Triggers
- **Map Size:** 20x20 tiles (640x640px).
- **Triggers:**
    - **To_North (ID 3):** Leads to Map 1002 (Sendero Norte), TargetSpawnId 1. Located at Top Center.
    - **To_Port (ID 4):** Leads to Map 1001 (Port), TargetSpawnId 1. Located at Left Center.
    - **To_Mines (ID 5):** Leads to Map 1003 (Mines), TargetSpawnId 1. Located at Right Center.

## Collisions
- **FountainBase (ID 1):** Solid collision block in the center (288, 288).

## Tileset Requirements
- Uses "placeholders" tileset (placeholder.png).
- **Ground Layer:** Filled with Tile ID 1 (placeholder).
- **TODO:** Assign real tileset GIDs and properly paint the city layout once art is finalized.
