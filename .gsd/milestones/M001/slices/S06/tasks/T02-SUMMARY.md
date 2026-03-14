---
id: T02
parent: S06
milestone: M001
provides:
  - Rebirth eligibility enforcement with quest and item prerequisites.
  - Prestige (rebirth count) visibility in client HUD and network DTOs.
  - Retention of skills and gear across rebirth verified.
key_files:
  - TWL.Server/Simulation/Managers/RebirthManager.cs
  - TWL.Server/Simulation/Networking/ClientSession.cs
  - TWL.Client/Presentation/UI/UiGameplay.cs
  - TWL.Tests/Rebirth/CharacterRebirthRequirementTests.cs
  - TWL.Tests/Rebirth/RebirthPrestigeDisplayTests.cs
key_decisions:
  - Rebirth requirements are loaded from a content JSON (`Content/Data/rebirth.json`) to maintain a data-driven architecture.
  - Required items are consumed during the atomic rebirth transaction.
  - Rebirth count is displayed as "(RN)" next to the level in the HUD.
patterns_established:
  - Prerequisite checking via optional component injection in domain services.
  - Extension of existing DTOs to propagate new progression metadata.
observability_surfaces:
  - Detailed failure reasons for rebirth prerequisites in `RebirthHistory` (Audit Log).
  - Visible prestige markers in client UI.
duration: 60m
verification_result: passed
completed_at: 2026-03-14
blocker_discovered: false
---

# T02: 06-rebirth-system 02

**Implemented rebirth eligibility enforcement and prestige visibility while preserving character build continuity after rebirth.**

## What Happened

In this task, I enhanced the rebirth system to enforce level, quest, and item requirements. The `RebirthManager` was updated to support a data-driven requirements model loaded from `Content/Data/rebirth.json`. I implemented the prerequisite checks by integrating with the `PlayerQuestComponent` to verify quest completion and checking character inventory for required items, which are now consumed upon successful rebirth.

On the presentation side, I updated the network DTOs (`LoginSuccessResponseDto`, `PlayerDataDTO`) and client-side models (`PlayerCharacterData`, `GameSaveData`, `PlayerCharacter`) to propagate and store the `RebirthLevel`. The HUD was modified to display this rebirth count next to the character's level, providing immediate visual prestige feedback.

Comprehensive tests were added to verify:
- Level requirement enforcement.
- Quest completion prerequisite checks.
- Item possession and consumption logic.
- Atomic recording of failure reasons in the rebirth history.
- Correct propagation and display of prestige metadata on the client.

## Verification

- **Unit Tests**: Ran `CharacterRebirthRequirementTests` and `RebirthPrestigeDisplayTests`.
  - Level checks (Pass)
  - Quest prerequisite checks (Pass)
  - Item consumption transaction (Pass)
  - Rebirth history audit for failures (Pass)
  - Client-side prestige storage (Pass)
- **Regression**: Rerun `CharacterRebirthTransactionTests` to ensure core transactional integrity remains intact (Pass).
- **Build**: Verified the entire solution compiles correctly after DTO and UI changes.

## Diagnostics

- **Rebirth History**: Inspect the `Reason` field in `RebirthHistory` records to see specific prerequisite failure messages (e.g., "Required quest 5000 not completed").
- **Client HUD**: The character level in the top-left HUD now shows `(RX)` where X is the rebirth level (visible only if > 0).

## Deviations

- Added `LoadRequirements` to `RebirthManager` and wired it into `ServerWorker` to ensure requirements are content-driven, rather than hardcoded.
- Updated `ServerWorker` constructor and several unit tests (`ShutdownTests`, `GracefulShutdownTests`) that manually instantiate the worker to include the `IRebirthService` dependency.

## Known Issues

- (none)

## Files Created/Modified

- `TWL.Shared/Domain/Models/RebirthRequirements.cs` — New model for rebirth prerequisites.
- `TWL.Shared/Domain/DTO/PlayerDataDTO.cs` — Added `RebirthLevel` and `Level`.
- `TWL.Shared/Net/Payloads/LoginSuccessResponseDto.cs` — Added `RebirthLevel` and `Level`.
- `TWL.Shared/Domain/Characters/PlayerCharacter.cs` — Added `RebirthLevel` property and updated `SetProgress`.
- `TWL.Server/Simulation/Managers/IRebirthService.cs` — Updated interface with requirement loading and prerequisite checks.
- `TWL.Server/Simulation/Managers/RebirthManager.cs` — Implemented prerequisite enforcement and item consumption.
- `TWL.Server/Simulation/Networking/ClientSession.cs` — Passed `QuestComponent` to rebirth service.
- `TWL.Server/Simulation/ServerWorker.cs` — Wired requirements loading and DI.
- `TWL.Client/Presentation/Models/GameSaveData.cs` — Added `RebirthLevel`.
- `TWL.Client/Presentation/Managers/PlayerCharacterData.cs` — Updated mapping.
- `TWL.Client/Presentation/UI/UiGameplay.cs` — Updated HUD to show prestige.
- `TWL.Client/Presentation/Scenes/SceneGameplay.cs` — Updated character progress sync.
- `TWL.Tests/Rebirth/CharacterRebirthRequirementTests.cs` — New requirement enforcement tests.
- `TWL.Tests/Rebirth/RebirthPrestigeDisplayTests.cs` — New prestige metadata tests.
- `TWL.Tests/Reliability/ShutdownTests.cs` — Fixed manual instantiation.
- `TWL.Tests/Server/Simulation/GracefulShutdownTests.cs` — Fixed manual instantiation.
- `Content/Data/rebirth.json` — New configuration file for rebirth requirements.
