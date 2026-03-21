# T04: 10-combat-progression-integration 04 Summary

**Goal:** Integrate full Phase 10 combat flow (`CMB-04`) so death penalties, pet AI, skill/status behavior, and utility movement seams operate coherently.

## Implemented Changes

1. **Blocked Actions for Dead / In-Combat Players:**
   - Modified `ClientSession.cs` to block movement (`HandleMoveAsync`) and attacks (`HandleAttackAsync`) if the player is dead (`Character.Hp <= 0`).
   - Modified `PetService.cs` to block pet utilities (`UseUtility`) if the player is dead or in combat (`Character.Hp <= 0 || _combatManager.GetCombatant(Character.Id) != null`).

2. **Integrated Status Effect Ticking:**
   - Added `TickStatusEffects(IStatusEngine engine)` to `ServerCombatant.cs`.
   - Updated `TurnEngine.cs` to accept an `IStatusEngine` instance in its constructor and invoke `CurrentCombatant.TickStatusEffects(_statusEngine)` inside `NextTurn()`.
   - Modified `CombatManager.cs` to pass its `_statusEngine` when instantiating `TurnEngine`.

3. **Integration Tests:**
   - Created `CombatFlowIntegrationTests.cs` to ensure that pet AI continues execution after the player dies, status effects tick properly on dead characters without causing crashes, and dead/in-combat players are blocked from using utilities.
   - All tests pass (189 tests total).

## Files Changed
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Server/Services/PetService.cs`
- `TWL.Server/Simulation/Networking/ServerCombatant.cs`
- `TWL.Server/Features/Combat/TurnEngine.cs`
- `TWL.Server/Simulation/Managers/CombatManager.cs`
- `TWL.Tests/Server/Combat/CombatFlowIntegrationTests.cs`
