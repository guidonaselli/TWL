# QUESTS ROADMAP

> **JULES-CONTEXT**: This roadmap tracks quest system implementation. Quests are defined in
> `Content/Data/quests.json`. All quests must follow `CONTENT_RULES.md` (unique IDs, idempotent
> rewards, valid prerequisites). Design docs are in `docs/quests/design/`.
> When implementing questlines, reference `docs/world/WORLD_REGIONS.md` for NPC names and map IDs.

## Definition of Done (DoD)
- Quest defined in `quests.json` with complete schema fields
- All prerequisites, objectives, and rewards validated by `ContentValidationTests`
- Reward idempotency verified (re-completing doesn't duplicate)
- Localization keys added for quest name, description, objective texts
- Zero names/concepts copied from WLO/NWLO/Wonderland M

---

## Quest ID Convention

| Range | Region / Type | Design Doc |
|-------|--------------|------------|
| 1001-1099 | Isla Brisa (main + side) | `2026-01-22-intro-arc.md` |
| 1100-1199 | Puerto Roca transition | `2026-01-22-jungle-access.md` |
| 1200-1299 | Hidden Ruins arc | `2024-05-24-hidden-ruins.md` |
| 1300-1399 | Signal/Radio arc | `2024-05-22-signs-of-life.md` |
| 1400-1499 | Hidden Cove arc | `2024-05-22-hidden-cove.md` |
| 2001-2099 | Sidequests (life skills, alchemy) | Various |
| 3001-3099 | Combat training sidequests | `2026-01-22-jungle-access.md` |
| 8001-8099 | Special skill trial quests | `2024-12-07-special-skill-quests.md` |
| 9001-9099 | Housing quests | `docs/housing/ROADMAP.md` |

---

## Backlog

### P0 - Core System
- [ ] **QST-001**: JSON schema + validator (prereqs, objectives, rewards, repeatability, expiry)
- [ ] **QST-002**: QuestEngine consumes server-side events (kill/drop/interact/craft/instance)
- [ ] **QST-003**: Objective types: `KillCount`, `Collect`, `Deliver`, `Interact`, `Explore`, `Reach`
- [ ] **QST-005**: Idempotent rewards + audit logging

### P1 - Content (Questlines)
- [ ] **QST-004**: Advanced objectives: `Craft/Compound`, `Party/Guild`, `Fish`, `UseItem`, `ShowItem`
- [ ] **QST-006**: Isla Brisa questline (Quests 1001-1018). Tutorial arc introducing all core loops.
- [ ] **QST-007**: Isla Brisa sidequests (1008-1010 pet, 1013-1014 bonding, 1050-1054 lore, 1060 supplies)
- [ ] **QST-008**: Puerto Roca questline (Quests 1100-1103). Citizenship arc with combat challenges.
- [ ] **QST-009**: Puerto Roca sidequests (2010 fishing, 2011 alchemy, 3001 combat training)
- [ ] **QST-010**: Tent Quest (Quest 9001-9004). Housing unlock requiring Level 10+, materials, gold.
- [ ] **QST-011**: Vehicle Quests (Quests 1200-1201). Canoe and Raft crafting for island travel.

### P2 - Advanced Content
- [ ] **QST-012**: Selva Esmeralda questline (Quests 2001+). Jungle exploration, ruins discovery.
- [ ] **QST-013**: Special skill trial quests (8001 Dragon Slash, 8002 Fairy's Blessing)
- [ ] **QST-014**: Repeatable daily quests (bounty board, gathering dailies)
- [ ] **QST-015**: Quest failure conditions (time limits, NPC death, instance wipes)
