# T02: 13-world-expansion 02

**Status:** Completed

## Actions Taken
- Explored `monsters.json` and retrieved IDs for appropriate level range monsters for regions 3 and 4.
  - Level 20-31 Bears and Spiders were selected for Selva Esmeralda (Region 3, Level 20-30). IDs: 2026, 2027, 2028, 2029, 2030, 2031, 2032.
  - Level 32-46 Goblins and Orcs were selected for Arrecife Hundido (Region 4, Level 30-45). IDs: 2033, 2034, 2035, 2036, 2037, 2038, 2039, 2040.
- Created `Content/Data/spawns/2000.spawns.json` mapping map ID 2000 to Selva monsters.
- Created `Content/Data/spawns/3000.spawns.json` mapping map ID 3000 to Arrecife monsters.
- Created map directory stubs at `Content/Maps/Selva/` and `Content/Maps/Arrecife/`.

## Test Skip Justification
The project could not be built due to existing C# compilation errors (`chara does not exist in the current context` in `PetService.cs`). Content validation testing was thus skipped as per instructions.