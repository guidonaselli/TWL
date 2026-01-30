# Sendero Norte (Map 1002)

## Overview
A dangerous path north of Puerto Roca. First encounter with hostile mobs.

## Layout & Triggers
- **Map Size:** 20x20 tiles (640x640px).
- **Triggers:**
    - **To_City (ID 1):** Leads to Map 1000 (City), TargetSpawnId 3. Located at Bottom Center.

## Collisions
- **LeftCliff (ID 1):** CliffBlock along left edge.
- **RightCliff (ID 2):** CliffBlock along right edge.

## Spawns
- **FromCity (ID 3):** Entry point from Map 1000.
- **Bandido1 (ID 4):** Monster ID 9200.
- **Lobo1 (ID 5):** Monster ID 9202.
- **RockCrab (ID 6):** Monster ID 9300.
- **MysticHerb (ID 8):** Resource Node ID 10001.

## Tileset Requirements
- Uses "placeholders" tileset (placeholder.png).
- Basic layout painted on `Ground_Detail` using placeholder tiles.
- **TODO:** Assign real tileset GIDs and properly paint the field layout once art is finalized.
