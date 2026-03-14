# T02: 06-rebirth-system 02

**Slice:** S06 — **Milestone:** M001

## Description

Implement rebirth eligibility enforcement and prestige visibility while preserving character build continuity after rebirth.

Purpose: This delivers REB-02, REB-03, and REB-04 using the transactional rebirth foundation from Plan 06-01.
Output: Rebirth requirement checks + payload/UI propagation for prestige display + retention and display regression tests.

## Must-Haves

- [ ] "Character rebirth enforces minimum level and optional quest/item prerequisites before allowing rebirth"
- [ ] "Character keeps skills and equipped gear across rebirth and can continue using retained gear at level 1"
- [ ] "Rebirth count is visible in character info/nameplate/HUD as prestige metadata"

## Files

- `TWL.Server/Simulation/Managers/RebirthManager.cs`
- `TWL.Server/Simulation/Networking/ServerCharacter.cs`
- `TWL.Server/Simulation/Networking/Components/PlayerQuestComponent.cs`
- `TWL.Shared/Net/Payloads/LoginResponseDto.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Client/Presentation/Managers/PlayerCharacterData.cs`
- `TWL.Client/Presentation/UI/UiGameplay.cs`
- `TWL.Tests/Rebirth/CharacterRebirthRequirementTests.cs`
- `TWL.Tests/Rebirth/RebirthPrestigeDisplayTests.cs`
