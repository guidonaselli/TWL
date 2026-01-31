# Pets Changelog

## [Unreleased]
### Current Verified State
*   **System**: `PetService` handles Capture, Revive, Amity.
*   **Logic**: Capture Formula and Amity Thresholds implemented.
*   **Status**: Partial (Engine ready, Content/AI missing).

### Production V1 Blockers (P0)
- **Content**: Populate `pets.json` with 20+ capturable creatures.
- **AI**: Implement `PetCombatAI` (Currently pets are passive/basic).
- **Combat**: Update Revive logic to enforce Item/Skill usage (No Gold).

### Next Milestones (P1)
- **Logic**: Pet Riding System (Stats boost + Visual).
- **Persistence**: Fix Re-hydration of `PetDefinition` (Ensure robust loading).
- **Logic**: Amity Penalty update (reduce from -10 to -1 per KO).

### Added
- **Logic**: Amity effects (Rebellious state < 20 Amity).
