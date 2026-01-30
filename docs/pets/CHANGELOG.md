# Pets Changelog
> Tracks changes to Pet System, AI, and Lifecycle.

## [Unreleased]

### Missing to Production
- **Lifecycle**:
    - **Amity System**: No logic for Amity decrease on KO or low-Amity stat penalties.
    - **Death Penalty**: "Soft Death" (Revive in combat) vs "Hard Death" (Despawn + Amity Loss) logic.
- **AI**: Pets currently do not act independently (Stubbed AI).
- **Capture**: `CaptureEnemy` logic is stubbed.

### Added
- **System**: `PetService` and `PetManager` (JSON loader).
- **Structure**: `ServerPet` class with basic Stats (Str, Con, Int, Wis, Agi).

### Changed
- **Persistence**: Pet state is saved within `ServerCharacter` JSON, but hydration logic in `ClientSession` was added to link `PetDefinition`.
