# THE WONDERLAND LEGACY - MASTER ROADMAP

> **JULES-CONTEXT**: This is the index for all roadmaps. When starting a new task,
> first read `CONTEXT.md` for game identity, then find the relevant domain roadmap below.
> Pick tasks by priority (P0 first). Each task must produce: code + tests + doc update.

---

## Domain Roadmaps

### 1. Core System & Architecture
**File:** [docs/core/ROADMAP.md](core/ROADMAP.md)
- Server architecture, networking, security, persistence
- Combat engine, inventory, economy, social systems
- World/map streaming, instances, events

### 2. Skills System
**File:** [docs/skills/ROADMAP.md](skills/ROADMAP.md)
- Skill definitions, elemental packs (Earth/Water/Fire/Wind)
- Tier budgets, mastery-by-use, stage evolution
- Goddess Skills, legendary skills, life skills

### 3. Quests & Content
**File:** [docs/quests/ROADMAP.md](quests/ROADMAP.md)
- Quest engine, objective types, reward system
- Questlines: Isla Brisa, Puerto Roca, Selva Esmeralda
- Design docs: `docs/quests/design/`

### 4. Pets System
**File:** [docs/pets/ROADMAP.md](pets/ROADMAP.md)
- Pet model, combat AI, capture mechanics
- Amity/bonding, riding, rebirth/evolution
- Pet population across regions

### 5. Housing & Manufacturing
**File:** [docs/housing/ROADMAP.md](housing/ROADMAP.md)
- Tent system, furniture grid, storage
- Manufacturing workbenches, crafting logic
- Permissions, garage system

### 6. World & Regions
**File:** [docs/world/WORLD_REGIONS.md](world/WORLD_REGIONS.md)
- Region definitions, map IDs, NPCs, progression gates
- Lore: Las Islas Perdidas, Los Ancestrales, Resonancia

---

## Rules (SSOT)

| Document | What it enforces |
|----------|-----------------|
| [docs/rules/GAMEPLAY_CONTRACTS.md](rules/GAMEPLAY_CONTRACTS.md) | Market, PvP, death penalty, instance lockouts, skill progression |
| [docs/rules/CONTENT_RULES.md](rules/CONTENT_RULES.md) | Skill IDs, quest rewards, data validation, tier budgets |

---

> **Adding new items:** Add to the domain-specific roadmap. Cross-cutting or architectural
> changes go in `docs/core/ROADMAP.md`. Format: `DOM-###`: short title, priority, acceptance criteria.
