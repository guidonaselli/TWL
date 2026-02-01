# CONTEXT.md

> **JULES-CONTEXT**: This is the conceptual source of truth for the entire project.
> Read this file FIRST before working on any task. It defines the game identity,
> architecture constraints, and design rules that all code must respect.
> For implementation tasks, cross-reference with `docs/core/ROADMAP.md` and `docs/rules/`.

---

## Project: The Wonderland Legacy (TWL)

**The Wonderland Legacy (TWL)** is a 2D multiplayer JRPG set in an uncharted archipelago
called **Las Islas Perdidas**. Players are shipwreck survivors who build a community,
explore ancient ruins, and uncover the secrets of a lost civilization called **Los Ancestrales**
that mastered elemental energy through crystalline technology known as **Resonancia**.

**Genre:** Turn-based MMORPG (WLO-inspired, original IP)
**Platform:** PC (MonoGame/DesktopGL), future: mobile
**Architecture:** Server-authoritative, clean 3-layer separation

### Core Pillars

1. **Multiplayer-First:** Persistent shared world. Login-based accounts. No local saves.
2. **Tactical Turn-Based Combat:** Party-based (player + pets + allies). Elemental cycle.
3. **Deep Progression:** Stats, equipment, rebirth, skill mastery, pet bonding, crafting.
4. **Visual Customization:** Palette swapping + visible equipment layering.
5. **Mystery-Driven Narrative:** Survival + ancient ruins + elemental technology discovery.
6. **Data-Driven Content:** All skills, quests, items, pets defined in JSON. No hardcoded content.

---

## Game Identity (What Makes TWL Unique)

TWL is NOT a fantasy kingdom game. It is:

- **Setting:** Tropical archipelago, shipwreck survival, ancient crystalline ruins
- **Tone:** *Lost* meets *Golden Sun* meets *classic 2D MMO*
- **Player Fantasy:** You are an explorer who discovers you have a unique connection to
  Ancestral technology (Resonancia). You are not a "chosen one" - you earned it by
  being brave enough to venture into the ruins.
- **World Logic:** Civilization exists because of previous shipwreck waves. Puerto Roca
  is a city built by generations of survivors. The world grows as players explore.

### Elemental System

| Element | Identity | Stat | Role | Color |
|---------|----------|------|------|-------|
| Earth | Endurance | CON | Tank/Shield | Brown/Green |
| Water | Healing | WIS | Healer/Cleanse | Blue |
| Fire | Burst | STR/INT | DPS/Offense | Red/Orange |
| Wind | Speed | SPD | Control/AoE | Cyan/White |

**Cycle:** Water > Fire > Wind > Earth > Water (1.5x advantage, 0.5x disadvantage)

### World Regions

| Region | Map Range | Theme | Status |
|--------|-----------|-------|--------|
| Isla Brisa (Starter) | 0001-0099 | Beach, shipwreck, tutorial | In Dev |
| Puerto Roca (Hub) | 1000-1099 | Colonial port city, commerce | In Dev |
| Selva Esmeralda (Jungle) | 2000-2099 | Dense jungle, Ancestral ruins | Planned |
| Isla Volcana (Volcanic) | 3000-3099 | Volcano, endgame, legendary forge | Planned |
| Arrecife Hundido (Underwater) | 4000-4099 | Submerged Ancestral city | Future |

> Full world design: `docs/world/WORLD_REGIONS.md`

---

## Solution Architecture

### 1. TWL.Shared (Domain & Contracts)

Pure domain project. **No MonoGame references.**

Responsibilities:
- Domain model: `Character`, `PlayerCharacter`, `PetCharacter`, `NpcCharacter`
- Inventory: `Inventory`, `InventoryItem`, `ItemDefinition`, `EquipmentSlot`
- Stats: STR, CON, INT, WIS, SPD -> derived ATK, DEF, MAT, MDF, SPD
- Combat: Turn representation, actions, skills, status effects
- Quests: Quest states, objectives, rewards
- Economy: Gold, TwlPoints, drops
- Network: DTOs, gameplay events, `INetworkChannel`, `IGameManager`

**Rules:**
- TWL.Shared **cannot depend** on MonoGame, ContentManager, Texture2D, SpriteBatch, or any graphics type
- Shared models the **"what"** (data and rules), never the "how it renders"

### 2. TWL.Client (Presentation & Game Client)

MonoGame-based graphical client.

Responsibilities:
- Scenes: `SceneBase`, `SceneMainMenu`, `SceneGameplay`, `SceneBattle`
- Content loading: `ContentManager`, `IAssetLoader`
- Tiled map rendering (MonoGame.Extended)
- Camera 2D, input handling, UI/HUD
- Player visual: `PlayerView` (sprite base + palette swap + equipment layers)
- UI system: `UiMainMenu`, `UiGameplay`, inventory, equipment, stats, party, chat

**Rules:**
- Client **can reference** TWL.Shared, but not the reverse
- All texture/sprite/animation/UI layout logic lives in TWL.Client
- Client does NOT own "real" character state; it renders from Shared models + server data

### 3. TWL.Server (Authoritative Server)

Owner of world state and character data.

Responsibilities:
- Persistence: Characters, equipment, pets, quests, inventories, accounts (PostgreSQL)
- Authoritative combat: Validate actions, apply effects, resolve turns, send results
- World logic: Map instances, spawns, event triggers
- Synchronization: Player state, positions, inventory/equipment updates

**Rules:**
- Server is the single source of truth for all game state
- All valuable operations must be idempotent
- Anti-dupe and anti-replay protections are mandatory

---

## Player Character Model

### PlayerCharacter (in TWL.Shared)

- **Identity:** `Id`, `Name`, `Element`
- **Progression:** `Level`, `Exp`, `ExpToNextLevel`, methods: `GainExp`, `TryLevelUp`, `DoRebirth`
- **Stats:** Base stats (STR, CON, INT, WIS, SPD) + equipment bonuses + buff/debuff modifiers
- **Inventory:** Consumables, materials, key items
- **Equipment:** Slots: Head, Body, Weapon, Boots, Accessory1, Accessory2. Each item provides stat bonuses.
- **Economy:** `Gold`, `TwlPoints`
- **Pets:** List of `PetCharacter` with leveling, amity, rebirth

PlayerCharacter **knows nothing** about visual representation.

### Visual Appearance (in TWL.Client)

Rendered by `PlayerView`:
- Sprite base (body, hair, eyes) with palette swapping
- Equipment overlay layers (clothing, helmet, weapon)
- Animation: idle/walk/run based on `FacingDirection`
- Draw order consistent with facing direction

---

## Data & Content

All game content is defined in JSON files under `Content/Data/`:

| File | Purpose |
|------|---------|
| `skills.json` | Skill definitions (damage, effects, unlock rules, tiers) |
| `quests.json` | Quest definitions (objectives, rewards, prerequisites) |
| `pets.json` | Pet definitions (stats, growth, capture rules, utilities) |
| `interactions.json` | World object interactions |
| `items.json` | Item definitions (consumables, equipment, materials) |
| `equipment.json` | Equipment metadata (stats + visual info) |
| `playercolors.json` | Player customization color palettes |
| `monsters.json` | Enemy definitions |

**Validation:** `TWL.Tests.ContentValidationTests` enforces data integrity.
**Rules:** See `docs/rules/CONTENT_RULES.md` and `docs/rules/GAMEPLAY_CONTRACTS.md`.

---

## Internationalization (i18n)

- No UI text is hardcoded in presentation code
- All text referenced by symbolic keys: `UI_Login`, `UI_Title`, `msg.battle.victory`
- Resolution via `Loc.T("key")` service using JSON-based translation files
- Domain (Shared) uses keys, not translated text

---

## Gameplay Scene Flow

```
SceneMainMenu -> [Login] -> SceneGameplay -> [Encounter] -> SceneBattle -> [Victory] -> SceneGameplay
                                          -> [Open UI]   -> Inventory/Equipment/QuestLog/Party
```

### SceneGameplay Loop
1. `Initialize`: Create/receive `PlayerCharacter` from `GameClientManager`, create `PlayerView` and UI
2. `LoadContent`: Load map, player assets, UI assets
3. `Update`: Process input, pathfinding, encounters, UI, update PlayerCharacter and PlayerView
4. `Draw`: Draw map -> player + equipment -> HUD/UI

---

## Design Evolution

TWL's design is iterative. Current class names, methods, and modules are a snapshot,
not an immutable contract. Refactorings are expected and encouraged when they improve:

- Conceptual clarity of the domain model
- Separation of responsibilities (Client / Shared / Server)
- Cohesion and expressiveness of domain entities
- Extensibility for new mechanics (skills, advanced equipment, crafting, pet AI)
- Performance or maintainability

---

## Key Documentation Index

| Document | Purpose |
|----------|---------|
| `docs/core/ROADMAP.md` | Master technical roadmap (P0-P3 priorities) |
| `docs/skills/ROADMAP.md` | Skill system backlog |
| `docs/quests/ROADMAP.md` | Quest system backlog |
| `docs/pets/ROADMAP.md` | Pet system backlog |
| `docs/housing/ROADMAP.md` | Housing & manufacturing backlog |
| `docs/world/WORLD_REGIONS.md` | World regions, maps, NPCs, lore |
| `docs/rules/GAMEPLAY_CONTRACTS.md` | System rules SSOT (market, PvP, death penalty, etc.) |
| `docs/rules/CONTENT_RULES.md` | Data rules SSOT (skill IDs, quest rewards, validation) |
| `docs/PRODUCTION_GAP_ANALYSIS.md` | Current gaps and risks |
