# Map 1003 - Old Mines Entrance

## Intent
This map serves as the entrance lobby for the "Minas Antiguas" dungeon. It connects the safe city of Puerto Roca to the dangerous depths of the mines.
Currently, the deeper levels are gated/locked.

## Status
**Skeleton / Work In Progress**
- Geometry: Basic 30x30 room with walls.
- Visuals: Uses placeholder tileset.
- Entities: Contains placeholder Cave Bats (ID 9101, not yet defined in DB).

## Connections
- **South**: To Map 1000 (Puerto Roca City), Spawn 4.
- **North**: Locked Gate (Future connection to Map 1004).

## Requirements
- **Level Range**: 5-10
- **Tileset**: `placeholders.png` (Temporary)

## Triggers
- `To_City` (MapTransition): Takes player back to city.
- `LockedGate` (Interaction): Placeholder for future progression.
