# Map Authoring Style Guide

This document defines the strict requirements for authoring maps in Tiled (.tmx) for The Wonderland.

## 1. General Requirements
- **File Format:** Tiled TMX (XML-based).
- **Tile Size:** 32x32 pixels (or project canonical size).
- **Orientation:** Orthogonal.
- **Naming Convention:** `Content/Maps/<Region>/<MapId>/<MapId>.tmx` where MapId is an integer.

## 2. Layer Structure (Strict Order)
Maps MUST include the following layers in this exact order. Missing layers will cause validation failures.

1.  **Ground** (Tile Layer): Base terrain.
2.  **Ground_Detail** (Tile Layer): Grass variations, paths, small details on ground.
3.  **Water** (Tile Layer): Water tiles.
4.  **Cliffs** (Tile Layer): Elevation changes, cliff walls.
5.  **Rocks** (Tile Layer): Large rocks, boulders integrated with cliffs.
6.  **Props_Low** (Tile Layer): Objects below player height (fences, small bushes).
7.  **Props_High** (Tile Layer): Objects above player height (trees, roofs).
8.  **Collisions** (Object Group): Collision geometry.
9.  **Spawns** (Object Group): Spawn points for players, NPCs, mobs.
10. **Triggers** (Object Group): Interaction zones, teleports, events.

## 3. Object Layers

### Collisions
- **Type:** Polygon or Rectangle objects.
- **Properties:**
    - `CollisionType` (string): `Solid`, `WaterBlock`, `CliffBlock`, `OneWay`.

### Spawns
- **Type:** Point or Rectangle objects.
- **Properties:**
    - `SpawnType` (string): `PlayerStart`, `Monster`, `NPC`, `ResourceNode`.
    - `Id` (int): Unique ID within the map (or global ID for NPCs/Mobs).
    - `Faction` (string): e.g., `Hostile`, `Neutral`.
    - `LevelRange` (string): e.g., `1-5`.
    - `RespawnSeconds` (int): e.g., `60`.
    - `Radius` (int): Patrol/wander radius.

### Triggers
- **Type:** Rectangle objects.
- **Properties:**
    - `TriggerType` (string): `MapTransition`, `QuestHook`, `InstanceGate`, `CutsceneHook`, `Interaction`.
    - `Id` (int): Unique Trigger ID.
    - `TargetMapId` (int): Destination Map ID (for transitions).
    - `TargetSpawnId` (int): Destination Spawn ID.
    - `RequiredFlags` (string): CSV of required quest flags.
    - `RequiredLevel` (int): Min level.
    - `Cooldown` (int): Seconds.
    - `OncePerCharacter` (bool).

## 4. Metadata (.meta.json)
Every map must have a companion `.meta.json` file.

```json
{
  "MapId": 1000,
  "RegionId": "PuertoRoca",
  "Biome": "Tropical",
  "RecommendedLevelMin": 1,
  "RecommendedLevelMax": 5,
  "EntryPoints": [ 1, 2 ],
  "Exits": [
    {
      "TargetMapId": 1001,
      "SpawnId": 1,
      "Gating": { "MinLevel": 1 }
    }
  ],
  "AmbientProfile": {
    "MusicKey": "BGM_TOWN_01",
    "AmbienceKey": "AMB_SEASIDE",
    "PaletteHintKey": "DAY_CYCLE"
  },
  "WorldFlagsSet": [],
  "WorldFlagsCleared": [],
  "Notes": "TODO: Replace placeholder tiles for Fountain."
}
```
