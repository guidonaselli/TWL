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

## 3. Quests Rules
*   **Unique IDs**: QuestIds must be unique.
*   **Idempotency**: Quest rewards must be idempotent (re-running a completed quest logic should not re-grant rewards).
*   **Skill Rewards**:
    *   Quests can only grant existing SkillIds.
    *   Quests CANNOT grant Goddess Skills (2001-2004).

## 4. Content Validation
*   `TWL.Tests.ContentValidationTests` is the enforcement mechanism.
*   The build must fail if inconsistencies are detected.
