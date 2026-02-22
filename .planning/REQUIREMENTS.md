# Requirements: The Wonderland Legacy (TWL)

**Defined:** 2026-02-15
**Core Value:** The multiplayer gameplay loop works end-to-end: players can party up for tactical combat, trade items through a player-driven market, form guilds with shared resources, progress their characters through rebirth mechanics, and experience deep pet companion gameplay - all in a secure, persistent shared world.

## v1 Requirements

Requirements for this milestone to achieve production-ready multiplayer systems for commercial launch.

### Infrastructure & Security

- [ ] **INFRA-01**: PostgreSQL persistence replaces FilePlayerRepository with atomic transactions and connection pooling
- [ ] **INFRA-02**: Entity Framework Core 10.0 handles complex write operations (market, guild bank) with migrations
- [ ] **INFRA-03**: Dapper handles high-performance read operations (market browsing, guild rosters)
- [ ] **SEC-01**: Movement validation prevents speed-hacks and teleportation (server calculates max distance per tick)
- [ ] **SEC-02**: Packet replay protection using nonce + timestamp validation (30-second validity window)
- [ ] **SEC-03**: Market transactions use Serializable isolation level to prevent race condition duplication
- [ ] **SEC-04**: All multi-party operations have idempotency keys extending EconomyManager pattern
- [ ] **QUAL-01**: Fix 8 failing content validation tests (quest chains: Hidden Cove, Ruins Expansion, Hidden Ruins; localization keys)

### P2P Market System

- [ ] **MKT-01**: Player can create item listing with price, quantity, and expiration (24-72 hours)
- [ ] **MKT-02**: Player can search market listings with filters (item name, type, price range, rarity)
- [ ] **MKT-03**: Player can purchase listing with atomic gold/item transfer and automatic tax deduction
- [ ] **MKT-04**: Player can cancel own listing before purchase (item returned to inventory)
- [ ] **MKT-05**: Market displays price history showing min/avg/max prices for last N transactions per item
- [ ] **MKT-06**: Transaction fees (5-10% configurable) are deducted from seller proceeds as economy sink
- [ ] **MKT-07**: Listings expire after configured duration and items return to seller inventory
- [ ] **MKT-08**: Direct player-to-player trade window for face-to-face trading with both-party confirmation

### Compound System

- [ ] **CMP-01**: Player can access compound NPC to enhance equipment
- [ ] **CMP-02**: Player can select base item and enhancement materials from inventory
- [ ] **CMP-03**: System calculates success rate based on item level and materials used
- [ ] **CMP-04**: Enhancement success grants permanent stat bonuses to equipment
- [ ] **CMP-05**: Enhancement failure consumes materials but preserves base item (no destruction)
- [ ] **CMP-06**: Non-refundable compound fee prevents listing fee arbitrage exploits

### Party System

- [ ] **PTY-01**: Player can create party (max 4 members) and invite other players
- [ ] **PTY-02**: Player can accept/decline party invitations
- [ ] **PTY-03**: Player can leave party or leader can kick members (not during combat)
- [ ] **PTY-04**: Party members share XP with proximity checks (must be on same map, within range)
- [ ] **PTY-05**: Party members share loot distribution using round-robin or need/greed roll system
- [ ] **PTY-06**: Party UI displays all members with real-time HP/MP/status sync
- [ ] **PTY-07**: Party chat channel provides private communication for coordination
- [ ] **PTY-08**: Tactical formation system allows 3x3 grid positioning (front/mid/back rows)
- [ ] **PTY-09**: Party kick is disabled during combat and boss fights to prevent kick abuse

### Guild System

- [ ] **GLD-01**: Player can create guild with unique name and configurable creation fee (gold sink)
- [ ] **GLD-02**: Player can invite other players to guild with acceptance flow
- [ ] **GLD-03**: Player can leave guild or be kicked by authorized members
- [ ] **GLD-04**: Guild has hierarchical rank system with granular permissions (invite, promote, kick, withdraw storage)
- [ ] **GLD-05**: Guild chat channel broadcasts to all guild members (persists when offline)
- [ ] **GLD-06**: Guild shared storage allows deposit/withdraw with permission-based access control
- [ ] **GLD-07**: Guild bank withdrawal operations have audit logs tracking who/what/when
- [ ] **GLD-08**: New guild members have 1-2 week time gate before storage withdrawal access
- [ ] **GLD-09**: Guild roster displays member list with online status, last login, rank

### Rebirth System

- [ ] **REB-01**: Character can rebirth at level 100+ resetting to level 1 with permanent stat bonuses (10-20 points)
- [ ] **REB-02**: Character rebirth requirements include minimum level and optional quest/item requirement
- [ ] **REB-03**: Character rebirth count is tracked and displayed in character info/nameplate (visible prestige)
- [ ] **REB-04**: Character retains skill trees and equipment after rebirth (can use all gear at level 1)
- [ ] **REB-05**: Rebirth operation is atomic transaction (all-or-nothing stat changes with rollback safety)
- [ ] **REB-06**: Rebirth history has audit trail for debugging and rollback capability
- [ ] **REB-07**: Diminishing returns formula applies (Rebirth 1: 20 stats, 2: 15, 3: 10, 4: 5) to prevent infinite scaling

### Pet System Completion

- [ ] **PET-01**: Pet combat AI makes intelligent skill selections based on target HP, party status, elemental advantage (not random)
- [ ] **PET-02**: pets.json is populated with complete starter region pet roster (20+ pets)
- [ ] **PET-03**: Quest pets can rebirth/evolve while capturable pets cannot (preserved differentiation from WLO)
- [ ] **PET-04**: Pet rebirth grants stat bonuses and evolution to new forms (Pet Rebirth 1: 10 stats, 2: 8, 3: 5)
- [ ] **PET-05**: Pet amity decreases by 1 on KO (knockout) in combat
- [ ] **PET-06**: Pet bonding mechanic rewards high amity with stat bonuses or special abilities
- [ ] **PET-07**: Pet riding system allows player to mount pets for movement speed bonus

### Combat & Progression

- [ ] **CMB-01**: Death penalty deducts 1% of current level EXP (floor 0%) on character death
- [ ] **CMB-02**: Death penalty deducts 1 durability from all equipped items on character death
- [ ] **CMB-03**: Equipment with 0 durability enters "Broken" state (stats disabled until repaired)
- [ ] **CMB-04**: Full combat flow integrates death penalty, pet AI, skill effects, and status system

### Instance & World

- [ ] **INST-01**: Instance system tracks dungeon runs per character per day (max 5)
- [ ] **INST-02**: Instance lockout counter resets daily at server midnight (00:00 UTC)
- [ ] **INST-03**: Instance entry is rejected if player has reached daily limit (5/5 runs)

## v2 Requirements

Deferred to future milestones. Tracked but not in current roadmap.

### Market Enhancements

- **MKT-ADV-01**: Market analytics dashboard with price graphs and volume trends (merchant playstyle)
- **MKT-ADV-02**: Buy orders allowing players to post "WTB" with auto-fulfill when item listed
- **MKT-ADV-03**: Escrow system for high-value trades reducing scam risk

### Social Enhancements

- **SOC-ADV-01**: Friend list system with online status and whisper messages
- **SOC-ADV-02**: Guild skills/buffs providing passive bonuses (XP%, drop rate%) from guild level
- **SOC-ADV-03**: Guild quests/missions with shared objectives and contribution tracking
- **SOC-ADV-04**: Guild vs Guild events with territory control or scheduled PvP

### Progression Enhancements

- **REB-ADV-01**: Class change on rebirth allowing playstyle experimentation
- **REB-ADV-02**: Rebirth-exclusive skills unlocking unique abilities after prestige
- **REB-ADV-03**: Rebirth quest chains with lore connecting to Los Ancestrales/Resonancia

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| Personal player shops (WLO vending) | Creates "ghost town" problem where players AFK in shops instead of exploring world |
| Unlimited guild size | Mega-guild dominance kills server competition; capped at 50 for v1 |
| Item destruction on enhancement failure | Creates rage-quits and negative player sentiment; preserve items to maintain trust |
| New map regions (Selva Esmeralda, Isla Volcana) | Content expansion phase after core systems stable |
| Housing system (tent, furniture, manufacturing) | Separate milestone with dedicated housing/crafting focus |
| PvP systems (duels, arenas, territory) | Post-launch feature after PvE content proven stable |
| Mobile platform support | PC-first launch; mobile port deferred to post-launch |
| Advanced endgame content (legendary dungeons, raids) | Requires stable player population and progression data |
| Real-money trading support | Legal/ethical complexity; focus on premium currency (Gems) only |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| INFRA-01 | Phase 1 | Pending |
| INFRA-02 | Phase 1 | Pending |
| INFRA-03 | Phase 1 | Pending |
| SEC-01 | Phase 2 | Pending |
| SEC-02 | Phase 2 | Pending |
| SEC-03 | Phase 2 | Pending |
| SEC-04 | Phase 2 | Pending |
| QUAL-01 | Phase 3 | Pending |
| MKT-01 | Phase 7 | Pending |
| MKT-02 | Phase 7 | Pending |
| MKT-03 | Phase 7 | Pending |
| MKT-04 | Phase 7 | Pending |
| MKT-05 | Phase 7 | Pending |
| MKT-06 | Phase 7 | Pending |
| MKT-07 | Phase 7 | Pending |
| MKT-08 | Phase 7 | Pending |
| CMP-01 | Phase 8 | Pending |
| CMP-02 | Phase 8 | Pending |
| CMP-03 | Phase 8 | Pending |
| CMP-04 | Phase 8 | Pending |
| CMP-05 | Phase 8 | Pending |
| CMP-06 | Phase 8 | Pending |
| PTY-01 | Phase 4 | Pending |
| PTY-02 | Phase 4 | Pending |
| PTY-03 | Phase 4 | Pending |
| PTY-04 | Phase 4 | Pending |
| PTY-05 | Phase 4 | Pending |
| PTY-06 | Phase 4 | Pending |
| PTY-07 | Phase 4 | Pending |
| PTY-08 | Phase 4 | Pending |
| PTY-09 | Phase 4 | Pending |
| GLD-01 | Phase 5 | Pending |
| GLD-02 | Phase 5 | Pending |
| GLD-03 | Phase 5 | Pending |
| GLD-04 | Phase 5 | Pending |
| GLD-05 | Phase 5 | Pending |
| GLD-06 | Phase 5 | Pending |
| GLD-07 | Phase 5 | Pending |
| GLD-08 | Phase 5 | Pending |
| GLD-09 | Phase 5 | Pending |
| REB-01 | Phase 6 | Pending |
| REB-02 | Phase 6 | Pending |
| REB-03 | Phase 6 | Pending |
| REB-04 | Phase 6 | Pending |
| REB-05 | Phase 6 | Pending |
| REB-06 | Phase 6 | Pending |
| REB-07 | Phase 6 | Pending |
| PET-01 | Phase 9 | Pending |
| PET-02 | Phase 9 | Pending |
| PET-03 | Phase 6 | Pending |
| PET-04 | Phase 6 | Pending |
| PET-05 | Phase 9 | Pending |
| PET-06 | Phase 9 | Pending |
| PET-07 | Phase 9 | Pending |
| CMB-01 | Phase 10 | Pending |
| CMB-02 | Phase 10 | Pending |
| CMB-03 | Phase 10 | Pending |
| CMB-04 | Phase 10 | Pending |
| INST-01 | Phase 10 | Pending |
| INST-02 | Phase 10 | Pending |
| INST-03 | Phase 10 | Pending |

**Coverage:**
- v1 requirements: 61 total
- Mapped to phases: 61 (100% coverage)
- Unmapped: 0

---
*Requirements defined: 2026-02-15*
*Last updated: 2026-02-15 after roadmap creation*

---

# Appendix A: Production Gap Analysis (V1)

**Date**: 2024-05-22
**Status**: Proto-Alpha / Vertical Slice (Incomplete)
**SSOT Compliance**: Low

## 1. Executive Summary

The current codebase represents a **Vertical Slice prototype** with foundational systems (Networking, Basic Combat, Quests, Pets, Premium Economy) in place. It is **NOT production-ready**.

**Maturity Level**: **Proto-Alpha**
*   **Strengths**: Clean architecture (Server-Authoritative, Data-Driven), Basic Networking, initial content pipelines.
*   **Weaknesses**: Missing Critical Features (Market, Persistence, Social), weak Security enforcement (Movement, Anti-Replay), failing content validation.

**Top 5 Production Risks**:
1.  **Persistence Integrity**: Reliance on `FilePlayerRepository` (JSON) is unfit for production. DB migration is stubbed.
2.  **Market/Economy**: Completely missing Player-to-Player economy (Centralized Ledger + Stalls) and Compound system.
3.  **Security**: Lack of authoritative movement validation (Speed/Teleport hacks possible) and Packet Replay protection.
4.  **Content Quality**: 8 Failing Tests indicating broken Quest chains and Localization keys.
5.  **Social Systems**: Missing Party and Guild systems (Chat, Storage, Formation) required for multiplayer loop.

**Single Biggest Bottleneck**: **Persistence Layer**. Without a proper DB (PostgreSQL), all other systems (Market, Guilds, Inventory) cannot be reliably implemented or tested for concurrency.

---

## 2. Feature Coverage Matrix (SSOT-Aligned)

| Domain | Status | Evidence / Notes | Next Best Action |
| :--- | :--- | :--- | :--- |
| **Persistence** | **Prototype** | `FilePlayerRepository.cs` (JSON). `DbService.cs` is a stub. | **Migrate to PostgreSQL** (Atomic Trans, JSONB). |
| **Combat** | **Partial** | `CombatManager.cs` handles skills/death. **Missing**: 1% EXP Loss, 1 Durability Loss. | Implement Death Penalty hooks in `CombatManager`. |
| **Skills** | **Partial** | System exists. **Missing**: Earth/Water/Fire/Wind T1 Packs, Goddess Skills logic. | Populate Skill JSONs & Validators. |
| **Quests** | **Broken** | `ServerQuestManager` exists. **Failing Tests**: 8 tests failing (Chains, Loc). | Fix `PuertoRoca` & `HiddenCove` chains. |
| **Pets** | **Partial** | `PetService` exists. **Missing**: AI Logic, `pets.json` population, Riding. | Implement `PetCombatAI` & populate data. |
| **Economy (Premium)** | **Implemented** | `EconomyManager.cs` handles Gems/Shop (Secure/Idempotent). | Verify Ledger rotation/archival. |
| **Economy (Market)** | **Missing** | **No code found**. SSOT requires Hybrid Market (Ledger + Stalls). | Implement `MarketService` (Listings, Tax). |
| **Social** | **Missing** | No Party/Guild logic in `TWL.Server`. | Implement `PartyManager` & `GuildService`. |
| **World/Instances** | **Stub** | `InstanceService` is empty. No Lockout logic (5/day). | Implement `InstanceLockoutService` (Daily Reset). |
| **Security** | **Weak** | `RateLimiter` exists. **Missing**: Movement Validation, Anti-Replay. | Implement `MovementValidator` & Nonce check. |
| **Observability** | **Partial** | `ServerMetrics` exists. `SecurityLogger` used. | Extend Serilog to all critical paths. |

---

## 3. Contradictions & Blockers Report

### A. SSOT Mismatches (Blockers)
1.  **Instance Lockouts**:
    *   **SSOT**: "5 Runs per Day per Character, resetting at Server-Day".
    *   **Code**: `InstanceService.cs` has no tracking or limit logic.
    *   **Action**: Must implement persistent counters.

2.  **Death Penalty**:
    *   **SSOT**: "1% EXP Loss + 1 Durability Loss per equipped item".
    *   **Code**: `CombatManager.cs` invokes `OnCombatantDeath` but only notifies Quests. No penalty logic.
    *   **Action**: Implement `DeathService` to apply penalties.

3.  **Market Architecture**:
    *   **SSOT**: "Hybrid Market (Centralized Ledger + Player Stalls)".
    *   **Code**: `EconomyManager.cs` only handles Premium/Shop. No P2P market exists.
    *   **Action**: Build `MarketService` from scratch.

### B. Content Integrity (Failing Tests)
The following tests are **FAILING** and block release:
*   `TWL.Tests.Localization.LocalizationValidationTests` (Missing Keys)
*   `TWL.Tests.HiddenCoveTests` (Quest Chain Broken)
*   `TWL.Tests.QuestRuinsExpansionTests` (Quest Chain Broken)
*   `TWL.Tests.HiddenRuinsQuestTests` (Quest Chain Broken)
*   `TWL.Tests.Reliability.WorldLoopObservabilityTests` (Metrics Failure)

### C. Architecture Gaps
*   **Database**: No migrations, no ORM/SQL mapping.
*   **Protocol**: `NetMessage` has no `Sequence` or `Nonce` field, leaving the server vulnerable to Replay Attacks.

---

## 4. Conclusion
The project is in a **pre-production state**. Immediate focus must shift from "adding features" to **stabilizing the core** (Persistence, Security) and **filling critical gaps** (Market, Social) before content expansion.
