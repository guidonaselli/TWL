# Design: Isla Brisa - Optional Content & Pet Bonding

> **JULES-CONTEXT**: This document defines optional sidequests on Isla Brisa that remain
> available after the player leaves for Puerto Roca. Players can return via boat.
> These quests focus on pet bonding, life skills, and lore discovery.

**Level Range:** 3-8
**Region:** Isla Brisa (Maps 0001-0005)
**Narrative Theme:** Deepening bonds, mastering survival skills, uncovering more Ancestral clues.

---

## Pet Bonding Sidequests

### Quest 1013: Manjar Favorito
- **Prerequisite:** Quest 1010 (pet monkey unlocked)
- **NPC:** Nia (Start + End)
- **Map:** 0003
- **Description:** "Your monkey seems restless. Nia says there are ripe bananas in the palm groves."
- **Objectives:**
  - Collect Bananas (Type: `Collect`, Target: `item_banana`, Count: 5)
  - Feed Monkey (Type: `UseItem`, Target: `item_banana_on_pet`, Count: 1)
- **Rewards:** 50 EXP, 2x Small Potion (ItemId: 105)
- **OnComplete:** Unlocks Quest 1014, Amity +1 for Monkey pet

### Quest 1014: Hora de Jugar
- **Prerequisite:** Quest 1013
- **NPC:** Nia (Start + End)
- **Map:** 0002
- **Description:** "The monkey is energetic! Take it to the tide pools and let it play."
- **Objectives:**
  - Reach Costa de Mareas with pet active (Type: `Reach`, Target: `map_0002_with_pet`, Count: 1)
  - Interact with Monkey 3 times (Type: `Interact`, Target: `pet_monkey_play`, Count: 3)
- **Rewards:** 100 EXP, Amity +2 for Monkey pet
- **Narrative:** Bonding mechanic demonstration. Higher Amity = better pet performance.

---

## Lore Discovery Sidequests

### Quest 1050: Rumores en la Cala
- **Prerequisite:** Quest 1007 (El Eco en la Cueva)
- **NPC:** El Viejo Coral (Start + End)
- **Map:** 0004
- **Description:** "The hermit has found more inscriptions deeper in the cave. He needs help deciphering them."
- **Objectives:**
  - Examine Wall Inscription 1 (Type: `Interact`, Target: `obj_inscription_1`, Count: 1)
  - Examine Wall Inscription 2 (Type: `Interact`, Target: `obj_inscription_2`, Count: 1)
  - Examine Wall Inscription 3 (Type: `Interact`, Target: `obj_inscription_3`, Count: 1)
- **Rewards:** 120 EXP, 30 Gold, Lore Entry: "Los Ancestrales - Fragment 1"
- **OnComplete:** Unlocks Quest 1051
- **Narrative:** The inscriptions describe a civilization that could channel elemental energy
  through crystalline structures. They called themselves "Los Resonantes."

### Quest 1051: La Fuente del Ruido
- **Prerequisite:** Quest 1050
- **NPC:** El Viejo Coral (Start + End)
- **Map:** 0004
- **Description:** "The cave hums louder at night. Find the source."
- **Objectives:**
  - Explore Deep Cave (Type: `Reach`, Target: `poi_deep_cave`, Count: 1)
  - Defeat Cave Bats (Type: `Kill`, Target: `mob_cave_bat`, Count: 5)
  - Activate Crystal Console (Type: `Interact`, Target: `obj_crystal_console`, Count: 1)
- **Rewards:** 150 EXP, 50 Gold, 1x Resonance Fragment (key item)
- **OnComplete:** Unlocks Quest 1054

### Quest 1054: Un Pequeno Amigo
- **Prerequisite:** Quest 1051
- **NPC:** El Viejo Coral (Start + End)
- **Map:** 0004
- **Description:** "Activating the console stirred something. A small stone creature emerged from the wall."
- **Objectives:**
  - Approach Stone Spirit (Type: `Interact`, Target: `npc_stone_spirit`, Count: 1)
  - Offer Resonance Fragment (Type: `UseItem`, Target: `item_resonance_fragment`, Count: 1)
- **Rewards:** 200 EXP, **Pet Unlock: Stone Spirit (PetId: 1054)**
- **Flags:** `UniquePerCharacter: true`
- **Narrative:** The Stone Spirit is an Ancestral construct - a guardian left behind.
  It bonds with the player through the Resonance Shard they carry. This is the first
  hint that the player has a special connection to Ancestral technology.

---

## Life Skill Sidequests

### Quest 1060: Suministros Perdidos
- **Prerequisite:** Quest 1005 (camp established)
- **NPC:** Capitana Maren (Start + End)
- **Map:** 0002
- **Description:** "Cargo from the wreck washed up along the coast. Retrieve what you can."
- **Objectives:**
  - Open Drifting Crate 1 (Type: `Interact`, Target: `obj_crate_1`, Count: 1)
  - Open Drifting Crate 2 (Type: `Interact`, Target: `obj_crate_2`, Count: 1)
  - Open Drifting Crate 3 (Type: `Interact`, Target: `obj_crate_3`, Count: 1)
- **Rewards:** 80 EXP, 40 Gold, Random loot (potions, materials)
- **Repeatable:** false

---

## Quest Flow Diagram

```
[Quest 1010: Pet Monkey] -> 1013 -> 1014 (Pet Bonding chain)

[Quest 1007: Cave] -> 1050 -> 1051 -> 1054 (Lore + Stone Spirit pet)

[Quest 1005: Camp] -> 1060 (Supply recovery)
```
