# Localization Key Conventions

## Naming Conventions
- Keys must be UPPERCASE_SNAKE_CASE.
- Keys must start with a context prefix.

### Valid Prefixes
- `UI_`: User Interface strings (e.g., `UI_LOGIN`, `UI_BACK`).
- `ERR_`: Error messages (e.g., `ERR_CONNECTION_FAILED`).
- `QUEST_`: Quest titles and descriptions (e.g., `QUEST_TITLE_1001`, `QUEST_DESC_1001`).
- `SKILL_`: Skill names and descriptions (e.g., `SKILL_FIREBALL`, `SKILL_DESC_FIREBALL`).
- `ITEM_`: Item names and descriptions (e.g., `ITEM_POTION`, `ITEM_DESC_POTION`).
- `TUTORIAL_`: Tutorial text.
- `ENEMY_`: Enemy names (e.g., `ENEMY_SLIME`).
- `NPC_`: NPC dialog or names (e.g., `NPC_MAREN_HELLO`).

## Usage
- In C# Code: `Loc.T("UI_KEY")` or `Loc.TF("UI_KEY_FORMAT", arg1, arg2)`.
- In JSON Content: `"TitleKey": "QUEST_TITLE_1001"`.

## Localization Files
- `Resources/Strings.resx`: Base language (English default).
- `Resources/Strings.en.resx`: English (Explicit).
- `Resources/Strings.es.resx`: Spanish (Optional/Future).
