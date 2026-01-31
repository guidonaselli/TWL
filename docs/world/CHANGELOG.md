# World System Changelog

## [Unreleased]

### Missing
- **Instances**: `InstanceManager` to create private map copies for parties is missing.
- **Lockouts**: Daily Limit (5/day) logic is not implemented.
- **Streaming**: Map loading is all-in-memory; no chunk/region streaming.

### Existing
- **Loading**: `MapLoader` parses TMX and `.meta.json` correctly.
- **Triggers**: `WorldTriggerService` handles Enter/Interact triggers via `ITriggerHandler`.
- **Spawns**: Static spawn points loaded from TMX.
