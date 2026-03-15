# T03: 09-pet-system-completion 03

**Slice:** S09 — **Milestone:** M001

## Description

Complete PET-05 and PET-06 by formalizing amity KO impact and bond-tier rewards.

Purpose: Make amity and bonding systems explicit, tunable, and test-verified rather than incidental side effects.
Output: Bond metadata model updates, runtime bond mechanics, and focused amity/bonding regression tests.

## Must-Haves

- [x] "Pet amity decreases by exactly 1 on KO/death combat event"
- [x] "Bonding thresholds grant measurable pet benefits (stat bonuses and/or unlock behavior) as amity rises"
- [x] "Bonding effects are deterministic and bounded to prevent runaway scaling"

## Steps

1. **Define Bonding Tiers**: Update `PetDefinition.cs` to include a data structure for bonding rewards (stat multipliers or flat bonuses).
2. **Implement KO Penalty**: Update `ServerPet.cs` or `PetService.cs` to hook into the death event and decrement amity.
3. **Apply Bonding Bonuses**: Update `ServerPet.cs` to calculate and apply stat bonuses based on current amity and definition tiers.
4. **Create Regression Tests**: Implement `PetAmityKoTests.cs` and `PetBondingMechanicsTests.cs` to verify these mechanics.
5. **Verify All Must-Haves**: Run `scripts\verify.ps1` and specific pet system tests.

## Observability Impact

- **Logs**: `PetService` and `ServerPet` should log amity changes (especially on KO) at `Info` level.
- **Metrics**: Current amity and active bond tier should be visible in the pet's state for debugging.
- **Failures**: If amity fails to decrement on KO, it should be caught by `PetAmityKoTests`. If bonding bonuses are miscalculated, `PetBondingMechanicsTests` will fail.

## Files

- `TWL.Shared/Domain/Characters/PetDefinition.cs`
- `TWL.Server/Simulation/Networking/ServerPet.cs`
- `TWL.Server/Services/PetService.cs`
- `TWL.Tests/PetTests/PetBondingMechanicsTests.cs`
- `TWL.Tests/PetTests/PetAmityKoTests.cs`
