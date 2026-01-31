# Design: Puerto Roca Transition - Act 2 Entry

> **JULES-CONTEXT**: This document defines the quest chain that transitions players from
> Isla Brisa to Puerto Roca. It introduces tool-gating mechanics and the concept of
> region unlocks. This is the bridge between tutorial and open game.
> Prerequisite: All Isla Brisa main quests (1001-1018) completed.

**Level Range:** 5-8
**Region:** Puerto Roca approach (Maps 1000-1012)
**Narrative Theme:** Arriving in civilization, proving yourself, becoming a citizen.

---

## Narrative Summary

After building a boat and crossing the strait, the player arrives at Puerto Roca - a
bustling port city built by generations of shipwreck survivors. The player must register
as a citizen, prove their worth through tasks for the city guard, and establish themselves
in this new community.

---

## Quest Chain: "Ciudadano Nuevo"

### Quest 1100: Llegada al Muelle
- **NPC:** Capitana Riel (Start + End)
- **Map:** 1001 (El Muelle)
- **Description:** "The docks are busy. A stern woman in a harbor master's uniform blocks your path."
- **Objectives:**
  - Talk to Capitana Riel (Type: `Talk`, Target: `npc_riel`, Count: 1)
  - Show Resonance Shard (Type: `ShowItem`, Target: `item_resonance_shard`, Count: 1)
- **Rewards:** 100 EXP, 25 Gold
- **OnComplete:** Unlocks Quest 1101, grants Temporary Dock Pass (flag)
- **Narrative:** Riel is skeptical but intrigued by the Resonance Shard. She directs
  the player to the Mayor for registration.

### Quest 1101: Registro Civil
- **NPC:** Alcalde Fuentes (Start + End)
- **Map:** 1000 (Plaza Mayor)
- **Description:** "The Mayor requires proof of good character before granting citizenship."
- **Objectives:**
  - Talk to Alcalde Fuentes (Type: `Talk`, Target: `npc_fuentes`, Count: 1)
  - Deliver Letter from Capitana Maren (Type: `DeliverItem`, Target: `item_maren_letter`, Count: 1)
- **Rewards:** 150 EXP, 50 Gold
- **OnComplete:** Unlocks Quest 1102
- **Note:** `item_maren_letter` is auto-granted when starting this quest (Maren gave it before departure)

### Quest 1102: Prueba de Valor
- **Prerequisite:** Quest 1101
- **NPC:** Sargento Bravo (Start + End)
- **Map:** 1002 (Sendero Norte)
- **Description:** "The Sergeant doesn't trust newcomers. Prove yourself by clearing bandits from the north road."
- **Objectives:**
  - Defeat Forest Bandits (Type: `Kill`, Target: `mob_forest_bandit`, Count: 8)
  - Defeat Bandit Leader (Type: `Kill`, Target: `mob_bandit_leader`, Count: 1)
- **Rewards:** 250 EXP, 75 Gold, 1x Iron Sword (ItemId: 110, ATK +8)
- **OnComplete:** Unlocks Quest 1103

### Quest 1103: Juramento de Ciudadania
- **Prerequisite:** Quest 1102
- **NPC:** Alcalde Fuentes (Start + End)
- **Map:** 1000 (Plaza Mayor)
- **Description:** "The Mayor is impressed. He offers citizenship and access to all city services."
- **Objectives:**
  - Talk to Alcalde Fuentes (Type: `Talk`, Target: `npc_fuentes`, Count: 1)
- **Rewards:** 300 EXP, 100 Gold, Citizenship Flag (`citizen_puerto_roca = true`)
- **OnComplete:** Unlocks all Puerto Roca services (marketplace, guild, advanced crafting)
- **Narrative:** The player swears an oath to protect the city. This is a significant
  milestone - the first time the player belongs to a community larger than the camp.

---

## Puerto Roca Sidequests (Unlocked with Citizenship)

### Quest 2010: Pesca del Dia
- **NPC:** Pescador Viejo (El Muelle)
- **Map:** 1001
- **Description:** "An old fisherman offers to teach you his craft."
- **Objectives:**
  - Craft Fishing Rod (Type: `Craft`, Target: `item_fishing_rod`, Count: 1)
  - Catch Raw Fish (Type: `Fish`, Target: `item_raw_fish`, Count: 3)
- **Rewards:** 100 EXP, 30 Gold, Fishing skill unlocked

### Quest 2011: Aprendiz de Alquimia
- **NPC:** Alquimista Luna
- **Map:** 1000
- **Description:** "Luna needs herb samples from the forest. In exchange, she'll teach you alchemy basics."
- **Objectives:**
  - Collect Healing Herbs (Type: `Collect`, Target: `item_healing_herb`, Count: 5)
  - Collect Moonpetal (Type: `Collect`, Target: `item_moonpetal`, Count: 2)
- **Rewards:** 120 EXP, 40 Gold, Alchemy tutorial unlocked

### Quest 3001: Entrenamiento de Combate
- **NPC:** Sargento Bravo
- **Map:** 1012 (Cuartel de la Guardia)
- **Description:** "The Sergeant offers advanced combat training to citizens."
- **Objectives:**
  - Defeat Training Dummy (Type: `Kill`, Target: `obj_training_dummy`, Count: 3)
  - Win Sparring Match (Type: `Kill`, Target: `npc_sparring_partner`, Count: 1)
- **Rewards:** 200 EXP, 50 Gold, Skill: "Power Strike" (basic combat skill)

---

## Quest Flow Diagram

```
[Isla Brisa Complete] -> 1100 -> 1101 -> 1102 -> 1103 [Citizenship]
                                                    |
                          +-------------------------+-------------------------+
                          |                         |                         |
                        2010 (Fishing)          2011 (Alchemy)          3001 (Combat)
```
