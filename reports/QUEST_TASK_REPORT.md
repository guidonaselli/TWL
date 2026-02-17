# Quest Implementation Report - 2026-02-17

**Status**: BLOCKED / REPORT ONLY
**Reason**: Anti-collision clause triggered (Potential conflict with daily Orchestrator task).

## Proposed Content Changes (Not Applied)

The following quest definitions are proposed for `Content/Data/quests_islabrisa_side.json` to replace the existing placeholders for Quests 1013 and 1014, matching the design doc `2026-01-22-brisa-island-cont.md`.

### Quest 1013: Favorite Treat
*Original ID matches existing placeholder. Content updated.*
```json
  {
    "QuestId": 1013,
    "Title": "Favorite Treat",
    "Description": "Your monkey seems restless. Nia says there are ripe bananas in the palm groves.",
    "Requirements": [
      1010
    ],
    "Objectives": [
      {
        "Type": "Collect",
        "TargetName": "Banana",
        "RequiredCount": 5,
        "Description": "Collect 5 Bananas.",
        "DataId": 5958
      },
      {
        "Type": "UseItem",
        "TargetName": "Banana",
        "RequiredCount": 1,
        "Description": "Feed the Monkey.",
        "DataId": 5958
      }
    ],
    "Rewards": {
      "Exp": 50,
      "Gold": 0,
      "Items": [
        {
          "ItemId": 4739,
          "Quantity": 2
        }
      ]
    }
  }
```

### Quest 1014: Playtime
*Original ID matches existing placeholder. Content updated.*
```json
  {
    "QuestId": 1014,
    "Title": "Playtime",
    "Description": "The monkey is energetic! Take it to the tide pools and let it play.",
    "Requirements": [
      1013
    ],
    "Objectives": [
      {
        "Type": "Reach",
        "TargetName": "2",
        "RequiredCount": 1,
        "Description": "Reach Costa de Mareas."
      },
      {
        "Type": "Talk",
        "TargetName": "Nia",
        "RequiredCount": 1,
        "Description": "Talk to Nia."
      }
    ],
    "Rewards": {
      "Exp": 100,
      "Gold": 0,
      "Items": []
    }
  }
```

## Dependencies & Blockers

1.  **Amity Rewards**: The design requires granting "Amity +1" as a reward. The current `RewardDefinition` schema in `TWL.Shared.Domain.Quests` does not support Amity.
    *   *Action Required*: Update `RewardDefinition` and `PlayerQuestComponent` to handle `AmityReward`.
2.  **Localization**: New strings ("Favorite Treat", descriptions) require entries in `TWL.Client/Resources/Strings.resx` and corresponding `TitleKey`/`DescriptionKey` in the JSON to pass strict validation.
    *   *Action Required*: Add keys `QUEST_1013_TITLE`, `QUEST_1013_DESC`, etc., to `Strings.resx`.
3.  **Interaction Targets**: The design called for interacting with `pet_monkey_play`. This interaction target does not exist in `interactions.json` or the map.
    *   *Workaround*: Substituted with "Talk to Nia" and "Reach Map 2".
4.  **Item Data Inconsistency**: `quests.json` uses ID 3001 for "Minor Potion", but `items.json` lists 3001 as "Camelia Sandal".
    *   *Action Required*: Audit Item IDs. Used ID 4739 (Healing Potion) for Quest 1013 reward.

## Validation Logic
- **Verified**: Quest IDs 1013/1014 exist in `quests_islabrisa_side.json` (ID Collision avoided by targeting update instead of insert).
- **Verified**: Item ID 5958 (Banana) and 4739 (Healing Potion) exist.
- **Verified**: Reach Objective supports Map ID "2".
