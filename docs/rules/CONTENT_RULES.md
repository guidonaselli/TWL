# CONTENT RULES (SSOT)

This document establishes the Single Source of Truth (SSOT) for Skills and Quests content.

## 1. Single Source of Truth
*   All content definitions must exist in JSON files (`skills.json`, `quests.json`).
*   No hardcoded skills or quests in C# code (except for ID constants referencing JSON data).
*   **Server-Authoritative**: Clients do not decide progress or unlocks.

## 2. Skills Rules
*   **Unique IDs**: SkillIds must be unique across all files.
*   **Goddess Skills Exception**:
    *   IDs 2001-2004 are reserved for Goddess Skills.
    *   **Names MUST be exactly**: "Shrink", "Blockage", "Hotfire", "Vanish".
    *   **Family**: `Special`.
    *   **Category**: `Goddess`.
    *   **Origin**: These skills are NOT granted by Quests. They are granted via specific game logic (Spirit contract).
*   **Special Skills**:
    *   Must have an "Origin" defined in `UnlockRules` (QuestId or QuestFlag) OR be granted by a Quest Reward.
    *   Cannot have `StageUpgradeRules` (Stages are for Core skills).
*   **Skill Families**:
    *   **Core**: Acquired via character progression (Level/Stats) or Stage Upgrades. NEVER granted directly by Quests.
    *   **Special**: Acquired via Quests, Events, or specific game logic (Goddess).

## 3. Quests Rules
*   **Unique IDs**: QuestIds must be unique.
*   **Idempotency**: Quest rewards must be idempotent (re-running a completed quest logic should not re-grant rewards).
*   **Skill Rewards**:
    *   Quests can only grant existing SkillIds.
    *   Quests CANNOT grant Goddess Skills (2001-2004).
    *   **Family Constraint**: Quests must ONLY grant skills of `Family: Special`.
    *   **Duplication/Exclusivity**: If multiple quests grant the same `GrantSkillId`, they MUST be mutually exclusive alternatives (share the same `MutualExclusionGroup`).
    *   **Consistency**: Any Skill granted by a Quest MUST have `UniquePerCharacter: true` in its restrictions to ensure anti-exploit/idempotency.
*   **Special Categories Constraints**:
    *   **RebirthJob**: Quests granting `RebirthJob` skills must require Rebirth Class/Status (e.g. `SpecialCategory: "RebirthJob"`).
    *   **ElementSpecial**: Quests granting `ElementSpecial` skills must have strict prerequisites (Level >= 10 OR Instance/Challenge type).

## 4. General Content Rules
*   **Unique DisplayNameKeys**: No two skills can share the same localization key.
*   **Anti-Snowball (Stage Upgrades)**:
    *   If Skill A upgrades to Skill B at Rank X (via `StageUpgradeRules`), then Skill B MUST strictly require Skill A at Rank X (via `UnlockRules`), OR have no parent requirements.
    *   Inconsistent requirements (e.g. A says Rank 10, B says Rank 20) are forbidden.
    *   **Integrity**: `StageUpgradeRules` MUST define `NextSkillId`. Partial rules (e.g. only RankThreshold) without a target are forbidden.

## 5. Content Validation
*   `TWL.Tests.ContentValidationTests` and `TWL.Tests.ContentValidationExtendedTests` are the enforcement mechanisms.
*   The build must fail if inconsistencies are detected.

## 6. Skill Categories Definition
*   **None**: Core skills (Tier 1-3).
*   **Goddess**: Special skills granted at start (Shrink, Blockage, Hotfire, Vanish).
*   **Dragon**: Legendary Fire physical skills.
*   **Fairy**: Support/Healing skills from Fairy quests.
*   **RebirthJob**: Skills unlocked after Rebirth.
*   **ElementSpecial**: High-tier elemental skills requiring specific quests.

## 7. Tier Budgets (SP & Cooldowns)
These budgets are enforced to maintain game balance.

### Tier 1 (Core)
*   **SP Cost**: 5 - 20 SP.
*   **Cooldown**: 0 - 2 Turns.
*   **Availability**: Level 1+, Low Stat Requirements (approx. 5).

### Tier 2 (Core)
*   **SP Cost**: 15 - 40 SP.
*   **Cooldown**: 1 - 3 Turns.
*   **Availability**: Upgrade from Tier 1 (Rank 10+).

### Tier 3 (Core / Special)
*   **SP Cost**: 30 - 100 SP.
*   **Cooldown**: 3 - 6 Turns.
*   **Availability**: High Level / Special Quest.
