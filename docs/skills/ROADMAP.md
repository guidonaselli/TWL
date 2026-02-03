# SKILLS ROADMAP

> **JULES-CONTEXT**: This roadmap tracks skill implementation. Skills are defined in
> `Content/Data/skills.json`. All skills must follow `CONTENT_RULES.md` (unique IDs,
> tier budgets, stage upgrade consistency). Use `TWL.Tests.ContentValidationTests` to verify.

## Definition of Done (DoD)
- Skill defined in `skills.json` with complete schema fields
- Minimum 3 branches per element pack (Physical, Magical, Support)
- Zero names copied from WLO/NWLO/Wonderland M (Exception: Goddess Skill names are original)
- Unit test validating damage/effect calculations
- Localization keys added for display names and descriptions

---

## Elemental Identity Guide

> **JULES-CONTEXT**: When creating new skills, follow these elemental identities strictly.
> They define what each element FEELS like in combat.

| Element | Identity | Primary Stat | Combat Role |
|---------|----------|-------------|-------------|
| **Earth** | Endurance & Protection | CON | Tank, shields, CC (seal) |
| **Water** | Healing & Sustain | WIS | Healer, cleanse, debuffs |
| **Fire** | Burst Damage | STR/INT | DPS, buffs to offense, DoT |
| **Wind** | Speed & Control | SPD | Evasion, AoE, speed manipulation |

**Elemental Cycle**: Water > Fire > Wind > Earth > Water (1.5x advantage, 0.5x disadvantage)

---

## Skill ID Convention

| Range | Element | Tier |
|-------|---------|------|
| 1001-1299 | Earth | T1 (1001-1203), T2 (1010, 1110, 1210) |
| 2001-2004 | Special | Goddess Skills (reserved, do not reuse) |
| 3001-3299 | Water | T1 (3001-3203), T2 (3010, 3110, 3210) |
| 4001-4299 | Fire | T1 (4001-4203), T2 (4010, 4110, 4210) |
| 5001-5299 | Wind | T1 (5001-5203), T2 (5010, 5110, 5210) |
| 6001-6999 | Special | Quest-granted skills |
| 8001-8999 | Special | Legendary/trial skills |

---

## Backlog

### P0 - Core System (DONE)
- [x] **SKL-001**: JSON schema + validator (unique IDs, references, tier constraints)
- [x] **SKL-002**: Mastery-by-use + thresholds + stage upgrades (Rank 6 -> Stage 2, Rank 12 -> Stage 3)
- [x] **SKL-003**: UnlockRules (level/stats/pre-skill mastery/quest flag)
- [x] **SKL-010**: Counter system (stacking policies, priority, resistance logic)

### P1 - Element Packs (IN PROGRESS)
- [ ] **SKL-004**: Enforce tier budgets in validator (SP range, coefficients, control limits)
- [ ] **SKL-005**: Pack T1 Earth (3 branches: Rock Smash, Stone Bullet, Gaia's Protection) + tests
- [ ] **SKL-006**: Pack T1 Water (3 branches: Aqua Impact, Water Ball, Aqua Recover) + tests
- [ ] **SKL-007**: Pack T1 Fire (3 branches: Flame Smash, Fireball, Fiery Will) + tests
- [ ] **SKL-008**: Pack T1 Wind (3 branches: Wind Slash, Air Blade, Breeze) + tests
- [ ] **SKL-009**: Packs T2 (all elements, stage evolution from T1 Rank 10)
- [ ] **SKL-011**: Goddess Skills (Diminution, Support Seal, Ember Surge, Untouchable Veil) - auto-grant by element on login

### P2 - Specialization
- [ ] **SKL-012**: Life Skills (Alchemy, Mining, Fishing passives affecting economy rates)
- [ ] **SKL-013**: Legendary quest skills (Dragon Slash, Fairy's Blessing)
- [ ] **SKL-014**: Rebirth-exclusive skills (new tier unlocked after first rebirth)
