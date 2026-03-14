# T03: 09-pet-system-completion 03

**Slice:** S09 — **Milestone:** M001

## Description

Complete PET-05 and PET-06 by formalizing amity KO impact and bond-tier rewards.

Purpose: Make amity and bonding systems explicit, tunable, and test-verified rather than incidental side effects.
Output: Bond metadata model updates, runtime bond mechanics, and focused amity/bonding regression tests.

## Must-Haves

- [ ] "Pet amity decreases by exactly 1 on KO/death combat event"
- [ ] "Bonding thresholds grant measurable pet benefits (stat bonuses and/or unlock behavior) as amity rises"
- [ ] "Bonding effects are deterministic and bounded to prevent runaway scaling"

## Files

- `TWL.Shared/Domain/Characters/PetDefinition.cs`
- `TWL.Server/Simulation/Networking/ServerPet.cs`
- `TWL.Server/Services/PetService.cs`
- `TWL.Tests/PetTests/PetBondingMechanicsTests.cs`
- `TWL.Tests/PetTests/PetAmityKoTests.cs`
