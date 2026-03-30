# T01: 13-world-expansion 01

**Slice:** S13 — **Milestone:** M001

## Description

Finalize spawn tables for Regions 1 and 2 (Isla Brisa and Puerto Roca) using expanded monster data.

Purpose: This supports CONT-06 and ensures early-game zones have balanced monster distribution matching the new `monsters.json`.
Output: Updated spawn table JSON files for the first two regions.

## Must-Haves

- [ ] "Spawn tables for Isla Brisa and Puerto Roca contain 3-5 mobs per map"
- [ ] "Map Region IDs must be 0001-0099 for Isla Brisa and 1000-1099 for Puerto Roca"
- [ ] "JSON validation: verify no ID collisions and check for missing localization keys"
- [ ] "Complete pre-commit steps to ensure proper testing, verification, review, and reflection are done."

## Files

- `Content/Data/spawns-isla-brisa.json`
- `Content/Data/spawns-puerto-roca.json`
