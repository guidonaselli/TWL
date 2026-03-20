# T04: 10-combat-progression-integration 04

## Summary
Integrated full Phase 10 combat flow (`CMB-04`) to ensure death penalties, pet AI, skill/status behavior, and utility movement seams operate coherently without regression.

## Implementation Details
1. **Combat Flow Integration Tests**: Created `TWL.Tests/Server/Combat/CombatFlowIntegrationTests.cs` to test the stability of combat subsystems when integrating with the death penalty service.
2. Verified that **Pet Turn Execution** isn't broken when the player dies and the death penalty triggers. The pet is verified to continue receiving combat actions and effectively fighting using its Spd-ordered turn.
3. Verified that **Status Effect Processing** continues running smoothly over a combatant that has logically died and sustained a death penalty, ensuring tick and duration logic handles dead players smoothly.
4. Verified that **Pet Utility Behaviors** remain available correctly via the server architecture even after the player receives a death penalty and falls to 0 HP.

## Tests Added
- `PlayerDeath_DoesNotBreakPetTurn_IntegrationTest`
- `StatusEffect_RemainsStable_AfterDeath_IntegrationTest`
- `PetUtility_RemainsAvailable_AfterOwnerDeathPenalty_IntegrationTest`

## Verification
- `dotnet test TWL.Tests/TWL.Tests.csproj --filter CombatFlowIntegrationTests`
- `dotnet test TWL.Tests/TWL.Tests.csproj -c Release`
