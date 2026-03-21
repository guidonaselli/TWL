# T04: 10-combat-progression-integration 04 Summary

**Slice:** S10 — **Milestone:** M001

## Implemented Features
1. **Blocked Pet Utilities During Combat/Death**: Modified `PetService.UseUtility` to prevent activating or deactivating utilities (like mounting or gathering) while the player is engaged in combat (`EncounterId > 0`) or dead (`Hp <= 0`).
2. **Handle Status Effects on Death**: In `CombatManager.cs`, added calls to `CleanseDebuffs` and `DispelBuffs` upon a combatant's death to prevent lingering status effects from ticking while the character is in a death penalty state.
3. **Visually Hide Mount in Combat**: Updated the client's `PlayerView.cs` to prevent the visual rendering of the mount asset while the player is engaged in combat. This prevents clutter while preserving the actual mounted state on the server, which maintains stat bonuses granted by items like saddles.

## Modified Files
- `TWL.Server/Services/PetService.cs`
- `TWL.Server/Simulation/Managers/CombatManager.cs`
- `TWL.Client/Presentation/Views/PlayerView.cs`
- `TWL.Tests/Server/Combat/CombatFlowIntegrationTests.cs` (New tests added)

## Testing
- Created `CombatFlowIntegrationTests` in `TWL.Tests` to assert that:
  - Death penalties apply successfully and clear status effects immediately.
  - Pet AI execution is not broken after the owner dies in combat.
  - Pet utility usage (specifically `Mount`) returns `false` if attempted while in combat or dead.
