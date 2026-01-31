# Pet System Changelog

## [Unreleased]

### Missing
- **Content**: `pets.json` is sparse. Needs population with 20+ capturable mobs.
- **AI**: `PetCombatAI` (Attack/Defend/Heal logic) is not implemented in Combat.

### Existing
- **Lifecycle**: Capture (Probability + Item Cost), Death (KO/Despawn), Revive (Gold Cost), Rebirth (Level 100 Reset).
- **Stats**: Amity System (Rebellious < 20, Bonus > 90) fully implemented in `ServerPet`.
- **Logic**: `PetService.CaptureEnemy` enforces `RequiredItemId` and HP% probability.
