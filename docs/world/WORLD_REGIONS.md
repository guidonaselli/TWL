# World Regions - The Wonderland Legacy

> **JULES-CONTEXT**: This file defines all game regions, their narrative role, and map IDs.
> When creating maps, NPCs, quests, or encounters, reference this document for region identity.
> Map IDs follow the convention: Region prefix (1xxx, 2xxx, 3xxx...) + sequential number.

---

## Lore Premise

The world of TWL is an uncharted archipelago called **Las Islas Perdidas** ("The Lost Islands").
A catastrophic storm wrecked the player's ship, stranding them on a beach. As survivors explore,
they discover the islands hold the ruins of **Los Ancestrales** - a vanished civilization that
mastered elemental energy through crystalline technology called **Resonancia**.

The world is NOT a fantasy kingdom. It is a mystery-driven survival setting where players uncover
ancient secrets while building a community from nothing. Think: *Lost* meets *Golden Sun* meets
*classic 2D MMO exploration*.

---

## Region 0: Isla Brisa (Starter Island)

**Map Range**: 0001 - 0099
**Theme**: Tropical beach, shipwreck debris, coastal jungle edge, tide pools.
**Tone**: Hopeful survival. Discovery. Learning the ropes.

The first island players experience. A crescent-shaped beach with a wrecked ship,
a freshwater stream, and dense vegetation inland. The crash site becomes a makeshift
camp where NPCs teach core mechanics.

### Key Locations

| Map ID | Name | Type | Description |
|--------|------|------|-------------|
| 0001 | Playa del Naufragio | Safe Zone | Crash site beach. Tutorial NPCs, campfire, basic crafting. |
| 0002 | Costa de Mareas | Adventure Field | Tide pools and rocky shore. Slimes, crabs. First combat area. |
| 0003 | Sendero de la Selva | Adventure Field | Jungle edge path. Monkeys, venomous plants. Leads to caves. |
| 0004 | Cueva del Eco | Mini-Dungeon | Shallow cave with glowing crystals. Stone Circle puzzle. |
| 0005 | Mirador del Faro | Landmark | Ruined lighthouse on a cliff. Unlocks view of Puerto Roca across the water. |

### NPCs (Starter Island)

| NPC | Role | Key Function |
|-----|------|-------------|
| Capitana Maren | Shipwreck Leader | Main quest giver (Acts 1-2). Guides player through survival basics. |
| Dr. Calloway | Ship Doctor | Teaches healing items and potion crafting. |
| Ruk | Ship Blacksmith | Teaches weapon/tool crafting. Gives first weapon quest. |
| Nia | Survivalist Scout | Teaches gathering, fishing, cooking. Jungle access questline. |
| El Viejo Coral | Mysterious Hermit | Lives near the caves. Knows about Los Ancestrales. Lore exposition. |

### Progression Gate
Players leave Isla Brisa by repairing a small boat (Quest: "Travesia"). This requires:
- Completing the core survival quests (camp, weapon, food)
- Discovering the Stone Circle in Cueva del Eco
- Reaching Level 5+

---

## Region 1: Puerto Roca (Main Hub City)

**Map Range**: 1000 - 1099
**Theme**: Tropical colonial port. Stone and wood architecture. Market stalls, docks, taverns.
**Tone**: Civilization. Commerce. Social hub. Quest branching point.

A port city built by previous waves of shipwreck survivors over generations. It is the
largest settlement in the archipelago and the central hub for trade, guilds, and expeditions.

### Key Locations

| Map ID | Name | Type | Description |
|--------|------|------|-------------|
| 1000 | Plaza Mayor | Safe Zone | Central plaza. Guild Hall, vendors, bulletin board, fountain. |
| 1001 | El Muelle | Safe Zone | Docks. Ship departures to other islands. Fisher NPCs. |
| 1002 | Sendero Norte | Adventure Field | Forest path north of city. Bandits, wolves. Connects to jungle. |
| 1003 | Minas Antiguas | Dungeon Entrance | Abandoned mines. Bats, golems. Mining resources. |
| 1010 | Barrio del Mercado | Safe Zone | Marketplace district. Player stalls, auction house NPC. |
| 1011 | La Taberna del Ancla | Interior | Tavern. Party formation, rumors/sidequests, lore hints. |
| 1012 | Cuartel de la Guardia | Interior | Guard HQ. Combat training quests, bounty board. |

### NPCs (Puerto Roca)

| NPC | Role | Key Function |
|-----|------|-------------|
| Alcalde Fuentes | City Mayor | Main quest giver for Puerto Roca arc. Citizenship quest. |
| Maestra Vega | Guild Registrar | Guild creation, management. |
| Herrero Dante | Master Blacksmith | Advanced weapon/armor crafting. Equipment upgrades. |
| Alquimista Luna | Alchemist | Potion brewing, rare herb quests. Teaches alchemy. |
| Capitana Riel | Harbor Master | Ship departures, vehicle upgrades (canoe -> raft -> ship). |
| Sargento Bravo | Guard Captain | Combat training arc. Bounty quests. |

### Progression Gate
Access to further islands requires:
- Puerto Roca citizenship (quest chain)
- A seaworthy vessel (crafted or purchased)
- Minimum Level 10

---

## Region 2: Selva Esmeralda (North Island - Jungle)

**Map Range**: 2000 - 2099
**Theme**: Dense tropical jungle. Overgrown ruins. Bioluminescent flora at night.
**Tone**: Dangerous exploration. Ancient mystery. Nature vs. civilization.

The northern island is dominated by impenetrable jungle. Ancestral ruins poke through
the canopy. The wildlife is aggressive and the flora is often poisonous. Deep within
lies a functional Ancestral facility - the Resonance Spire.

### Key Locations

| Map ID | Name | Type | Description |
|--------|------|------|-------------|
| 2001 | Orilla Verde | Adventure Field | Jungle beach landing. Transition from boat. |
| 2002 | Senda Venenosa | Adventure Field | Poisonous flora path. Jaguars, snakes. |
| 2003 | Claro del Guardian | Boss Arena | Jungle Guardian mini-boss. Gate to ruins. |
| 2010 | Ruinas Exteriores | Adventure Field | Crumbling walls, overgrown plazas. Bat enemies. |
| 2020 | Camara del Holograma | Dungeon | Interior of Ancestral facility. Hologram puzzle. Crystal tech. |
| 2030 | La Aguja de Resonancia | Dungeon (Deep) | Core of the ruins. Element attunement mechanic. |

---

## Region 3: Isla Volcana (South Island - Volcanic)

**Map Range**: 3000 - 3099
**Theme**: Volcanic terrain. Black sand beaches. Lava flows. Obsidian formations.
**Tone**: High-level challenge. Fire element focus. Endgame progression.

A hostile island dominated by an active volcano. The Ancestrales built their most
powerful facility here - the Forge of Elements - where Resonance crystals were created.
Only experienced adventurers should attempt this region.

### Key Locations

| Map ID | Name | Type | Description |
|--------|------|------|-------------|
| 3001 | Playa Negra | Adventure Field | Black sand beach. Fire wolves, lava crabs. |
| 3002 | Sendero de Ceniza | Adventure Field | Ash-covered path. Flame tigers. |
| 3010 | Boca del Volcan | Dungeon Entrance | Volcano mouth. Environmental damage mechanics. |
| 3020 | La Forja Ancestral | Raid Dungeon | Endgame dungeon. Legendary equipment crafting. |

---

## Region 4: Arrecife Hundido (Underwater Ruins) [FUTURE]

**Map Range**: 4000 - 4099
**Theme**: Submerged Ancestral city. Coral-covered architecture. Underwater combat.
**Tone**: Mysterious. Exploration-focused. Water element content.

Unlocked through a special questline. Requires diving equipment (crafted).

---

## Region 9: Test / Event Maps

**Map Range**: 9000 - 9099
**Theme**: Varies. Used for seasonal events, GM events, and QA testing.

| Map ID | Name | Type |
|--------|------|------|
| 9001 | Arena de Pruebas | PvP Arena |
| 9010 | Evento Estacional | Event Map (rotates) |

---

## Map ID Convention Summary

| Prefix | Region | Status |
|--------|--------|--------|
| 0xxx | Isla Brisa (Starter) | In Development |
| 1xxx | Puerto Roca (Hub) | In Development |
| 2xxx | Selva Esmeralda (Jungle) | Planned |
| 3xxx | Isla Volcana (Volcanic) | Planned |
| 4xxx | Arrecife Hundido (Underwater) | Future |
| 9xxx | Test / Events | Active |
