# Research Summary: MMORPG Multiplayer Systems

**Project:** The Wonderland Legacy (TWL)
**Domain:** Turn-based MMORPG - P2P Market, Party, Guild, Rebirth Systems
**Date:** 2026-02-15
**Overall Confidence:** MEDIUM-HIGH

---

## Executive Summary

Research across 4 dimensions (Stack, Features, Architecture, Pitfalls) reveals that implementing secure P2P market, party, guild, and rebirth systems requires **database transaction safety as the foundation**. The PostgreSQL migration is not just a performance upgrade - it's a security requirement. Real-world MMORPG disasters (New World, MapleStory, Lost Ark) prove that race conditions and incomplete rollbacks destroy economies and player trust.

**Key Finding:** TWL already has strong patterns in `TradeManager` (two-phase commit) and `EconomyManager` (idempotency). The challenge is extending these patterns to P2P market, party loot/XP distribution, and guild bank operations while maintaining ACID guarantees.

**Recommended Approach:** Hybrid ORM (EF Core for writes, Dapper for reads) + CQRS (MediatR) + in-memory state caching for active parties/guilds.

---

## Stack Recommendations

### Core Technology Decisions

| Technology | Version | Purpose | Confidence |
|------------|---------|---------|------------|
| **PostgreSQL** | 16+ | Primary database with ACID transactions | HIGH |
| **Npgsql** | 10.0.1 | PostgreSQL driver | HIGH |
| **EF Core** | 10.0.0 | ORM for complex writes (market, guild bank) | HIGH |
| **Dapper** | 2.1.35+ | Micro-ORM for high-performance reads | HIGH |
| **MediatR** | 12.4.0+ | CQRS command/query separation | MEDIUM-HIGH |
| **FluentValidation** | 11.10.0+ | Input validation pipeline | MEDIUM-HIGH |
| **Argon2** | 1.3.1 | Password hashing (new accounts) | HIGH |

### Hybrid ORM Rationale

- **EF Core for Writes:** Market transactions, guild bank operations, party formation require change tracking, migrations, and ACID guarantees
- **Dapper for Reads:** Market browsing, guild member lists, leaderboards are 2x faster with Dapper
- **Migration Management:** EF Core Migrations provides code-first approach with version control

### Security Upgrades Required

1. **Anti-Replay Protection:** Add nonce + timestamp validation for critical operations (market trades, guild withdrawals)
2. **Password Hashing:** Migrate from BCrypt to Argon2 for new registrations (OWASP 2025 standard, GPU-resistant)
3. **Transaction Isolation:** Use `Serializable` isolation level for all multi-party operations to prevent race conditions

---

## Feature Landscape

### Table Stakes (Must Have)

**P2P Market:**
- Item listing with search/filters
- Buy/sell interface with atomic gold transfer
- Price history and market data
- Transaction taxes (economy sink)
- Listing expiration (24-72 hours)
- Direct player-to-player trade

**Party System:**
- Invite/join/leave (max 4 players)
- Shared XP distribution with proximity checks
- Shared loot distribution (round-robin, need/greed)
- Party member UI with HP/MP/status sync
- Party chat channel

**Guild System:**
- Create/join/leave with unique names
- Guild chat channel
- Guild ranks and permissions (hierarchical)
- Guild shared storage with transaction logs
- Guild roster with online status

**Rebirth System:**
- Level reset with permanent stat bonuses (10-20 points per rebirth)
- Rebirth requirements (min level 100+, optional quest)
- Rebirth count tracking (visible prestige)
- Skill/stat retention rules

### Differentiators (Competitive Advantage)

- **Market Analytics:** Price graphs, volume trends, profit calculators (merchant playstyle)
- **Tactical Formation:** 3x3 grid positioning for turn-based combat depth
- **Guild Skills/Buffs:** Passive bonuses (XP%, drop rate%) from guild level
- **Pet Rebirth:** Pets rebirth with stat inheritance and evolution paths
- **Diminishing Returns:** Rebirth bonuses decrease (20/15/10/5) to prevent infinite scaling

### Anti-Features (Do NOT Build)

- **Personal Player Shops:** Creates "ghost town" problem where players AFK in shops instead of exploring
- **Unlimited Guild Size:** Mega-guilds dominate server, kills competition
- **Item Destruction on Enhancement Failure:** Creates rage-quits and negative player sentiment
- **Complex Contribution-Based Loot:** Causes drama; simple round-robin is better
- **Real-Money Trading Support:** Legal/ethical minefield; focus on in-game economy

---

## Architecture Patterns

### System Structure

```
PartyService ──┐
GuildService ──┼──> Existing Core Services ──> DbService (PostgreSQL)
MarketService ─┤    (Combat, Inventory,         - Atomic Transactions
RebirthService ┘     Economy, Trade)             - Idempotency Keys
                                                  - Audit Logging
```

### Component Responsibilities

| Service | State Management | Transaction Pattern |
|---------|------------------|---------------------|
| **PartyService** | In-memory `ConcurrentDictionary` with async DB snapshots | Eventual consistency for party state |
| **GuildService** | Database-backed with in-memory cache for active guilds | ACID transactions for bank withdrawals |
| **MarketService** | Database-backed auction house | Serializable isolation for purchases |
| **RebirthService** | Stateless validator using `ServerCharacter` state | Single atomic transaction for rebirth |

### Data Flow: Market Purchase

```
1. Client → CreatePurchaseCommand
2. MediatR → FluentValidation (listing exists, buyer has gold)
3. MarketService → BeginTransaction(Serializable)
4. Lock: Buyer row, Seller row, Listing row
5. Deduct: Buyer gold
6. Add: Buyer inventory (via existing InventoryService)
7. Remove: Seller inventory
8. Add: Seller gold (minus tax)
9. Delete: Market listing
10. Commit transaction OR Rollback on any failure
11. Broadcast: Update client UIs
```

### Recommended Build Order

1. **Phase 1: Rebirth System** - Simplest (stateless, no cross-service dependencies)
2. **Phase 2: Party System** - Foundation for multiplayer content
3. **Phase 3: Guild System** - Builds on party patterns (invites, membership)
4. **Phase 4: Market System** - Most complex, depends on mature transaction infrastructure

**Rationale:** Start simple to validate patterns, build social foundation before economy expansion, defer market until guild systems are mature (guild taxes, guild-wide purchases depend on guild infrastructure).

---

## Critical Pitfalls

### Top 5 Economy-Destroying Exploits

1. **Race Condition Duplication** (New World 2021, MapleStory 2011, 2025)
   - **Cause:** Non-atomic transactions allow multiple simultaneous withdrawals
   - **Prevention:** Use `IsolationLevel.Serializable` with row-level locks (`ForUpdate()`)
   - **Phase:** Market Foundation must implement from day one

2. **Incomplete Transaction Rollback** (Lost Ark 2024)
   - **Cause:** Multi-step operations fail to rollback ALL state changes
   - **Prevention:** Wrap all market/trade operations in database transactions with compensating transactions for recovery
   - **Phase:** Market Foundation transaction infrastructure

3. **Missing Idempotency Protection** (Diablo IV, RuneScape 2003)
   - **Cause:** Network retries cause duplicate operations
   - **Prevention:** Extend existing `EconomyManager` operation ID pattern to all multiplayer operations
   - **Phase:** Market Foundation - use operation IDs for all trades

4. **Guild Bank Permission Escalation** (Turtle WoW 2024, Albion Online, Black Desert)
   - **Cause:** New members granted immediate withdrawal access
   - **Prevention:** Time gates (1-2 week wait), granular permissions (view/deposit/withdraw separate), audit logs
   - **Phase:** Guild System foundation with permission hierarchy

5. **Party Kick Abuse & Ninja Looting** (Neverwinter, WoW)
   - **Cause:** Party leader kicks members before loot distribution or during boss fights
   - **Prevention:** Disable kick during combat, loot-locked status, need/greed roll system with timers
   - **Phase:** Party System loot distribution logic

### Security Checklist

- [ ] All market transactions use `IsolationLevel.Serializable`
- [ ] All multi-party operations have idempotency keys
- [ ] Guild bank withdrawals have audit logs
- [ ] Party kick disabled during combat/loot rolling
- [ ] Movement validation prevents proximity exploits for party XP
- [ ] Anti-replay protection (nonce + timestamp) for critical operations
- [ ] PostgreSQL migration complete before market/guild launch

---

## Roadmap Implications

### Phase Dependencies

```
PostgreSQL Migration (P0 Blocker)
    ↓
Rebirth System (Phase 1) ──> No dependencies, validates prestige loop
    ↓
Party System (Phase 2) ──> Enables group content testing
    ↓
Guild System (Phase 3) ──> Reuses party patterns, adds permissions
    ↓
Market System (Phase 4) ──> Depends on all prior transaction patterns
```

### Phase Estimates

| Phase | Features | Complexity | Est. Duration |
|-------|----------|------------|---------------|
| P0: PostgreSQL Migration | EF Core setup, migrations, connection pooling | HIGH | 1-2 weeks |
| Phase 1: Rebirth | Character rebirth, stat bonuses, pet rebirth | MEDIUM | 1 week |
| Phase 2: Party | Invite/join, XP/loot share, formation | MEDIUM | 2 weeks |
| Phase 3: Guild | Create/join, chat, permissions, storage | HIGH | 2-3 weeks |
| Phase 4: Market | Listings, search, purchases, compound | HIGH | 2-3 weeks |

**Total:** 8-11 weeks for complete multiplayer systems

### Open Questions for Phase Planning

1. **Market Analytics Scope:** How much analytics is "enough" without overwhelming casual players?
2. **Party Formation:** Is 3x3 grid the right size for 4-player tactical combat?
3. **Guild Size Cap:** 50 members for MVP - needs validation against expected server population
4. **Rebirth Stat Scaling:** 20/15/10/5 diminishing returns - needs balance testing
5. **Compound Enhancement:** Combinatorial (A+B=C) vs skill-based crafting?

---

## Key Takeaways

1. **PostgreSQL migration is non-negotiable** - Market/guild systems REQUIRE atomic transactions
2. **Leverage existing patterns** - `TradeManager` and `EconomyManager` already implement safety patterns; extend them
3. **Security first, not retrofit** - Real-world MMORPGs prove exploits compound over time and are nearly impossible to rollback
4. **Simple is better** - Round-robin loot beats complex contribution systems; avoid feature creep
5. **Build order matters** - Rebirth → Party → Guild → Market allows pattern validation before complexity

---

## Research Confidence

| Area | Confidence | Reason |
|------|------------|--------|
| Technology Stack | HIGH | Versions verified via NuGet/docs, patterns proven in production |
| Feature Requirements | MEDIUM | Strong consensus across WLO/RO/ToS for table stakes; differentiators need playtesting |
| Architecture Patterns | MEDIUM | Industry patterns well-documented; TWL-specific performance testing needed |
| Pitfall Prevention | HIGH | Real disasters documented with technical details and prevention strategies |

---

## Next Steps

1. **Validate with existing codebase:** Review `TradeManager.cs` and `EconomyManager.cs` to confirm transaction patterns
2. **PostgreSQL spike:** Test EF Core performance with 10k+ market listings
3. **Define requirements:** Scope table stakes vs differentiators for v1
4. **Plan phases:** Break into implementable chunks with clear acceptance criteria
5. **Security audit:** Identify gaps in movement validation and anti-replay protection

---

*Research complete. All findings are ready for requirements definition and roadmap planning.*
