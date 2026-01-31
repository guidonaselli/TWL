# Pets Changelog

## [Unreleased]
### Missing
- **Content**: Population of `pets.json` with 20+ capturable creatures (Stats/Skills).
- **AI**: `PetCombatAI` logic (currently pets are passive or basic).
- **Logic**: "Ride" mechanics (Stats boost + Visual).
- **Persistence**: Re-hydration of `PetDefinition` after loading from save.

### Added
- **System**: `PetService` for Capture, Revive, and Amity management.
- **Logic**: Capture Formula (Health % + Level Delta).
- **Logic**: Amity effects (Rebellious state < 20 Amity).
