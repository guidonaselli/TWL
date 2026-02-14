# Sendero Norte (Map 1002)

## Overview
A dangerous path north of Puerto Roca. First encounter with hostile mobs.
Playable Alpha version.

## Layout & Tileset
- **Map Size:** 40x40 tiles (1280x1280px).
- **Tileset:** `Tiraka` (tiraka_tileset.png).
- **Structure:**
  - Winding dirt path from South to North.
  - Surrounded by grass and rock boundaries.
  - Cliffs on East/West edges.

## Triggers
- **To_City (ID 1):** Leads to Map 1000 (City), TargetSpawnId 3. Located at Bottom Center (on path).

## Collisions
- **LeftCliff (ID 1):** CliffBlock along left edge.
- **RightCliff (ID 2):** CliffBlock along right edge.

## Spawns
- **FromCity (ID 1):** Entry point from Map 1000. Located at South end of path.
- **Bandido1 (ID 2):** Monster ID 9200 (Level 2-4).
- **Lobo1 (ID 3):** Monster ID 9202 (Level 2-3).
- **RockCrab (ID 4):** Monster ID 9300 (Level 3-5).
- **MysticHerb (ID 10001):** Resource Node.

## Notes
- Tileset usage is explicit via `tiraka.tsx`.
- GIDs are +1 from tileset IDs.
- Future work: Add more props and detailed decorations once more tiles are available.
