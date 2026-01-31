# Special Skill Quests & Goddess Skills Design
Date: 2024-12-07
Focus: Special Skill Quests, Hard Gating, and Initial GS Grant

## Overview
This design implements the system for "Special Skill Quests" (SSQ) and the initial grant of Goddess Skills (GS).
SSQs are high-difficulty, limited quests that reward unique skills (e.g., Dragon, Fairy, Element Special).
GS are granted automatically upon character creation or first login, ensuring idempotency.

## 1. Goddess Skills (GS)
**Logic:**
- **Trigger:** Login / Character Creation.
- **Mechanism:** Server checks `GS_GRANTED` flag in `PlayerQuestComponent`.
- **Assignment:**
  - Water -> Shrink (2001)
  - Earth -> Blockage (2002)
  - Fire -> Hotfire (2003)
  - Wind -> Vanish (2004)
- **Idempotency:**
  - If flag exists -> Skip.
  - If skill known -> Set flag, Skip.
  - If neither -> Grant Skill, Set flag.

## 2. Special Skill Quests (SSQ)
**Schema Extensions in `QuestDefinition`:**
- `Type`: "SpecialSkill" (distinguishes from "Regular")
- `SpecialCategory`: "Dragon", "Fairy", "RebirthJob", "ElementSpecial"
  - **Exclusivity:** Only one quest of a specific `SpecialCategory` can be `InProgress` at a time.
- `AntiAbuseRules`: "UniquePerCharacter"
  - **Restriction:** If the quest has *ever* been started (exists in `QuestStates`), it cannot be started again. Strictly one-time per character.

**Gating & Prerequisites:**
- **Level:** High requirement (e.g., Lv 10+ for Dragon).
- **Stats:** Specific stat thresholds (e.g., Str 20).
- **Objectives:** Must use verifiable server events (`Kill`, `InstanceComplete`, `Interact` with specific items).

**New Content:**
- **Fairy Skill Quest (ID 8002):**
  - Title: "The Fairy's Blessing"
  - Reward: Skill 8002 (Fairy's Blessing - Support/Heal)
  - Requirement: Level 10, Wis 20.
  - Objective: Complete Instance "Fairy Woods" (simulated via `InstanceComplete` type).

## 3. Implementation Details
- **`ClientSession`:**
  - `HandleInstanceCompletion(string instanceName)`: Hooks into `QuestComponent.TryProgress("Instance", instanceName)`.
- **`PlayerQuestComponent`:**
  - Already supports `AntiAbuseRules` and `SpecialCategory` logic (verified).
  - Needs `TryProgress` to support "Instance" type (generic implementation supports it).

## 4. Testing Strategy
- **`SpecialSkillQuestTests`:**
  - Mock `ClientSession` (subclass `TestClientSession`).
  - Verify GS Grant logic for different elements.
  - Verify `StartQuest` fails for duplicate SSQ of same category.
  - Verify `StartQuest` fails for "UniquePerCharacter" if already attempted.
  - Verify Reward Claiming.
