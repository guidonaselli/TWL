# Pets System Changelog
> Tracks changes in Pet System, AI, Capture, and Evolution.

## [Unreleased]

### Missing to Production (Pets)
- **Capture Logic**: Implement `CaptureEnemy` mechanics with Item consumption and Probability formula.
- **AI**: Implement Basic Pet AI (Attack/Defend/Follow) in `CombatManager`.
- **Amity**: Implement Amity effects (Stats reduction if rebellious).
- **Evolution**: Implement Rebirth/Evolution lifecycle.

### Added
- **Pet Engine**:
  - `PetService`: Basic management of `ServerPet` instances.
  - `PetDefinition`: JSON schema for base stats and growth.
  - `PetGrowthCalculator`: Implemented stat growth logic.

### Changed
- `ServerCharacter`: Updated `GetSaveData` to include Pet persistence (Partial).
