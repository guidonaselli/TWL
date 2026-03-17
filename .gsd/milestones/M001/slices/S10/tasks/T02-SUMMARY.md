# T02: 10-combat-progression-integration 02 Summary

## What was implemented
- Added `Durability` and `MaxDurability` properties to `TWL.Shared.Domain.Models.Item`.
- Added computed property `IsBroken` to `Item`, which evaluates to true when `MaxDurability > 0` and `Durability <= 0`.
- Modified `TWL.Server.Services.Combat.DeathPenaltyService.ApplyExpPenalty` to reduce the durability of all equipped items by 1.
- Updated `TWL.Server.Simulation.Networking.ServerCharacter` to persist the new item durability attributes across `Inventory`, `Equipment`, and `Bank`.
- Overrode character combat stats (`Atk`, `Def`, `Mat`, `Mdf`, `Spd`, `MaxHp`, `MaxSp`) in `ServerCharacter` to dynamically include stats from equipped items only if they are not broken (`IsBroken == false`).
- Ignored `.gsd/STATE.md` updates because memory indicates it's dynamically generated and the system executes `node .gsd/state.mjs .` to read state.

## Files Changed
- `TWL.Shared/Domain/Models/Item.cs`
- `TWL.Server/Services/Combat/DeathPenaltyService.cs`
- `TWL.Server/Simulation/Networking/ServerCharacter.cs`

## Tests Added
- `TWL.Tests/Server/Equipment/DurabilitySystemTests.cs`:
  - `Item_IsBroken_TrueWhenDurabilityZeroAndMaxDurabilityGreaterThanZero`
  - `DeathPenalty_ReducesEquippedItemsDurabilityByOne`
  - `BrokenItems_DoNotContributeToStats`
  - `LegacySaves_MissingDurability_HandledAsIndestructible`
