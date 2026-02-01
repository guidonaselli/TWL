# PETS ROADMAP

> **JULES-CONTEXT**: This roadmap tracks pet system implementation. Pet definitions go in
> `Content/Data/pets.json`. All pets must follow `CONTENT_RULES.md`. Quest-unique pets are
> granted via `PetUnlockId` in quest rewards. Capturable pets use the combat capture system.
> Reference: `GAMEPLAY_CONTRACTS.md` Section 3 for pet death/revive rules.

## Definition of Done (DoD)
- Pet defined in `pets.json` with complete schema fields
- Stats, growth curves, and capture rules validated by tests
- Combat AI tested (basic: attack strongest, heal weakest)
- Zero names/concepts copied from WLO/NWLO/Wonderland M

---

## Pet Categories

| Category | Acquisition | Tradeable | Example |
|----------|------------|-----------|---------|
| Capturable | Combat capture (HP threshold + level check) | Yes (if not bound) | Green Slime, Fire Wolf |
| QuestUnique | Quest reward (`PetUnlockId`) | No (bound) | Mono Travieso, Stone Spirit |
| HumanLike | Special quest chain | No (bound) | Prototype Android |

## Pet ID Convention

| Range | Source |
|-------|--------|
| 1001-1099 | Capturable wild creatures |
| 1011, 1054 | Quest-unique (Isla Brisa) |
| 2001-2099 | HumanLike companions |
| 3001-3099 | Utility pets (delivery, gathering) |

---

## Backlog

### P1 - Core System
- [ ] **PET-001**: Persistent pet model (ownership, stats, element, level, amity) in PostgreSQL
- [ ] **PET-002**: Pet in combat (SPD-based turns, basic AI: attack/heal/defend)
- [ ] **PET-003**: Capture system: combat capture with HP threshold + level cap + base chance %
- [ ] **PET-004**: Quest-unique pet rules: `PetUnlockId` reward type, bound to character, non-tradeable
- [ ] **PET-005**: Anti-dupe protections (cannot duplicate pets via trade/reconnect)

### P2 - Progression & Features
- [ ] **PET-006**: Amity system: bonding quests increase Amity, higher Amity = better performance
- [ ] **PET-007**: Pet riding (mount utility): speed boost + visual change on overworld
- [ ] **PET-008**: Pet rebirth/evolution: reset level for stat boost + new skill unlocks (Legend tier)
- [ ] **PET-009**: Pet utilities: Delivery (carry goods), Gathering (resource collection)
- [ ] **PET-010**: Populate 20+ capturable creatures across all regions (5 per region minimum)
