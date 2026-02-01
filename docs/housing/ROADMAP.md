# HOUSING & MANUFACTURING ROADMAP

> **JULES-CONTEXT**: This roadmap tracks the housing (tent) and manufacturing systems.
> Housing is unlocked via quest chain (9001-9004) requiring Level 10+ and materials.
> The tent is a personal instance accessed from the overworld. Manufacturing uses
> workbench furniture inside the tent.

## Definition of Done (DoD)
- Server-side instance management for personal housing
- Furniture placement persisted (grid-based position + rotation)
- Manufacturing recipe validation (blueprints + resources)
- Transaction logging for storage operations

---

## Backlog

### P1 - Core Housing
- [ ] **HOU-001**: Tent System Core. "Tent" item spawns an entrance on the world map, teleports to a private instance.
- [ ] **HOU-002**: Furniture Grid. Grid-based placement (X, Y, rotation) with server-side collision validation.
- [ ] **HOU-003**: Storage/Warehouse. Furniture type "Cabinet" allowing persistent secondary inventory.

### P2 - Manufacturing & Access
- [ ] **HOU-004**: Manufacturing Workbenches. Interactive furniture (Workbench, Kitchen, Lathe) opening crafting UIs.
- [ ] **HOU-005**: Manufacturing Logic. Crafting with time costs (minutes/hours) or instant, consuming resources from inventory or storage.
- [ ] **HOU-006**: Permissions. Access control (Public, Private, Team Only, Guild Only).
- [ ] **HOU-007**: Garage System. Furniture for parking and repairing vehicles.
