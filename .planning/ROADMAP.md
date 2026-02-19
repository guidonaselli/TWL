# Roadmap: The Wonderland Legacy (TWL)

## Overview

This roadmap delivers production-ready multiplayer systems for commercial launch of TWL, a turn-based MMORPG. Starting with foundational infrastructure (PostgreSQL migration, security hardening, content fixes), we build the core social systems (Party, Guild), followed by the economy layer (P2P Market, Compound), and progression mechanics (Rebirth, Pet completion, Combat integration). Each phase completes a coherent capability that brings us closer to the commercial launch target.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [x] **Phase 1: Infrastructure Foundation** - PostgreSQL migration and production-grade persistence
- [ ] **Phase 2: Security Hardening** - Anti-cheat, anti-replay, and transaction safety
- [ ] **Phase 3: Content Quality** - Fix broken quest chains and localization
- [ ] **Phase 4: Party System** - Invite, join, XP/loot sharing, tactical formation
- [ ] **Phase 5: Guild System** - Create, join, chat, permissions, shared storage
- [ ] **Phase 6: Rebirth System** - Character and pet prestige progression
- [ ] **Phase 7: P2P Market System** - Item listings, search, purchases, price history
- [ ] **Phase 8: Compound System** - Equipment enhancement with success/failure mechanics
- [ ] **Phase 9: Pet System Completion** - Combat AI, amity, bonding, riding, data population
- [ ] **Phase 10: Combat & Progression Integration** - Death penalty, durability, instance lockouts, full combat flow

## Phase Details

### Phase 1: Infrastructure Foundation
**Goal**: Production-grade database persistence replaces JSON files, enabling concurrent writes and ACID transactions
**Depends on**: Nothing (foundational blocker)
**Requirements**: INFRA-01, INFRA-02, INFRA-03
**Success Criteria** (what must be TRUE):
  1. Player data persists in PostgreSQL with atomic transactions and connection pooling
  2. Entity Framework Core handles complex write operations (market, guild bank) with migration tracking
  3. Dapper handles high-performance read operations (market browsing, guild rosters) with 2x+ speed vs EF Core
  4. FilePlayerRepository is removed and all player operations use PostgreSQL
  5. Database migrations are version-controlled and can roll forward/backward
**Plans**: 2 plans

Plans:
- [x] 01-01-PLAN.md -- EF Core infrastructure: GameDbContext, entity configurations, NpgsqlDataSource pooling, initial migration
- [x] 01-02-PLAN.md -- DbPlayerRepository (hybrid EF Core + Dapper), DI swap, FilePlayerRepository removal

### Phase 2: Security Hardening
**Goal**: Server prevents cheating through movement validation, anti-replay protection, and transaction race condition prevention
**Depends on**: Phase 1 (requires PostgreSQL for transaction isolation)
**Requirements**: SEC-01, SEC-02, SEC-03, SEC-04
**Success Criteria** (what must be TRUE):
  1. Server rejects movement packets that exceed max distance per tick (prevents speed-hacks and teleportation)
  2. Server rejects packet replays using nonce + timestamp validation (30-second validity window)
  3. Market transactions use Serializable isolation level with row-level locks (no duplication exploits)
  4. All multi-party operations (market, trade, guild bank) have idempotency keys preventing duplicate operations
**Plans**: 3 plans

Plans:
- [ ] 02-01-PLAN.md -- Packet replay protection (nonce + timestamp) and pre-dispatch replay gate
- [ ] 02-02-PLAN.md -- Server-authoritative movement validation (max distance per tick, anti-speedhack checks)
- [ ] 02-03-PLAN.md -- Serializable transaction and shared idempotency foundations for valuable operations

### Phase 3: Content Quality
**Goal**: All content validation tests pass, broken quest chains are fixed, and missing localization keys are resolved
**Depends on**: Nothing (can run in parallel with other phases)
**Requirements**: QUAL-01
**Success Criteria** (what must be TRUE):
  1. All 8 failing content validation tests pass (Hidden Cove, Ruins Expansion, Hidden Ruins quest chains)
  2. Missing localization keys are added to localization files
  3. Quest chain progression works end-to-end without blocking players
**Plans**: 3 plans

Plans:
- [ ] 03-01-PLAN.md -- Hidden Ruins and Ruins Expansion quest-chain stabilization (1301-1307) with stronger regression coverage
- [ ] 03-02-PLAN.md -- Hidden Cove chain stabilization (1401-1404, 2401) across quest and interaction contracts
- [ ] 03-03-PLAN.md -- Localization key closure and arc-scoped localization regression guardrails for phase-3 content

### Phase 4: Party System
**Goal**: Players can form parties of up to 4 members, share XP and loot, and use tactical formation for combat
**Depends on**: Phase 2 (requires movement validation for proximity checks)
**Requirements**: PTY-01, PTY-02, PTY-03, PTY-04, PTY-05, PTY-06, PTY-07, PTY-08, PTY-09
**Success Criteria** (what must be TRUE):
  1. Player can create party, invite others, and receive acceptance/decline responses
  2. Player can leave party or be kicked by leader (kick disabled during combat/boss fights)
  3. Party members share XP when on same map and within range (proximity checks enforced)
  4. Party members share loot using round-robin or need/greed roll system
  5. Party UI displays all members with real-time HP/MP/status sync
  6. Party chat channel provides private communication visible only to party members
  7. Tactical formation system allows 3x3 grid positioning (front/mid/back rows) affecting combat
**Plans**: 4 plans

Plans:
- [ ] 04-01-PLAN.md -- Party foundation: invite/accept/decline/leave/kick lifecycle, combat-safe kick policy, and quest-gating parity fix
- [ ] 04-02-PLAN.md -- Party XP and loot sharing with same-map/proximity enforcement and deterministic distribution tests
- [ ] 04-03-PLAN.md -- Real-time party member sync and private party chat with client HUD integration
- [ ] 04-04-PLAN.md -- Tactical 3x3 formation with server validation and combat row integration

### Phase 5: Guild System
**Goal**: Players can create guilds, manage hierarchical ranks, use guild chat, and access shared storage with permission controls
**Depends on**: Phase 4 (reuses party patterns for invites/membership)
**Requirements**: GLD-01, GLD-02, GLD-03, GLD-04, GLD-05, GLD-06, GLD-07, GLD-08, GLD-09
**Success Criteria** (what must be TRUE):
  1. Player can create guild with unique name and configurable creation fee (gold sink)
  2. Player can invite others to guild with acceptance flow (reuses party invite pattern)
  3. Player can leave guild or be kicked by authorized members based on rank permissions
  4. Guild has hierarchical rank system with granular permissions (invite, promote, kick, withdraw storage)
  5. Guild chat channel broadcasts to all guild members (persists when offline)
  6. Guild shared storage allows deposit/withdraw with permission-based access control
  7. Guild bank withdrawal operations have audit logs tracking who/what/when
  8. New guild members have 1-2 week time gate before storage withdrawal access (prevents guild bank theft)
  9. Guild roster displays member list with online status, last login, rank
**Plans**: 4 plans

Plans:
- [ ] 05-01-PLAN.md -- Guild lifecycle foundation: create/invite/accept/decline/leave/kick with unique-name and creation-fee enforcement
- [ ] 05-02-PLAN.md -- Rank hierarchy and centralized permission enforcement for invite/promote/kick/withdraw actions
- [ ] 05-03-PLAN.md -- Guild chat with offline persistence plus roster sync (rank, online status, last login) and client guild UI
- [ ] 05-04-PLAN.md -- Guild shared storage with permission + tenure-gated withdrawals and append-only audit logging

### Phase 6: Rebirth System
**Goal**: Characters and pets can rebirth to gain permanent stat bonuses and prestige, creating long-term progression loop
**Depends on**: Phase 1 (requires atomic transactions for rebirth operation)
**Requirements**: REB-01, REB-02, REB-03, REB-04, REB-05, REB-06, REB-07, PET-03, PET-04
**Success Criteria** (what must be TRUE):
  1. Character can rebirth at level 100+ resetting to level 1 with permanent stat bonuses (20/15/10/5 diminishing returns)
  2. Character rebirth requires minimum level and optional quest/item requirement
  3. Character rebirth count is tracked and displayed in character info/nameplate (visible prestige)
  4. Character retains skill trees and equipment after rebirth (can use all gear at level 1)
  5. Rebirth operation is atomic transaction with rollback safety (all-or-nothing stat changes)
  6. Rebirth history has audit trail for debugging and rollback capability
  7. Quest pets can rebirth/evolve while capturable pets cannot (preserved differentiation)
  8. Pet rebirth grants stat bonuses and evolution to new forms (10/8/5 diminishing returns)
**Plans**: 4 plans

Plans:
- [ ] 06-01-PLAN.md -- Character rebirth transactional foundation with 20/15/10/5 formula, atomic mutation, and auditable history records
- [ ] 06-02-PLAN.md -- Character rebirth eligibility gates, build retention, and client prestige display in character info/nameplate/HUD
- [ ] 06-03-PLAN.md -- Pet rebirth policy completion with quest-vs-capturable differentiation, 10/8/5 diminishing bonuses, and evolution/action routing
- [ ] 06-04-PLAN.md -- End-to-end and rollback/audit regression suite validating character + pet rebirth integration and quest-gating continuity

### Phase 7: P2P Market System
**Goal**: Players can list items for sale, search listings, buy from other players, with centralized ledger and automatic tax/gold transfer
**Depends on**: Phase 2 (requires Serializable isolation and idempotency), Phase 5 (guild taxes depend on guild infrastructure)
**Requirements**: MKT-01, MKT-02, MKT-03, MKT-04, MKT-05, MKT-06, MKT-07, MKT-08
**Success Criteria** (what must be TRUE):
  1. Player can create item listing with price, quantity, and expiration (24-72 hours)
  2. Player can search market listings with filters (item name, type, price range, rarity)
  3. Player can purchase listing with atomic gold/item transfer and automatic tax deduction (5-10% configurable)
  4. Player can cancel own listing before purchase (item returned to inventory)
  5. Market displays price history showing min/avg/max prices for last N transactions per item
  6. Listings expire after configured duration and items return to seller inventory
  7. Direct player-to-player trade window for face-to-face trading with both-party confirmation (extends TradeManager)
  8. All market operations are atomic with idempotency keys (no duplication exploits)
**Plans**: 5 plans

Plans:
- [ ] 07-01-PLAN.md -- Market foundation contracts, persistence schema, and opcode/session wiring for server-authoritative listings
- [ ] 07-02-PLAN.md -- Listing lifecycle operations: create, cancel, expiration scheduling, and item-return safety
- [ ] 07-03-PLAN.md -- Listing search/filter API and min/avg/max price-history projection with client ingestion
- [ ] 07-04-PLAN.md -- Atomic purchase settlement with configurable tax and operation-id idempotency guards
- [ ] 07-05-PLAN.md -- Direct player-to-player trade window (dual confirmation) and client market/trade integration

### Phase 8: Compound System
**Goal**: Players can enhance equipment through compound NPC with success/failure mechanics and economy sink
**Depends on**: Phase 7 (extends market transaction patterns)
**Requirements**: CMP-01, CMP-02, CMP-03, CMP-04, CMP-05, CMP-06
**Success Criteria** (what must be TRUE):
  1. Player can access compound NPC to enhance equipment
  2. Player can select base item and enhancement materials from inventory
  3. System calculates success rate based on item level and materials used
  4. Enhancement success grants permanent stat bonuses to equipment
  5. Enhancement failure consumes materials but preserves base item (no destruction, no rage-quits)
  6. Non-refundable compound fee prevents listing fee arbitrage exploits
**Plans**: 5 plans

Plans:
- [ ] 08-01-PLAN.md -- Compound service/DTO foundation with enhancement metadata persistence and DI wiring
- [ ] 08-02-PLAN.md -- Compound NPC access path, interaction configuration, and inventory selection validation
- [ ] 08-03-PLAN.md -- Success-rate policy and success/failure outcome engine with base-item preservation
- [ ] 08-04-PLAN.md -- Non-refundable compound fee and operation-id idempotency safeguards
- [ ] 08-05-PLAN.md -- Client integration rewrite to server-authoritative flow plus phase acceptance coverage

### Phase 9: Pet System Completion
**Goal**: Pet combat AI, amity/bonding mechanics, riding system, and complete starter region pet roster are functional
**Depends on**: Phase 6 (pet rebirth foundation), Phase 10 (combat flow for AI testing)
**Requirements**: PET-01, PET-02, PET-05, PET-06, PET-07
**Success Criteria** (what must be TRUE):
  1. Pet combat AI makes intelligent skill selections based on target HP, party status, elemental advantage (not random)
  2. pets.json is populated with complete starter region pet roster (20+ pets with stats, skills, evolution paths)
  3. Pet amity decreases by 1 on KO (knockout) in combat
  4. Pet bonding mechanic rewards high amity with stat bonuses or special abilities
  5. Pet riding system allows player to mount pets for movement speed bonus
**Plans**: 5 plans

Plans:
- [ ] 09-01-PLAN.md -- Pet combat AI policy hardening with deterministic intelligent decision tests
- [ ] 09-02-PLAN.md -- Starter roster and capture-world content completion with validation coverage
- [ ] 09-03-PLAN.md -- Amity KO and bond-tier reward mechanics completion
- [ ] 09-04-PLAN.md -- Riding utility routing, mounted movement bonus, and client/server flow integration
- [ ] 09-05-PLAN.md -- Phase-level PET acceptance and cross-system integration verification

### Phase 10: Combat & Progression Integration
**Goal**: Death penalty, durability system, instance lockouts, and full combat flow are integrated and functional
**Depends on**: Phase 4 (party system for combat testing), Phase 9 (pet AI for combat testing)
**Requirements**: CMB-01, CMB-02, CMB-03, CMB-04, INST-01, INST-02, INST-03
**Success Criteria** (what must be TRUE):
  1. Character death deducts 1% of current level EXP (floor 0%) and 1 durability from all equipped items
  2. Equipment with 0 durability enters "Broken" state (stats disabled until repaired)
  3. Instance system tracks dungeon runs per character per day (max 5) with daily reset at server midnight (00:00 UTC)
  4. Instance entry is rejected if player has reached daily limit (5/5 runs)
  5. Full combat flow integrates death penalty, pet AI, skill effects, and status system
**Plans**: 5 plans

Plans:
- [ ] 10-01-PLAN.md -- Combat death-penalty foundation: server-authoritative 1% EXP loss and idempotent death-event wiring
- [ ] 10-02-PLAN.md -- Durability and broken-state system: -1 durability on death and stat-disable behavior at 0 durability
- [ ] 10-03-PLAN.md -- Instance run quotas: per-instance 5/day tracking, UTC-midnight reset, and entry rejection at 5/5
- [ ] 10-04-PLAN.md -- Full combat-flow integration across death penalties, pet/status systems, and utility/movement seams
- [ ] 10-05-PLAN.md -- Phase-level CMB/INST acceptance suite and verification artifact for execution handoff

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 → 9 → 10

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Infrastructure Foundation | 2/2 | ✅ Complete | 2026-02-19 |
| 2. Security Hardening | 0/3 | Not started | - |
| 3. Content Quality | 0/3 | Not started | - |
| 4. Party System | 0/4 | Not started | - |
| 5. Guild System | 0/4 | Not started | - |
| 6. Rebirth System | 0/4 | Not started | - |
| 7. P2P Market System | 0/5 | Not started | - |
| 8. Compound System | 0/5 | Not started | - |
| 9. Pet System Completion | 0/5 | Not started | - |
| 10. Combat & Progression Integration | 0/5 | Not started | - |

---
*Roadmap created: 2026-02-15*
*Last updated: 2026-02-19*
