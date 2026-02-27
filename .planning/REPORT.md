# Consistency Auditor Report

## RESULT: REPORT

## SUMMARY
A comprehensive audit of the Skills and Quests data was performed. The Skill system is robust, but the Quest system has significant consistency issues regarding reward Item IDs and logical references. Specifically, several quests reward items that do not exist in the client item database, and some quests reward items that contradict their narrative context (e.g., rewarding Sandals for a Potion quest).

## VIOLATIONS (Prioritized)

### P0: Broken Item References (Quest Rewards)
*   **Content/Data/quests_islabrisa_side.json** (Quest 1015): Reward ItemId `8005` does not exist.
*   **Content/Data/quests.json** (Quest 9004): Reward ItemId `800` does not exist.
*   **Content/Data/quests.json** (Quest 1020): Reward ItemId `10001` does not exist.

### P1: Logical Inconsistencies (Narrative vs Data)
*   **Content/Data/quests.json** (Quest 2003): Quest "Basic Brew" rewards `3001` (Camelia Sandal). Should likely be `4739` (Healing Potion).
*   **Content/Data/quests.json** (Quest 1051): Quest "La Fuente del Ruido" rewards `3001` (Camelia Sandal). Narrative implies `4739` (Healing Potion).
*   **Content/Data/quests.json** (Quest 1100): Quest "Partida Inminente" rewards `3001` (Camelia Sandal). Narrative implies `4739` (Healing Potion).
*   **Content/Data/quests_messenger.json** (Quest 5002): Objective requires delivering `3001` (Camelia Sandal). Narrative explicitly says "Deliver 1 Potion".

## PROPOSED FIX

1.  **Correct Missing IDs**:
    *   Quest 1015: Replace `8005` with `6312` (Conch Shell).
    *   Quest 9004: Replace `800` with `7373` (Saving Box).
    *   Quest 1020: Replace `10001` with `7316` (Wooden Stick).

2.  **Align Narrative**:
    *   Quest 2003, 1051, 1100, 5002: Replace ItemId/DataId `3001` with `4739` (Healing Potion).

## ACTION ITEMS
1.  [FIX] Apply ID corrections to `Content/Data/quests*.json`.
2.  [AUDIT] Run `consistency_check.py` post-fix to verify zero violations.
3.  [TEST] Verify `ContentValidationTests` pass.
