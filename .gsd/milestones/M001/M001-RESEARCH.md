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

# Architecture Research: MMORPG Market, Party, Guild, and Rebirth Systems

**Domain:** MMORPG Multiplayer Social Systems
**Researched:** 2026-02-14
**Confidence:** MEDIUM

## Standard Architecture

### System Overview

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                           CLIENT LAYER (TWL.Client)                              │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐          │
│  │ PartyUI  │  │ GuildUI  │  │MarketUI  │  │ TradeUI  │  │RebirthUI │          │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘          │
│       │             │             │             │             │                 │
│       └─────────────┴─────────────┴─────────────┴─────────────┘                 │
│                              │ (Network Messages)                               │
├──────────────────────────────┼──────────────────────────────────────────────────┤
│                              ▼                                                   │
│                     NETWORK SERVER (ClientSession)                               │
│                              │                                                   │
├──────────────────────────────┼──────────────────────────────────────────────────┤
│                      SERVICE LAYER (TWL.Server)                                  │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐        │
│  │  PartyService│  │ GuildService │  │MarketService │  │RebirthService│        │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘        │
│         │                 │                 │                 │                 │
│         ├─────────────────┼─────────────────┼─────────────────┤                 │
│         │                 │                 │                 │                 │
│  ┌──────▼─────────────────▼─────────────────▼─────────────────▼──────┐          │
│  │              EXISTING CORE SERVICES                                │          │
│  │  ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌────────────┐  │          │
│  │  │  Combat    │  │ Inventory  │  │  Economy   │  │   Trade    │  │          │
│  │  │  Manager   │  │ (Server    │  │  Manager   │  │  Manager   │  │          │
│  │  │            │  │ Character) │  │            │  │            │  │          │
│  │  └────────────┘  └────────────┘  └────────────┘  └────────────┘  │          │
│  └────────────────────────────┬───────────────────────────────────────┘          │
│                               │                                                  │
├───────────────────────────────┼──────────────────────────────────────────────────┤
│                      PERSISTENCE LAYER                                           │
│  ┌────────────────────────────▼───────────────────────────────────┐              │
│  │                       DbService (PostgreSQL)                   │              │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐      │              │
│  │  │ players  │  │ parties  │  │  guilds  │  │ markets  │      │              │
│  │  └──────────┘  └──────────┘  └──────────┘  └──────────┘      │              │
│  └─────────────────────────────────────────────────────────────────┘              │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### Component Responsibilities

| Component | Responsibility | Typical Implementation |
|-----------|----------------|------------------------|
| **PartyService** | Manages party formation, member management, XP/loot distribution | In-memory party state with DB snapshots on changes |
| **GuildService** | Handles guild CRUD, membership, rank permissions, guild bank | Database-backed with in-memory cache for active guilds |
| **MarketService** | P2P marketplace listings, search, bid/purchase transactions | Database-backed auction house with eventual consistency |
| **RebirthService** | Manages character prestige/rebirth cycles, stat resets, bonuses | Stateless validator using ServerCharacter state |
| **TradeManager** (existing) | Direct player-to-player item trades with atomic transfers | Already implemented with rollback safety |
| **EconomyManager** (existing) | Premium currency, shop purchases, transaction ledger | Already implemented with idempotency & audit trail |
| **InventoryService** (ServerCharacter) | Item storage, stacking, bind policies | Character-embedded inventory with dirty tracking |

## Recommended Project Structure

```
TWL.Server/
├── Services/
│   ├── Party/
│   │   ├── PartyService.cs              # Party lifecycle management
│   │   ├── PartyInviteManager.cs        # Invitation state machine
│   │   ├── PartyLootDistributor.cs      # Loot distribution policies
│   │   └── PartyXpCalculator.cs         # XP sharing formulas
│   ├── Guild/
│   │   ├── GuildService.cs              # Guild CRUD operations
│   │   ├── GuildRankManager.cs          # Permission hierarchy
│   │   ├── GuildBankService.cs          # Shared storage with logs
│   │   └── GuildEventLogger.cs          # Audit trail for actions
│   ├── Market/
│   │   ├── MarketService.cs             # Listing/search/purchase
│   │   ├── AuctionEngine.cs             # Time-based bid resolution
│   │   ├── MarketSearchIndex.cs         # In-memory listing cache
│   │   └── MarketTransactionLog.cs      # Completed sales history
│   └── Rebirth/
│       ├── RebirthService.cs            # Rebirth eligibility & execution
│       ├── RebirthCalculator.cs         # Stat/bonus formulas
│       └── RebirthValidator.cs          # Precondition checks
├── Features/
│   ├── Party/
│   │   ├── CreatePartyCommand.cs
│   │   ├── InviteToPartyCommand.cs
│   │   ├── LeavePartyCommand.cs
│   │   └── PartyHandlers.cs
│   ├── Guild/
│   │   ├── CreateGuildCommand.cs
│   │   ├── InviteToGuildCommand.cs
│   │   ├── PromoteMemberCommand.cs
│   │   └── GuildHandlers.cs
│   ├── Market/
│   │   ├── CreateListingCommand.cs
│   │   ├── SearchMarketCommand.cs
│   │   ├── PurchaseListingCommand.cs
│   │   └── MarketHandlers.cs
│   └── Rebirth/
│       ├── InitiateRebirthCommand.cs
│       └── RebirthHandler.cs
├── Persistence/
│   └── Database/
│       ├── PartyRepository.cs           # Party persistence
│       ├── GuildRepository.cs           # Guild persistence
│       └── MarketRepository.cs          # Listing persistence
└── Simulation/
    └── Networking/
        ├── ServerCharacter.cs           # Extended with PartyId, GuildId, RebirthLevel
        └── ServerParty.cs (NEW)         # Runtime party state
        └── ServerGuild.cs (NEW)         # Runtime guild state
```

### Structure Rationale

- **Services/**: High-level business logic, stateful managers for runtime entities (parties, guilds)
- **Features/**: Command pattern handlers using IMediator (matches existing architecture)
- **Persistence/**: Database repositories for durability (matches existing DbService pattern)
- **Simulation/Networking/**: Runtime server-side state objects (matches ServerCharacter pattern)

## Architectural Patterns

### Pattern 1: In-Memory State with DB Snapshots

**What:** Keep active parties/guilds in memory for fast access; persist to DB on state changes or periodically

**When to use:** For frequently accessed, low-latency multiplayer state (party membership, guild roster)

**Trade-offs:**
- **Pros:** Fast reads, reduced DB load, supports real-time updates
- **Cons:** Requires crash recovery logic, potential data loss on server crash
- **Mitigation:** Write-ahead log or snapshot on every mutation

**Example:**
```csharp
public class PartyService
{
    private readonly ConcurrentDictionary<int, ServerParty> _activeParties = new();
    private readonly PartyRepository _repository;

    public async Task<ServerParty> CreatePartyAsync(int leaderId)
    {
        var party = new ServerParty(leaderId);
        _activeParties[party.Id] = party;

        // Persist immediately
        await _repository.SavePartyAsync(party);

        return party;
    }

    public ServerParty? GetParty(int partyId)
    {
        // Fast in-memory lookup
        return _activeParties.TryGetValue(partyId, out var party) ? party : null;
    }
}
```

### Pattern 2: State Synchronization Broadcasting

**What:** Server authoritative state broadcast to all party/guild members when state changes

**When to use:** When multiple clients need real-time visibility into shared state (party HP, guild announcements)

**Trade-offs:**
- **Pros:** Consistent view across clients, server controls truth
- **Cons:** Network overhead for broadcast messages
- **Mitigation:** Use delta updates (only send what changed)

**Example:**
```csharp
public class PartyService
{
    private readonly NetworkManager _network;

    public void BroadcastPartyUpdate(ServerParty party)
    {
        var dto = new PartyStateDTO
        {
            PartyId = party.Id,
            Members = party.Members.Select(m => new PartyMemberDTO
            {
                CharacterId = m.Id,
                Name = m.Name,
                Hp = m.CurrentHp,
                MaxHp = m.MaxHp
            }).ToList()
        };

        foreach (var member in party.Members)
        {
            _network.SendToClient(member.SessionId, dto);
        }
    }
}
```

### Pattern 3: Two-Phase Commit for P2P Trades

**What:** Atomic transfer of items/gold between players using lock-validate-execute-rollback pattern

**When to use:** For any transaction involving multiple player inventories (already implemented in TradeManager)

**Trade-offs:**
- **Pros:** Prevents duplication exploits, ensures ACID properties
- **Cons:** Complex rollback logic, potential for deadlocks
- **Mitigation:** Consistent lock ordering (always lock lower CharacterId first)

**Example (existing TradeManager):**
```csharp
// Already implemented in TWL.Server.Simulation.Managers.TradeManager
// 1. Lock source inventory
// 2. Validate items exist and are tradable
// 3. Remove from source
// 4. Add to target
// 5. If add fails, rollback removal
// 6. Log trade completion to SecurityLogger
```

### Pattern 4: Idempotent Commands with Operation IDs

**What:** Client sends operationId with command; server deduplicates retries

**When to use:** For network-unreliable operations (auction bids, guild applications)

**Trade-offs:**
- **Pros:** Safe client retries, no duplicate processing
- **Cons:** Requires operation ID tracking/cleanup
- **Mitigation:** TTL-based cleanup of old operation IDs (already in EconomyManager)

**Example (existing EconomyManager):**
```csharp
// Already implemented in EconomyManager.BuyShopItem
public EconomyOperationResultDTO BuyShopItem(
    ServerCharacter character,
    int shopItemId,
    int quantity,
    string? operationId)
{
    if (!string.IsNullOrEmpty(operationId))
    {
        if (_transactions.TryGetValue(operationId, out var tx))
        {
            if (tx.State == TransactionState.Completed)
            {
                // Idempotent success
                return new EconomyOperationResultDTO { Success = true };
            }
        }
    }
    // Process transaction...
}
```

### Pattern 5: Permission-Based Action Authorization

**What:** Guild ranks define permission bitmasks; actions check permissions before execution

**When to use:** For hierarchical organizations with role-based access (guilds)

**Trade-offs:**
- **Pros:** Flexible permission model, easy to extend
- **Cons:** Complexity in permission inheritance/conflicts
- **Mitigation:** Use enums/flags for type-safe permission checks

**Example:**
```csharp
[Flags]
public enum GuildPermission
{
    None = 0,
    InviteMembers = 1 << 0,
    KickMembers = 1 << 1,
    PromoteMembers = 1 << 2,
    AccessBank = 1 << 3,
    WithdrawBank = 1 << 4,
    ManageRanks = 1 << 5,
    DisbandGuild = 1 << 6
}

public class GuildRank
{
    public int RankId { get; set; }
    public string Name { get; set; }
    public GuildPermission Permissions { get; set; }
}

public class GuildService
{
    public bool CanPerformAction(ServerCharacter character, GuildPermission permission)
    {
        var guild = GetGuild(character.GuildId);
        var member = guild.GetMember(character.Id);
        var rank = guild.GetRank(member.RankId);

        return rank.Permissions.HasFlag(permission);
    }
}
```

## Data Flow

### Party XP Distribution Flow

```
[Monster Defeated in Combat]
    ↓
[CombatManager.OnEncounterComplete()]
    ↓
[PartyService.DistributeXP(partyId, totalXp)]
    ↓
[PartyXpCalculator.CalculateShares(party, totalXp)]
    ↓ (for each member)
[ServerCharacter.AddExp(share)]
    ↓
[Broadcast PartyUpdateDTO to all members]
```

**Key considerations:**
- Only members within proximity (e.g., same map or within radius) receive XP
- Level difference penalties/bonuses apply
- Inactive members (AFK timeout) excluded from distribution

### P2P Market Transaction Flow

```
[Seller: CreateListingCommand]
    ↓
[MarketService.CreateListing(itemId, price, duration)]
    ↓
[Validate item ownership & transferability via TradeManager.ValidateTransfer()]
    ↓
[Lock item in seller's inventory (mark as "listed")]
    ↓
[MarketRepository.SaveListing(listing)]
    ↓
[MarketSearchIndex.AddListing(listing)] (in-memory cache)
    ↓
[Buyer: SearchMarketCommand → PurchaseListingCommand]
    ↓
[MarketService.PurchaseListing(listingId, buyerId, operationId)]
    ↓
[Atomic Transaction:]
    1. Validate buyer has gold
    2. Remove gold from buyer
    3. Transfer item from seller to buyer (via TradeManager.TransferItem)
    4. Add gold to seller (minus market fee)
    5. Mark listing as sold
    ↓
[MarketRepository.CompleteListing(listingId)]
    ↓
[Notify seller & buyer via ClientSession]
```

**Transaction safety:**
- Use database transaction for listing creation/completion
- Leverage existing TradeManager rollback logic for item transfers
- Log all market transactions to EconomyManager ledger (extends existing audit trail)

### Guild Bank Deposit/Withdrawal Flow

```
[Member: DepositToGuildBankCommand]
    ↓
[GuildService.DepositItem(guildId, characterId, itemId, quantity)]
    ↓
[Validate membership & permissions (GuildPermission.AccessBank)]
    ↓
[TradeManager.TransferItem(character, guildBank, itemId, quantity)]
    ↓
[GuildEventLogger.LogDeposit(guildId, characterId, itemId, quantity)]
    ↓
[GuildRepository.SaveBankState(guildId)]
    ↓
[Broadcast GuildBankUpdateDTO to online members]
```

**Key considerations:**
- Guild bank is a special "virtual character" with inventory
- Withdrawal requires additional permission (GuildPermission.WithdrawBank)
- Daily withdrawal limits per rank configurable
- All transactions logged for audit (prevents theft disputes)

### Rebirth Execution Flow

```
[Player: InitiateRebirthCommand]
    ↓
[RebirthService.CanRebirth(characterId)]
    ↓
[RebirthValidator.CheckPreconditions(character)]
    - Level >= MaxLevel (e.g., 100)
    - Not in combat/party/instance
    - Required quest completed (optional)
    ↓
[RebirthCalculator.CalculateRebirthBonuses(character)]
    ↓
[RebirthService.ExecuteRebirth(character)]
    ↓
[Atomic State Change:]
    1. Increment character.RebirthLevel
    2. Reset character.Level = 1
    3. Reset character.Exp = 0
    4. Apply permanent stat bonuses (e.g., +1% all stats per rebirth)
    5. Unlock rebirth-exclusive content flags
    ↓
[PlayerService.SaveCharacter(character)] (persistence)
    ↓
[ClientSession.SendRebirthCompleteDTO()]
```

**Key considerations:**
- Rebirth is irreversible (confirm dialog on client)
- Equipped items remain (but may require level 1 to use - design choice)
- Skill mastery levels preserved or partially preserved (50% retention)
- Rebirth count visible in character sheet & leaderboards

## Scaling Considerations

| Scale | Architecture Adjustments |
|-------|--------------------------|
| **0-1k users** | Single server process, in-memory party/guild state, PostgreSQL on same host |
| **1k-10k users** | Separate DB server, read replicas for market search, Redis cache for guild roster |
| **10k-100k users** | Partition parties/guilds by shard, dedicated market service, CDN for static assets |
| **100k+ users** | Distributed guild service (multi-region), auction house eventual consistency, CQRS for market reads |

### Scaling Priorities

1. **First bottleneck:** Database write contention on party/guild updates
   - **Fix:** Batch updates, write-behind caching, use dirty flags (already implemented for ServerCharacter)

2. **Second bottleneck:** Market search query performance
   - **Fix:** Elasticsearch or in-memory index (MarketSearchIndex) with background DB sync

3. **Third bottleneck:** Guild bank item transfers
   - **Fix:** Pessimistic locking with timeout, queue-based withdrawal processing

## Component Boundaries

### PartyService ↔ CombatManager
- **Direction:** CombatManager → PartyService (one-way)
- **Interface:** `IPartyService.OnCombatComplete(partyId, xp, loot)`
- **Pattern:** Event-driven (CombatManager emits `CombatComplete` event, PartyService subscribes)

### MarketService ↔ TradeManager
- **Direction:** MarketService → TradeManager (one-way)
- **Interface:** `TradeManager.TransferItem(seller, buyer, itemId, quantity)`
- **Pattern:** Direct method invocation (TradeManager is stateless utility)

### MarketService ↔ EconomyManager
- **Direction:** MarketService → EconomyManager (one-way)
- **Interface:** `EconomyManager.LogLedger("MarketSale", ...)`
- **Pattern:** Append-only audit log (MarketService writes, EconomyManager manages ledger)

### GuildService ↔ InventoryService (ServerCharacter)
- **Direction:** GuildService ↔ ServerCharacter (bidirectional)
- **Interface:**
  - GuildService reads `character.GuildId`
  - ServerCharacter reads guild bank via `GuildService.GetGuildBank(guildId)`
- **Pattern:** Service locator or dependency injection

### PartyService ↔ InventoryService (ServerCharacter)
- **Direction:** PartyService → ServerCharacter (one-way)
- **Interface:** `character.AddExp(xp)`, `character.AddItem(loot)`
- **Pattern:** Direct property/method access (ServerCharacter is domain entity)

### RebirthService ↔ ServerCharacter
- **Direction:** RebirthService → ServerCharacter (one-way)
- **Interface:** `character.RebirthLevel`, `character.Level`, `character.Exp` (read/write)
- **Pattern:** Direct mutation (stateless service operates on character entity)

### All Services ↔ DbService
- **Direction:** All Services → DbService (one-way)
- **Interface:** `DbService.ExecuteAsync(sql, params)`, `Repository.SaveAsync(entity)`
- **Pattern:** Repository pattern (each service has dedicated repository)

### ClientSession ↔ All Services
- **Direction:** ClientSession → Services (via IMediator)
- **Interface:** `IMediator.Send(command)` → `ICommandHandler.Handle(command)`
- **Pattern:** Command pattern with mediator (matches existing architecture)

## Integration Points

### External Services

| Service | Integration Pattern | Notes |
|---------|---------------------|-------|
| **PostgreSQL** | Npgsql direct connection | Existing DbService; extend with party/guild/market tables |
| **Redis (future)** | StackExchange.Redis | Optional caching layer for guild rosters and market listings |
| **Serilog** | ILogger injection | Existing logging infrastructure; extend for party/guild events |
| **SecurityLogger** | Static logger | Existing audit trail; extend for market fraud detection |

### Internal Boundaries

| Boundary | Communication | Notes |
|----------|---------------|-------|
| **PartyService ↔ GuildService** | Independent (no direct calls) | Characters can be in both party and guild simultaneously |
| **MarketService ↔ PartyService** | Independent | Market operations allowed while in party |
| **RebirthService ↔ PartyService** | Validation check | Rebirth requires leaving party first |
| **GuildService ↔ PartyService** | Independent | Guild members can form parties freely |
| **All Services ↔ ClientSession** | Mediator pattern | ClientSession routes network messages to command handlers |

## State Management Patterns

### Party State Lifecycle

```
[Party Created]
    → Active (in-memory + DB)
    → Members Join/Leave (broadcast updates)
    → Party Disbanded (remove from memory, archive in DB)
```

**State storage:**
- In-memory: `ConcurrentDictionary<int, ServerParty>`
- Database: `parties` table with JSON member list
- Cleanup: Auto-disband when last member leaves or leader offline > 24h

### Guild State Lifecycle

```
[Guild Created]
    → Active (in-memory cache + DB)
    → Members Join/Promoted/Leave (broadcast updates)
    → Guild Disbanded (soft delete in DB, remove from cache)
```

**State storage:**
- In-memory: `ConcurrentDictionary<int, ServerGuild>` (active guilds only)
- Database: `guilds`, `guild_members`, `guild_ranks`, `guild_bank` tables
- Cache TTL: Evict inactive guilds (no online members) after 1 hour

### Market Listing State

```
[Listing Created]
    → Active (searchable in index + DB)
    → Purchased (mark sold, remove from index)
    → Expired (remove from index, refund item to seller)
```

**State storage:**
- In-memory: `MarketSearchIndex` (sorted by price, category)
- Database: `market_listings` table with indexes on `item_id`, `price`, `expires_at`
- Background job: Expire listings every 5 minutes

### Rebirth State

**No separate state storage** (stateless service)
- Rebirth level stored in `ServerCharacter.RebirthLevel`
- Rebirth history optionally logged to `rebirth_log` table for analytics

## Transaction Safety Patterns

### Pattern 1: Database Transactions for Multi-Table Writes

**Use case:** Guild creation (insert into `guilds`, `guild_members`, `guild_ranks`)

```csharp
public async Task<int> CreateGuildAsync(string name, int founderId)
{
    await using var conn = new NpgsqlConnection(_connString);
    await conn.OpenAsync();
    await using var tx = await conn.BeginTransactionAsync();

    try
    {
        // Insert guild
        var guildId = await InsertGuildAsync(conn, tx, name);

        // Insert founder as leader
        await InsertMemberAsync(conn, tx, guildId, founderId, rankId: 1);

        // Insert default ranks
        await InsertDefaultRanksAsync(conn, tx, guildId);

        await tx.CommitAsync();
        return guildId;
    }
    catch
    {
        await tx.RollbackAsync();
        throw;
    }
}
```

### Pattern 2: Optimistic Concurrency with Version Stamps

**Use case:** Market listing purchase (prevent double-purchase)

```csharp
public async Task<bool> PurchaseListingAsync(int listingId, int buyerId)
{
    var sql = @"
        UPDATE market_listings
        SET status = 'sold', buyer_id = @buyer, sold_at = NOW()
        WHERE listing_id = @id AND status = 'active'
        RETURNING listing_id";

    var result = await _db.ExecuteScalarAsync(sql, new { id = listingId, buyer = buyerId });

    // Returns null if listing already sold (status != 'active')
    return result != null;
}
```

### Pattern 3: In-Memory Locks for Hot Paths

**Use case:** Party invite acceptance (prevent race conditions)

```csharp
public class PartyService
{
    private readonly ConcurrentDictionary<int, SemaphoreSlim> _partyLocks = new();

    public async Task<bool> AcceptInviteAsync(int partyId, int characterId)
    {
        var partyLock = _partyLocks.GetOrAdd(partyId, _ => new SemaphoreSlim(1, 1));

        await partyLock.WaitAsync();
        try
        {
            var party = GetParty(partyId);
            if (party.Members.Count >= party.MaxSize)
                return false;

            party.AddMember(characterId);
            await _repository.SavePartyAsync(party);

            return true;
        }
        finally
        {
            partyLock.Release();
        }
    }
}
```

### Pattern 4: Idempotency Keys (Already Implemented)

**Use case:** Market purchase retries (reuse existing EconomyManager pattern)

```csharp
public async Task<MarketPurchaseResultDTO> PurchaseListingAsync(
    int listingId,
    int buyerId,
    string operationId)
{
    // Check if already processed (idempotency)
    if (_processedOperations.TryGetValue(operationId, out var result))
        return result;

    // Process purchase...
    var purchaseResult = await ExecutePurchaseAsync(listingId, buyerId);

    // Cache result
    _processedOperations[operationId] = purchaseResult;

    return purchaseResult;
}
```

## Build Order Implications

### Phase 1: Foundation (No Dependencies)
1. **RebirthService** (simplest - stateless, only touches ServerCharacter)
   - Extend `ServerCharacter` with `RebirthLevel` property (already exists)
   - Implement `RebirthValidator` and `RebirthCalculator`
   - Add `InitiateRebirthCommand` handler

**Rationale:** No cross-service dependencies, purely computational

### Phase 2: Core Social Systems
2. **PartyService** (depends on: CombatManager, InventoryService)
   - Create `ServerParty` runtime entity
   - Implement party invite/join/leave commands
   - Integrate with CombatManager for XP distribution

**Rationale:** Parties are prerequisite for guild activities (guild raids, guild parties)

### Phase 3: Organizations
3. **GuildService** (depends on: PartyService concepts, InventoryService, TradeManager)
   - Create `ServerGuild` runtime entity with ranks and permissions
   - Implement guild bank using TradeManager transfer logic
   - Add guild event logging

**Rationale:** Guilds are more complex than parties (permissions, bank, persistence)

### Phase 4: Economy Extensions
4. **MarketService** (depends on: TradeManager, EconomyManager, InventoryService)
   - Create market listings table and search index
   - Implement auction/buyout mechanics
   - Integrate with existing EconomyManager ledger

**Rationale:** Market needs fully functional guild/party systems for social trust & bulk operations

### Dependency Graph

```
RebirthService (standalone)

PartyService
  ↓ (uses)
CombatManager, InventoryService

GuildService
  ↓ (uses)
TradeManager, InventoryService, PartyService (optional - for guild parties)

MarketService
  ↓ (uses)
TradeManager, EconomyManager, InventoryService, GuildService (optional - for guild market taxes)
```

## Anti-Patterns to Avoid

### Anti-Pattern 1: Storing Party State in Database Only

**What people do:** Every party action (invite, join, leave) writes to database synchronously

**Why it's wrong:**
- Excessive DB load (parties change frequently)
- High latency for real-time actions
- Doesn't scale with concurrent parties

**Do this instead:** In-memory party state with async DB snapshots (write-behind caching)

### Anti-Pattern 2: Global Guild Roster Broadcasts

**What people do:** On any guild event, send full guild roster to all 500+ members

**Why it's wrong:**
- Network bandwidth waste (O(n²) messages)
- Client overwhelmed with data
- Doesn't scale past 100 members

**Do this instead:**
- Send delta updates (only what changed)
- Only broadcast to online members
- Use pagination for guild roster UI

### Anti-Pattern 3: Market Search on Every Keystroke

**What people do:** Client sends search query on every character typed in search box

**Why it's wrong:**
- Server overload with rapid queries
- Database index thrashing
- Poor UX (results flicker)

**Do this instead:**
- Client-side debouncing (wait 300ms after typing stops)
- Server-side query deduplication
- In-memory search index for hot queries

### Anti-Pattern 4: Synchronous Item Transfer in Market Purchase

**What people do:** Market purchase blocks until item transfer completes and DB writes finish

**Why it's wrong:**
- Client timeout on slow transactions
- Lock contention on seller's inventory
- Deadlock risk with concurrent purchases

**Do this instead:**
- Asynchronous completion with status polling
- Optimistic locking (fail fast if item already sold)
- Queue-based processing for non-critical path

### Anti-Pattern 5: Guild Bank Without Audit Logs

**What people do:** Direct inventory transfers without logging who took what

**Why it's wrong:**
- Guild drama (members accuse each other of theft)
- No way to investigate disputes
- Exploits go undetected

**Do this instead:**
- Log every deposit/withdrawal to `guild_event_log` table
- Display recent transactions in guild bank UI
- Alerting for suspicious patterns (mass withdrawals)

## Sources

### MMORPG Architecture Patterns
- [MMO Architecture: Source of truth, Dataflows, I/O bottlenecks](https://prdeving.wordpress.com/2023/09/29/mmo-architecture-source-of-truth-dataflows-i-o-bottlenecks-and-how-to-solve-them/)
- [Server-Side MMO Architecture - IT Hare on Soft.ware](http://ithare.com/chapter-via-server-side-mmo-architecture-naive-and-classical-deployment-architectures/)
- [A distributed architecture for MMORPG](https://www.researchgate.net/publication/221391409_A_distributed_architecture_for_MMORPG)

### Database Schema and State Management
- [MMORPG Database Structure - GameDev.net](https://www.gamedev.net/forums/topic/690002-database-structure-for-mmos/)
- [MMO Database Consistency - GameDev.net](https://gamedev.net/forums/topic/550290-mmo-database-consistency/4541675/)
- [MMO Games and Database Design - Redgate](https://www.red-gate.com/blog/mmo-games-and-database-design)

### Multiplayer State Synchronization
- [How do Multiplayer Games sync their state? Part 1 - Medium](https://medium.com/@qingweilim/how-do-multiplayer-games-sync-their-state-part-1-ab72d6a54043)
- [How do Multiplayer Games sync their state? Part 2 - Medium](https://medium.com/@qingweilim/how-do-multiplayer-game-sync-their-state-part-2-d746fa303950)
- [Mastering Multiplayer Game Architecture - Getgud.io](https://www.getgud.io/blog/mastering-multiplayer-game-architecture-choosing-the-right-approach/)

### Trading and Transaction Systems
- [A transaction execution engine architecture for multiplayer online games](https://www.researchgate.net/publication/221391499_A_transaction_execution_engine_architecture_for_multiplayer_online_games)
- [Xaya's Blockchain-based Gaming Platform - MMORPG.com](https://www.mmorpg.com/general-articles/our-first-look-at-xayas-blockchain-based-gaming-platform-sponsored-2000106873)

### Guild and Permission Systems
- [Creating Useful Guild Ranks FAQ - WoW Guild Relations Wiki](https://guildrelationswow.fandom.com/wiki/Creating_Useful_Guild_Ranks:_a_FAQ)
- [Guild Chat: Wrestling with guild permissions - Massively Overpowered](https://massivelyop.com/2015/11/08/guild-chat-wrestling-with-guild-permissions-and-controlling-leaders/)

### Auction House and Marketplace
- [How to Design Auction System - Coding Monkey](https://pyemma.github.io/How-to-design-auction-system/)
- [A Dependable Distributed Auction System - ResearchGate](https://www.researchgate.net/publication/3894414_A_Dependable_Distributed_Auction_System_Architecture_and_an)

---
*Architecture research for: The Wonderland Legacy MMORPG Market/Party/Guild/Rebirth Systems*
*Researched: 2026-02-14*

# Technology Stack Research

**Project:** The Wonderland Legacy (TWL)
**Domain:** Turn-based MMORPG (MonoGame/C#)
**Researched:** 2026-02-14
**Overall Confidence:** MEDIUM-HIGH

## Executive Summary

This research covers the technology stack for implementing P2P market systems, party/guild features, rebirth mechanics, and PostgreSQL persistence in TWL's existing MonoGame-based MMORPG. The project already uses LiteNetLib for networking, Npgsql for PostgreSQL, and Microsoft.Extensions for DI/configuration. This document focuses on the additional libraries and patterns needed for the new features while maintaining security and performance.

**Key Recommendation:** Adopt a hybrid persistence approach using Entity Framework Core for complex operations (market transactions, guild data) and Dapper for high-performance reads. Implement CQRS pattern with MediatR for market/party/guild commands to ensure transaction safety. Upgrade password hashing from BCrypt to Argon2 for new accounts.

---

## Recommended Stack

### Core Framework (Existing)

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| **.NET** | 10.0 | Runtime | Latest LTS version with performance improvements, already in use |
| **MonoGame** | 3.8.4.1 | Game client framework | Already integrated, cross-platform game development |
| **LiteNetLib** | 1.3.5 | UDP networking | Lightweight reliable UDP with automatic fragmentation, MTU detection, NAT punching. Fast, battle-tested for MMORPGs (v1.2.0 released 2024-01) |

**Confidence:** HIGH (existing stack, proven in production)

---

### Database & Persistence

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| **PostgreSQL** | 16+ | Primary database | ACID guarantees critical for market transactions, mature JSON support for flexible schemas |
| **Npgsql** | 10.0.1 | PostgreSQL driver | Already in use. V10 adds JSON complex types, virtual generated columns (PG18), improved performance |
| **Npgsql.EntityFrameworkCore.PostgreSQL** | 10.0.0 | ORM for complex operations | EF Core 10 with sequential GUIDs (v7), simplified configuration, strong typing. Use for writes and complex queries |
| **Dapper** | 2.1.35+ | Micro-ORM for reads | 2x faster than EF Core for bulk operations. Use for leaderboards, guild member lists, market browsing |

**Confidence:** HIGH (verified via NuGet, official docs)

**Hybrid Approach Rationale:**
- Market transactions: EF Core (ACID guarantees, change tracking)
- Party/Guild queries: Dapper (performance, simple reads)
- Migration management: EF Core Migrations (code-first approach)

---

### Market System Libraries

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| **MediatR** | 12.4.0+ | CQRS command/query separation | Market listings, trades, compound crafting. Ensures validation pipeline, transaction boundaries |
| **FluentValidation** | 11.10.0+ | Input validation | Validate market listings (price > 0, quantity valid, item exists) before hitting database |
| **Polly** | 8.4.0+ | Retry/circuit breaker | Handle transient PostgreSQL failures during market transactions |

**Confidence:** MEDIUM-HIGH (versions verified via NuGet, patterns verified via docs)

**Why MediatR for Markets:**
- Atomic command handling (CreateListing, CancelListing, CompleteTrade)
- Validation pipeline prevents invalid data
- Easy to add logging, security checks as behaviors
- Clear separation of read (browse market) vs write (create listing) operations

---

### Security & Validation

| Library | Version | Purpose | Why Recommended |
|---------|---------|---------|-----------------|
| **BCrypt.Net-Next** | 4.0.3 | Password hashing (existing) | Already in use for legacy accounts. Secure with cost factor 13-14 |
| **Konscious.Security.Cryptography.Argon2** | 1.3.1 | Password hashing (new standard) | Winner of Password Hashing Competition 2015. GPU/ASIC resistant, memory-hard. OWASP recommends 19 MiB/2 iterations minimum |
| **System.Security.Cryptography** | Built-in | HMAC, nonce generation | Anti-replay protection: timestamp + nonce validation for critical actions |

**Confidence:** HIGH (Argon2 OWASP guidance 2025, BCrypt verified via NuGet)

**Migration Strategy:**
- Keep BCrypt for existing passwords
- Use Argon2 for new registrations and password changes
- Store hash algorithm identifier in database (future-proof)

**Anti-Replay Implementation:**
```csharp
// For market trades, movement validation
public class ReplayProtection
{
    private readonly TimeSpan _validityWindow = TimeSpan.FromSeconds(30);
    private readonly ConcurrentDictionary<string, DateTime> _usedNonces;

    public bool ValidateRequest(string nonce, DateTime timestamp)
    {
        if (Math.Abs((DateTime.UtcNow - timestamp).TotalSeconds) > _validityWindow.TotalSeconds)
            return false; // Too old or from future

        return _usedNonces.TryAdd(nonce, timestamp);
    }
}
```

---

### State Management & Synchronization

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| **Microsoft.Extensions.Caching.Memory** | 10.0.0 | In-memory caching | Cache guild rosters, party state, active market listings (TTL: 5-60s) |
| **Microsoft.Extensions.Caching.StackExchangeRedis** | 10.0.0 (future) | Distributed cache | When scaling to multiple game servers. NOT needed for Proto-Alpha |

**Confidence:** HIGH (Microsoft official libraries)

**Party/Guild State Pattern:**
- Store persistent data in PostgreSQL (guild members, permissions, storage)
- Cache active state in-memory (current party, online guild members)
- Use LiteNetLib for real-time updates (party chat, guild events)
- Synchronize via server-authoritative commands

---

### Development & Debugging

| Tool | Version | Purpose | Notes |
|------|---------|---------|-------|
| **Serilog** | 10.0.0 | Structured logging | Already in use. Essential for debugging market exploits |
| **BenchmarkDotNet** | 0.14.0+ | Performance testing | Benchmark market queries, party state updates |
| **EF Core Power Tools** | Latest | Reverse engineering | Generate entities from existing PostgreSQL schema if needed |

**Confidence:** MEDIUM (BenchmarkDotNet version approximated, Serilog verified)

---

## Supporting Libraries Detail

### PostgreSQL Idempotency & Duplicate Prevention

**Pattern:** Idempotency keys for market operations

```sql
-- Market listing table with idempotency
CREATE TABLE market_listings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    idempotency_key VARCHAR(64) UNIQUE NOT NULL,
    player_id UUID NOT NULL,
    item_id UUID NOT NULL,
    quantity INT NOT NULL CHECK (quantity > 0),
    price BIGINT NOT NULL CHECK (price > 0),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    expires_at TIMESTAMPTZ NOT NULL,
    status VARCHAR(20) DEFAULT 'active' -- active, sold, cancelled
);

CREATE INDEX idx_market_active ON market_listings(status, created_at)
WHERE status = 'active';
```

**Why:** PostgreSQL `UNIQUE` constraint + `INSERT ... ON CONFLICT DO NOTHING` prevents duplicate listings even if client retries due to network issues.

**Confidence:** HIGH (PostgreSQL official docs, idempotency patterns verified)

---

### Transaction Isolation for Market Trades

**Critical:** Use `Serializable` isolation for trade completion to prevent race conditions

```csharp
using var transaction = await dbContext.Database.BeginTransactionAsync(
    IsolationLevel.Serializable);
try
{
    // 1. Verify listing still active
    var listing = await dbContext.MarketListings
        .FirstOrDefaultAsync(l => l.Id == listingId && l.Status == "active");

    if (listing == null)
        throw new InvalidOperationException("Listing no longer available");

    // 2. Deduct buyer currency
    buyer.Currency -= listing.Price;

    // 3. Transfer item to buyer
    buyer.Inventory.Add(new InventoryItem { ItemId = listing.ItemId, Quantity = listing.Quantity });

    // 4. Credit seller (even if offline)
    seller.Currency += listing.Price;

    // 5. Mark listing sold
    listing.Status = "sold";
    listing.CompletedAt = DateTime.UtcNow;

    await dbContext.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

**Why:** Serializable isolation prevents two buyers from purchasing the same item simultaneously. Critical for market integrity.

**Confidence:** HIGH (PostgreSQL ACID guarantees, EF Core transaction docs)

---

## Installation

### Core Dependencies (Already Installed)

```bash
# Already in TWL.Server.csproj
dotnet add package Npgsql --version 10.0.1
dotnet add package LiteNetLib --version 1.3.5
dotnet add package BCrypt.Net-Next --version 4.0.3
dotnet add package Microsoft.Extensions.Configuration --version 10.0.2
dotnet add package Serilog.AspNetCore --version 10.0.0
```

### New Dependencies for Market/Party/Guild

```bash
# PostgreSQL ORM (hybrid approach)
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 10.0.0
dotnet add package Dapper --version 2.1.35

# CQRS & Validation
dotnet add package MediatR --version 12.4.0
dotnet add package FluentValidation --version 11.10.0
dotnet add package FluentValidation.DependencyInjectionExtensions --version 11.10.0

# Resilience
dotnet add package Polly --version 8.4.0

# Security
dotnet add package Konscious.Security.Cryptography.Argon2 --version 1.3.1

# Caching
dotnet add package Microsoft.Extensions.Caching.Memory --version 10.0.0
```

### Development Tools

```bash
# Performance testing
dotnet add package BenchmarkDotNet --version 0.14.0 --project TWL.Tests

# EF Core tooling
dotnet tool install --global dotnet-ef --version 10.0.0
```

---

## Alternatives Considered

| Category | Recommended | Alternative | Why Not Alternative |
|----------|-------------|-------------|---------------------|
| **ORM** | EF Core + Dapper hybrid | EF Core only | EF Core 1.5x slower for reads. Guild member lists (1000+ players) need Dapper speed |
| **ORM** | EF Core + Dapper hybrid | Dapper only | No migration management. Market transactions need EF's change tracking |
| **Validation** | FluentValidation | Data Annotations | Market logic too complex for attributes. Need conditional validation |
| **CQRS** | MediatR | Custom implementation | MediatR provides pipeline behaviors (logging, validation) out of box |
| **Password Hashing** | Argon2 (new) + BCrypt (legacy) | BCrypt only | BCrypt vulnerable to GPU attacks. Argon2 is 2025 standard |
| **Caching** | In-memory (now), Redis (later) | Redis from start | Proto-Alpha single server. Redis adds complexity without benefit |
| **Networking** | LiteNetLib | ASP.NET Core SignalR | SignalR requires WebSocket/HTTP. LiteNetLib UDP is lower latency, already integrated |

---

## What NOT to Use

| Avoid | Why | Use Instead |
|-------|-----|-------------|
| **Entity Framework 6** | Legacy framework, .NET Framework only | EF Core 10 (.NET 10 compatible) |
| **MD5/SHA1 for passwords** | Cryptographically broken | Argon2 (new) or BCrypt (legacy) |
| **Global mutable state for party/guild** | Race conditions, not thread-safe | Immutable state + CQRS commands |
| **Repository pattern over EF Core** | EF Core DbContext already is repository/UoW | Use DbContext directly or thin wrappers |
| **SQL string concatenation** | SQL injection risk | Parameterized queries (EF Core/Dapper both enforce) |
| **Storing items as JSON blobs** | Can't query efficiently, no referential integrity | Relational tables with EF Core navigation properties |
| **Optimistic concurrency for market trades** | Race conditions (two buyers purchase same item) | Pessimistic locking (Serializable transactions) |

**Repository Pattern Note:** While the repository pattern is common, EF Core's `DbContext` already implements Unit of Work and `DbSet<T>` is already a repository. Adding another layer often creates unnecessary abstraction. **Exception:** You may want thin repositories if you need to swap ORMs or mock for testing.

---

## Architecture Patterns

### CQRS with MediatR for Market Operations

**Why:** Separate read (browse listings) and write (create listing) concerns. Writes need validation, transactions, logging. Reads need performance.

```
Command (Write)          Query (Read)
    ↓                       ↓
MediatR Pipeline        MediatR Pipeline
    ↓                       ↓
Validation              Cache Check
    ↓                       ↓
EF Core Transaction     Dapper Direct Query
    ↓                       ↓
PostgreSQL              PostgreSQL (read replica, future)
```

### Vertical Slice Architecture (Optional)

**Consider for:** Party, Guild, Market as separate "slices" with their own commands/queries/handlers.

**Benefit:** Each feature is self-contained. Easy to add new features without affecting others.

**Trade-off:** More files, less code reuse. Good for large teams, may be overkill for Proto-Alpha.

**Confidence:** MEDIUM (pattern is well-established but may be over-engineering for current stage)

---

## PostgreSQL Configuration Best Practices

### Connection Pooling

**Use NpgsqlDataSource (Npgsql 7.0+):**

```csharp
// Startup.cs / Program.cs
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.ConnectionStringBuilder.MaxPoolSize = 100;
dataSourceBuilder.ConnectionStringBuilder.MinPoolSize = 10;
dataSourceBuilder.ConnectionStringBuilder.ConnectionIdleLifetime = 300; // 5 min
dataSourceBuilder.ConnectionStringBuilder.ConnectionLifetime = 3600; // 1 hour

var dataSource = dataSourceBuilder.Build();
services.AddSingleton(dataSource);

// EF Core
services.AddDbContext<GameDbContext>(options =>
    options.UseNpgsql(dataSource));
```

**Why:** `NpgsqlDataSource` is the modern approach (v7.0+). Connection pooling is automatic. The data source manages connections efficiently. Open/close connections freely—pooling makes it fast.

**Confidence:** HIGH (Npgsql official docs)

### Migration Strategy (JSON to PostgreSQL)

**Approach:** Code-First with EF Core Migrations

```bash
# 1. Design entities (Player, Guild, MarketListing, etc.)
# 2. Create initial migration
dotnet ef migrations add InitialCreate --project TWL.Server

# 3. Review generated SQL
dotnet ef migrations script --project TWL.Server

# 4. Apply to database
dotnet ef database update --project TWL.Server

# 5. Migrate data from JSON (one-time script)
dotnet run --project TWL.DataMigration
```

**Why Code-First:**
- Track schema changes in version control
- Easy to apply migrations to dev/staging/prod
- Team can collaborate on schema without manual SQL scripts
- Database-first requires manual reverse engineering

**Confidence:** HIGH (EF Core migrations are standard practice)

---

## Security Considerations

### P2P Market Exploits (Prevention)

| Exploit | Prevention | Implementation |
|---------|-----------|----------------|
| **Item duplication** | Serializable transactions | `IsolationLevel.Serializable` for trades |
| **Negative pricing** | Database constraints + validation | `CHECK (price > 0)` + FluentValidation |
| **Expired listing trade** | Timestamp validation | Filter `WHERE expires_at > NOW()` |
| **Replay attack** | Nonce + timestamp | 30-second validity window, reject used nonces |
| **SQL injection** | Parameterized queries | EF Core/Dapper enforce by default |
| **Race condition (two buyers)** | Pessimistic locking | Lock listing row during transaction |
| **Fake item_id** | Foreign key constraints | `FOREIGN KEY (item_id) REFERENCES items(id)` |

### Movement/Action Validation (Existing + Enhanced)

**Current:** Server-authoritative (good foundation)

**Add for market/trading:**
- Rate limiting: Max 10 market actions/minute/player
- Action cost validation: Player has currency before creating listing
- Permission checks: Player owns item before listing
- Geo-fencing: Trades only in market zones (optional)

### Guild/Party Security

| Risk | Mitigation |
|------|-----------|
| **Permission escalation** | Role-based checks: `if (!player.HasGuildPermission(Permission.Invite))` |
| **Invite spam** | Rate limit: 5 invites/minute |
| **Storage theft** | Audit log: Track who deposits/withdraws from guild storage |
| **Impersonation** | Server validates player ID from session, not client packet |

---

## Performance Optimization

### Database Indexing Strategy

```sql
-- Market browsing (most common query)
CREATE INDEX idx_market_browse ON market_listings(status, created_at DESC)
WHERE status = 'active';

-- Player's active listings
CREATE INDEX idx_market_player ON market_listings(player_id, status);

-- Item price history (analytics)
CREATE INDEX idx_market_item_price ON market_listings(item_id, created_at)
WHERE status = 'sold';

-- Guild member lookup
CREATE INDEX idx_guild_members ON guild_members(guild_id, player_id);

-- Party lookup
CREATE INDEX idx_party_members ON party_members(party_id);
```

### Caching Strategy

| Data | Cache | TTL | Invalidate On |
|------|-------|-----|---------------|
| Active market listings | `IMemoryCache` | 10s | New listing created |
| Guild roster | `IMemoryCache` | 60s | Member join/leave |
| Party state | In-memory object | N/A | Member join/leave/disconnect |
| Player inventory | PostgreSQL only | N/A | Never cache (too mutable) |
| Item definitions | `IMemoryCache` | Infinite | Server restart only (static data) |

**Why not cache inventory?** Inventory changes frequently (combat loot, crafting). Caching creates stale data risk. PostgreSQL is fast enough for single-row lookups.

---

## Version Compatibility Matrix

| Package | Compatible With | Notes |
|---------|------------------|-------|
| **Npgsql.EntityFrameworkCore.PostgreSQL 10.0** | PostgreSQL 12-18 | Optimal: PostgreSQL 16+ |
| | .NET 8.0, 9.0, 10.0 | Requires .NET 8+ |
| | Entity Framework Core 10.0 | Must match EF Core version |
| **Dapper 2.1.35** | Any .NET Standard 2.0+ | Framework-agnostic |
| | Npgsql 8.0+ | Use same Npgsql connection |
| **MediatR 12.4.0** | .NET 8.0+ | Latest supports source generators |
| **FluentValidation 11.10.0** | .NET 8.0+ | Use DI extensions package |
| **LiteNetLib 1.3.5** | .NET Standard 2.0+ | Cross-platform (Windows/Linux/Mac) |
| **Argon2 1.3.1** | .NET Standard 2.0+ | No dependencies |

**Critical:** Npgsql.EntityFrameworkCore.PostgreSQL version MUST match Entity Framework Core version. Don't mix EF Core 9 with Npgsql.EF 10.

---

## Roadmap Implications

### Phase Ordering Recommendation

**Phase 1: PostgreSQL Foundation (Weeks 1-2)**
- Set up PostgreSQL schema (code-first migrations)
- Migrate player/character data from JSON
- Implement connection pooling
- Test transaction performance

**Phase 2: Market System (Weeks 3-5)**
- MediatR + FluentValidation pipeline
- Market listing CRUD (create, browse, cancel)
- Trade transactions with Serializable isolation
- Anti-duplication safeguards
- Compound crafting (if market dependencies exist)

**Phase 3: Party System (Weeks 6-7)**
- Party creation, invite, join, leave
- Shared XP distribution
- Shared loot distribution
- In-memory party state + PostgreSQL persistence

**Phase 4: Guild System (Weeks 8-10)**
- Guild creation, roles, permissions
- Guild chat (LiteNetLib broadcast)
- Guild storage (shared inventory)
- Member management (invite, kick, promote)

**Phase 5: Rebirth Mechanics (Week 11)**
- Rebirth progression schema
- Character stat reset + rewards
- Rebirth-gated content

**Phase 6: Security Hardening (Week 12)**
- Argon2 migration for passwords
- Anti-replay nonce validation
- Rate limiting for market/guild actions
- Penetration testing

**Dependency Rationale:**
- Market before Party/Guild: Market is most complex (transactions, exploits). Learn lessons early.
- Party before Guild: Party is simpler (fewer features). Test state synchronization before scaling to guilds.
- Rebirth last: Depends on stable character data in PostgreSQL.
- Security throughout: Each phase adds anti-cheat measures, but Phase 6 audits everything.

---

## Open Questions & Research Flags

### Questions Requiring Phase-Specific Research

1. **Market UI (Client):** MonoGame UI framework for market browser? (ImGui.NET? Custom?)
2. **PostgreSQL Hosting:** Self-hosted vs managed (AWS RDS, Azure Database)? Cost/performance trade-offs?
3. **Redis for multi-server:** When does player count justify distributed cache? (1000? 5000?)
4. **Compound crafting logic:** Is this combinatorial (A + B = C) or skill-based? Schema design varies.
5. **Pet riding/AI:** Does this need pathfinding? NavMesh integration with MonoGame?

### Low Confidence Areas (Need Validation)

- **BenchmarkDotNet version:** Approximated as 0.14.0 (verify on NuGet before installing)
- **LiteNetLib anti-cheat:** No specific docs found. May need custom packet validation layer.
- **MonoGame + EF Core:** No known issues, but limited examples. Test early.

### Recommended Phase 1 Spikes

- **Spike 1:** EF Core performance with 10k+ market listings (is Dapper really needed?)
- **Spike 2:** LiteNetLib packet size for guild chat (50-player guild, is fragmentation an issue?)
- **Spike 3:** PostgreSQL connection pool under load (100 concurrent players)

---

## Sources

### High Confidence (Official Docs, Context7)

- [Npgsql 10.0 Release Notes](https://www.npgsql.org/efcore/release-notes/10.0.html) — EF Core 10 features
- [Npgsql Entity Framework Core Provider](https://www.npgsql.org/efcore/) — Official integration docs
- [Npgsql Performance Guide](https://www.npgsql.org/doc/performance.html) — Connection pooling
- [NuGet: Npgsql.EntityFrameworkCore.PostgreSQL 10.0](https://www.nuget.org/packages/npgsql.entityframeworkcore.postgresql) — Version verification
- [NuGet: LiteNetLib 1.3.5](https://packages.nuget.org/packages/LiteNetLib/1.2.0) — Library details
- [GitHub: LiteNetLib](https://github.com/RevenantX/LiteNetLib) — Official repository
- [Microsoft: EF Core Migrations](https://www.milanjovanovic.tech/blog/efcore-migrations-a-detailed-guide) — Migration patterns
- [PostgreSQL: ACID Transactions](https://www.postgresql.org/docs/9.1/tutorial-transactions.html) — Official docs
- [OWASP: Password Storage Cheat Sheet](https://guptadeepak.com/the-complete-guide-to-password-hashing-argon2-vs-bcrypt-vs-scrypt-vs-pbkdf2-2026/) — Argon2 recommendations (verified 2025)

### Medium Confidence (Verified WebSearch, Multiple Sources)

- [Dapper vs EF Core Performance](https://blog.devart.com/dapper-vs-entity-framework-core.html) — Devart benchmarks
- [EF Core 9 vs Dapper](https://trailheadtechnology.com/ef-core-9-vs-dapper-performance-face-off/) — 2025 benchmarks
- [MediatR with FluentValidation](https://www.milanjovanovic.tech/blog/cqrs-validation-with-mediatr-pipeline-and-fluentvalidation) — CQRS pattern
- [Vertical Slice Architecture](https://www.jimmybogard.com/vertical-slice-architecture/) — Jimmy Bogard (MediatR author)
- [PostgreSQL Idempotency Patterns](https://www.morling.dev/blog/on-idempotency-keys/) — Gunnar Morling
- [Anti-Replay Attacks](https://www.packetlabs.net/posts/a-guide-to-replay-attacks-and-how-to-defend-against-them) — PacketLabs guide
- [Argon2 vs BCrypt](https://stytch.com/blog/argon2-vs-bcrypt-vs-scrypt/) — Stytch security blog

### Low Confidence (WebSearch Only, Needs Validation)

- BenchmarkDotNet version (approximated, check NuGet)
- Game-specific market schema patterns (general guidance, no MMORPG-specific source)
- LiteNetLib anti-cheat (no official docs, community practices only)

---

**Research Complete:** 2026-02-14
**Next Step:** Create roadmap phases using this stack as foundation

# Feature Research: MMORPG Multiplayer Systems

**Domain:** Turn-based MMORPG (P2P Market, Party, Guild, Rebirth)
**Researched:** 2026-02-14
**Confidence:** MEDIUM

## Feature Landscape

### Table Stakes (Users Expect These)

Features users assume exist. Missing these = product feels incomplete.

#### P2P Market/Trading System

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Item listing with search | Players need to find items quickly without spamming chat | MEDIUM | Requires database indexing, search filters (name, type, price range, rarity) |
| Buy/sell interface | Core economy mechanic; without it, trading is chat spam | MEDIUM | Centralized ledger, atomic transactions, gold transfer automation |
| Price history/market data | Players expect to see pricing trends to make informed decisions | LOW | Track last N transactions per item, show min/avg/max prices |
| Transaction taxes/fees | Economy sink to prevent inflation; expected in all modern MMOs | LOW | Configurable % fee, auto-deduction from sale price |
| Listing expiration | Prevents stale listings; players expect 24-72 hour default | LOW | Scheduled cleanup job, return unsold items to inventory |
| Direct player-to-player trade | Face-to-face trading for trust/social interaction | LOW | Trade window, both parties confirm, atomic swap |

#### Party System

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Invite/join/leave party | Basic social grouping; without it, no multiplayer cooperation | LOW | Max party size enforcement (4 for TWL), invite accept/decline flow |
| Shared XP distribution | Players expect bonus for grouping; core party incentive | MEDIUM | Proximity checks, level difference limits (±10 levels common), even share calculation |
| Shared loot distribution | Prevents loot drama; players assume fair distribution exists | MEDIUM | Round-robin, free-for-all, master looter, need/greed roll systems |
| Party member list/UI | Players need to see HP/status of party members at a glance | MEDIUM | Real-time HP/MP sync, status effect icons, distance indicators |
| Party chat channel | Private communication channel for coordination | LOW | Scoped message broadcast to party members only |

#### Guild System

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Create/join/leave guild | Basic guild lifecycle; without it, no guild system | LOW | Guild name uniqueness, creation fee (gold sink), member roster |
| Guild chat channel | Dedicated communication for guild members | LOW | Scoped broadcast, persists when members offline |
| Guild ranks/permissions | Guild leaders expect to control who can do what | MEDIUM | Hierarchical ranks, granular permissions (invite, promote, kick, withdraw storage) |
| Guild shared storage | Core feature from WLO/RO; expected for resource sharing | MEDIUM | Permission-based deposit/withdraw, transaction logs, tab limits |
| Guild roster management | Leaders need to see online status, last login, contribution | LOW | Member list with metadata, kick/promote/demote actions |

#### Rebirth System

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Level reset with stat bonuses | Core prestige mechanic; players replay content for permanent power | MEDIUM | Reset to level 1, grant bonus attribute points (e.g., 10-20 per rebirth) |
| Rebirth requirements | Players expect gates (min level, quest, currency) to prevent spam | LOW | Level 100+ requirement common, optional quest/item requirement |
| Rebirth tracking/count | Players want to show prestige; "Rebirth 5" visible in profile | LOW | Store rebirth count, display in character info/name plates |
| Skill/stat retention | Players expect to keep some progress; losing everything feels punishing | MEDIUM | Keep skill trees, lose levels, retain equipment (or have rebirth-specific gear) |

### Differentiators (Competitive Advantage)

Features that set the product apart. Not required, but valuable.

#### P2P Market/Trading System

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Market analytics dashboard | Empowers merchant playstyle; unique value for economy-focused players | HIGH | Price graphs, volume trends, profit calculator, rare item alerts |
| Buy orders (not just sell listings) | Players post "WTB" with auto-fulfill when item listed | HIGH | Reverse auction, queue matching, notification system |
| Escrow for high-value trades | Reduces scams; builds trust in player economy | MEDIUM | Third-party holding, dispute resolution flags |
| Compound material market predictions | Help players decide when to sell/buy enhancement mats | MEDIUM | Track compound success rates, mat price correlation with enhancement demand |
| Cross-server market (if multi-server) | Increases liquidity; unique in turn-based MMOs | HIGH | Requires cross-server database sync, complex reconciliation |

#### Party System

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Tactical formation system | Turn-based positioning strategy; differentiates from action MMOs | HIGH | 3x3 grid positioning, front/mid/back row mechanics, range/melee restrictions |
| Role assignment UI | Tank/DPS/Healer indicators; streamlines party composition | LOW | Player-set role tags, visual icons in party list |
| Party-wide skill combos | Coordinated abilities create synergy; encourages teamwork | HIGH | Skill chaining detection, bonus damage/effects for combo sequences |
| Saved party compositions | Quick re-invite for regular groups; QoL for guild dungeon runs | LOW | Store party roster templates, one-click invite all |
| Party XP/loot sharing strategy | Leader sets "optimize for speed" vs "fair share"; adds depth | MEDIUM | Dynamic distribution based on contribution, damage dealt, healing done |

#### Guild System

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Guild skills/buffs | Provides tangible benefit; incentivizes guild loyalty | MEDIUM | Passive buffs (XP%, drop rate%), guild-wide skill unlocks via guild level |
| Guild quests/missions | Shared objectives build community; differentiates from solo play | MEDIUM | Daily/weekly objectives, contribution tracking, guild XP rewards |
| Guild vs Guild events | Competitive endgame; creates rivalry and social cohesion | HIGH | Territory control, siege systems, scheduled PvP events |
| Guild crafting stations | Shared utility buildings; unique to guild members | MEDIUM | Exclusive crafting recipes, reduced material costs for guild members |
| Guild reputation/ranking | Leaderboards drive competition; visible prestige | LOW | Server-wide ranking by guild level/achievements, public display |

#### Rebirth System

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Class change on rebirth | Players can try new playstyles; extends replayability | HIGH | Class switching, skill tree reset, multi-class progression systems |
| Rebirth-exclusive skills | Unlock unique abilities only accessible after rebirth | MEDIUM | Skill pool expansion based on rebirth count, prestige talents |
| Pet rebirth with stat inheritance | Pets grow with character; deep companion bonding | HIGH | Pet level reset, stat bonus transfer, skill retention, evolution paths |
| Rebirth quest chains with lore | Narrative reward for prestige; connects gameplay to worldbuilding | MEDIUM | Story-driven rebirth ritual, ties to Los Ancestrales/Resonancia lore |
| Diminishing returns on later rebirths | Prevents infinite scaling; balance mechanism | LOW | First rebirth: 20 stats, second: 15, third: 10, etc. |

### Anti-Features (Commonly Requested, Often Problematic)

Features that seem good but create problems.

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|-----------------|-------------|
| Personal player shops (vs centralized market) | "Feels more social, like old MMOs" | Creates ghost towns of AFK vendors; players spend hours searching shops instead of playing; clutters maps | Centralized auction house with robust search + direct player trade option for trust-based transactions |
| Unlimited guild size | "More friends = better" | Mega-guilds dominate server, kills small guild viability, reduces social cohesion (anonymous members) | Cap at 50-100 members; encourage alliances (allied guilds share chat/storage) instead of bloat |
| No rebirth requirements | "Let players rebirth whenever they want" | Devalues prestige; players rebirth spam at low levels for stat stacking; breaks progression curve | Strict level 100+ gate + quest/currency requirement to maintain prestige value |
| Real-money trading support | "Players do it anyway, might as well monetize" | Destroys economy; pay-to-win perception kills new player retention; legal liability in many regions | Strict anti-RMT policies; report system; generous F2P progression to reduce incentive |
| Guild wars without opt-in | "Forced PvP creates drama and engagement" | Griefing; casual guilds quit; drives away PvE-focused players (majority in turn-based MMOs) | Opt-in guild war declarations; instanced GvG arenas; separate PvP-flagged guilds |
| Item enhancement breaking gear | "Risk creates value" | Feels punishing in turn-based MMO (slower progression); drives players to pay-to-skip; creates rage-quit moments | Enhancement failures reduce success chance next attempt (failstacks) but never destroy item; protection scrolls available via gameplay |
| Party XP share with unlimited range | "Don't force players to stay together" | AFK leeching; exploits with high-level alts power-leveling; breaks zone balance | Proximity requirement (same map + within range); level difference cap (±10 levels); contribution-based share |

## Feature Dependencies

```
[Party System]
    └──requires──> [Party Chat]
    └──requires──> [XP/Loot Distribution]
    └──optional──> [Tactical Formation] (differentiator)

[Guild System]
    └──requires──> [Guild Roster Management]
    └──requires──> [Guild Chat]
    └──requires──> [Guild Shared Storage]
    └──optional──> [Guild Skills/Buffs] (differentiator)
    └──optional──> [Guild Quests] (differentiator)

[P2P Market]
    └──requires──> [Item Listing Database]
    └──requires──> [Search/Filter System]
    └──requires──> [Transaction Ledger]
    └──optional──> [Market Analytics] (differentiator)
    └──optional──> [Buy Orders] (differentiator)

[Rebirth System]
    └──requires──> [Character Stat Reset]
    └──requires──> [Rebirth Count Tracking]
    └──optional──> [Class Change] (differentiator)
    └──optional──> [Rebirth-Exclusive Skills] (differentiator)

[Pet Rebirth]
    └──requires──> [Rebirth System] (character rebirth first)
    └──requires──> [Pet Stat System]
    └──optional──> [Pet Evolution Paths] (differentiator)

[Compound Enhancement System]
    └──enhances──> [P2P Market] (creates demand for materials)
    └──requires──> [Item Stat Modification]
    └──requires──> [Enhancement Materials Database]

[Guild Storage]
    ──conflicts──> [Item Duplication Bugs] (requires transaction atomicity)

[Market System]
    ──conflicts──> [JSON File Persistence] (needs PostgreSQL for transactions)
```

### Dependency Notes

- **Party System requires XP/Loot Distribution**: Core party incentive; without it, parties are just chat groups
- **Guild Storage requires Guild System**: Must establish guild membership before storage access
- **Rebirth System blocks Pet Rebirth**: Character rebirth mechanics must exist first; pet rebirth extends the system
- **Compound Enhancement enhances P2P Market**: Creates economy for materials; drives trading volume
- **Market System conflicts with JSON persistence**: Requires atomic transactions to prevent gold duplication; must use PostgreSQL
- **Guild Storage conflicts with concurrency bugs**: Without database transactions, item duplication exploits are likely

## MVP Definition

### Launch With (v1 - Commercial Release)

Minimum viable product for commercial launch.

- [x] **Direct player-to-player trade** - Face-to-face trading for trust-based transactions; table stakes for any MMORPG economy
- [x] **Centralized market with search** - Players can list/buy items without chat spam; core economy feature
- [x] **Market price history** - Players see recent pricing trends; prevents scams, informed decision-making
- [x] **Party invite/join/leave** - Basic party lifecycle; enables cooperative gameplay
- [x] **Shared XP distribution (even share)** - Incentivizes grouping; standard party mechanic
- [x] **Shared loot (round-robin)** - Fair loot distribution; prevents party drama
- [x] **Party chat channel** - Private communication for coordination
- [x] **Party member list UI** - See party HP/status; situational awareness in combat
- [x] **Guild create/join/leave** - Basic guild lifecycle; foundation for guild system
- [x] **Guild chat channel** - Guild communication; core social feature
- [x] **Guild ranks with permissions** - Leader controls invite/kick/promote; expected guild management
- [x] **Guild shared storage** - Resource sharing; table stakes from WLO/RO legacy
- [x] **Character rebirth (level reset + stat bonus)** - Core prestige mechanic; extends endgame progression
- [x] **Rebirth requirements (level 100 gate)** - Prevents prestige spam; maintains rebirth value
- [x] **Compound enhancement system** - Equipment progression; creates material economy

### Add After Validation (v1.1-v1.5)

Features to add once core is working and validated with players.

- [ ] **Tactical formation system (3x3 grid)** - Trigger: Players request more strategic depth in party combat
- [ ] **Market analytics dashboard** - Trigger: Merchant players emerge; economy matures
- [ ] **Guild skills/buffs** - Trigger: Guild retention metrics show players need tangible guild benefits
- [ ] **Pet rebirth with stat inheritance** - Trigger: Pet system completion + player demand for pet progression
- [ ] **Buy orders (reverse auction)** - Trigger: Market volume increases; players request WTB automation
- [ ] **Guild quests/missions** - Trigger: Guilds need shared objectives beyond storage/chat
- [ ] **Rebirth-exclusive skills** - Trigger: Rebirth 3+ players need additional progression rewards
- [ ] **Saved party compositions** - Trigger: Regular dungeon groups request QoL for re-invites

### Future Consideration (v2+)

Features to defer until product-market fit is established.

- [ ] **Class change on rebirth** - Reason: High complexity; validate single-class rebirth demand first
- [ ] **Guild vs Guild events** - Reason: Requires critical mass of guilds; PvP-focused feature post-launch
- [ ] **Cross-server market** - Reason: Only needed if multiple servers; defer until server capacity issues
- [ ] **Escrow for high-value trades** - Reason: Requires dispute resolution system; defer until scam reports justify
- [ ] **Party XP/loot contribution-based sharing** - Reason: Complex balancing; even share simpler for MVP
- [ ] **Guild crafting stations** - Reason: Requires crafting system expansion; defer to housing milestone
- [ ] **Rebirth quest chains with lore** - Reason: Content creation heavy; validate rebirth mechanics first

## Feature Prioritization Matrix

| Feature | User Value | Implementation Cost | Priority |
|---------|------------|---------------------|----------|
| Direct player-to-player trade | HIGH | LOW | P1 |
| Centralized market with search | HIGH | MEDIUM | P1 |
| Party invite/join/leave | HIGH | LOW | P1 |
| Shared XP distribution | HIGH | MEDIUM | P1 |
| Shared loot (round-robin) | HIGH | MEDIUM | P1 |
| Guild create/join/leave | HIGH | LOW | P1 |
| Guild chat | HIGH | LOW | P1 |
| Guild shared storage | HIGH | MEDIUM | P1 |
| Character rebirth | HIGH | MEDIUM | P1 |
| Compound enhancement | HIGH | MEDIUM | P1 |
| Party chat | MEDIUM | LOW | P1 |
| Party member list UI | MEDIUM | MEDIUM | P1 |
| Guild ranks/permissions | MEDIUM | MEDIUM | P1 |
| Market price history | MEDIUM | LOW | P1 |
| Rebirth requirements/gates | MEDIUM | LOW | P1 |
| Tactical formation system | HIGH | HIGH | P2 |
| Market analytics dashboard | MEDIUM | HIGH | P2 |
| Guild skills/buffs | MEDIUM | MEDIUM | P2 |
| Pet rebirth | MEDIUM | HIGH | P2 |
| Buy orders | MEDIUM | HIGH | P2 |
| Guild quests | MEDIUM | MEDIUM | P2 |
| Rebirth-exclusive skills | LOW | MEDIUM | P2 |
| Class change on rebirth | HIGH | HIGH | P3 |
| Guild vs Guild events | MEDIUM | HIGH | P3 |
| Cross-server market | LOW | HIGH | P3 |
| Escrow system | LOW | MEDIUM | P3 |

**Priority key:**
- P1: Must have for commercial launch - missing these makes product feel incomplete
- P2: Should have after validation - adds depth once core is proven
- P3: Nice to have for future - differentiators for later competitive advantage

## Competitor Feature Analysis

| Feature | Wonderland Online (WLO) | Ragnarok Online (RO) | Tree of Savior (ToS) | TWL Approach |
|---------|-------------------------|----------------------|----------------------|--------------|
| **Market System** | Vending shops (AFK player stores) | Both player shops + centralized markets in some versions | Market + personal stalls + restricted P2P trade (30/month) | Centralized auction house (avoids ghost town problem) + direct trade option |
| **Party Size** | Variable | 12 players max | 5 players standard | 4 players max (tactical turn-based focus) |
| **XP Share** | Even share, proximity-based | Even share + level difference limits (±10 base levels) | Proximity required or limited utility | Even share with proximity + level difference cap |
| **Loot Distribution** | Free-for-all + master looter options | Round-robin, random, master looter | Party loot with sharing restrictions | Round-robin default (fairest for turn-based) |
| **Guild Storage** | Shared storage with basic permissions | No built-in guild storage (unofficial workaround) | Dual storage (quest rewards + shared) with rank-based permissions | Permission-based storage with transaction logs |
| **Guild Features** | Guild chat, sieges (weekly events), basic ranks | Guild skills, alliances, WoE (War of Emperium), emblems | Guild quests, territory wars (GTW), tariff system, extensive permissions | Chat + storage + ranks for MVP; guild skills/quests post-launch |
| **Rebirth System** | Character rebirth with class change, stat redistribution, level reset to 1 | No rebirth (uses transcendent classes instead) | Rebirth system with rank progression, class advancement | Level reset + stat bonus + rebirth count tracking; defer class change to v2 |
| **Pet System** | Pets with skills, evolution, rebirth mechanics | Pets primarily cosmetic/utility (no combat in most versions) | Companion system with combat support | Quest pets (story-locked) + capturable pets with rebirth/evolution |
| **Enhancement** | Compound system with materials, % success rates | Refine system with materials, can break items at high levels | Enhancement with protection crystals, failstack mechanics | Compound with failstacks (no item destruction - anti-feature) |

## Implementation Notes by System

### P2P Market System

**Table Stakes Implementation:**
- Centralized database table: `market_listings` (item_id, seller_id, price, quantity, listed_at, expires_at)
- Search API with filters: item name (LIKE), item type, price range, rarity
- Transaction flow: buyer confirms → atomic gold transfer → remove listing → add item to buyer inventory → tax deduction
- Price history: track last 50 transactions per item_type in `market_history` table
- Listing expiration: daily cron job to expire listings older than 72 hours, return items to seller's mail/storage

**Critical Security:**
- Atomic transactions required (PostgreSQL, not JSON files)
- Prevent listing duplication (item removed from inventory on listing)
- Prevent gold duplication (single transaction for gold transfer + item transfer)
- Rate limiting on listings (max 20 active listings per player to prevent spam)

**WLO Lesson:** Vending shops created ghost towns of AFK players. Centralized market keeps players active.

### Party System

**Table Stakes Implementation:**
- Party entity: max 4 members, leader ID, XP share mode, loot mode
- Invite flow: leader sends invite → target accepts → add to party (reject if full)
- XP calculation: total XP / party member count (if within proximity + level range)
- Loot distribution: round-robin queue, next item goes to next player in sequence
- Party chat: scoped message broadcast with `[Party]` prefix

**Turn-Based Specific:**
- Combat order: party members get consecutive turns (avoid interleaving with enemies for clarity)
- Formation positions: if tactical grid implemented, enforce front/mid/back row restrictions

**RO Lesson:** 12-player parties too large for coordination. 4-player cap forces meaningful composition.

### Guild System

**Table Stakes Implementation:**
- Guild entity: guild_id, name (unique), leader_id, created_at, member_count (max 50 for MVP)
- Ranks: predefined 5 ranks (Leader, Officer, Veteran, Member, Recruit) with permission flags
- Permissions: can_invite, can_promote, can_kick, can_withdraw_storage (bitmask or JSON)
- Guild storage: separate inventory table with guild_id FK, transaction log (who deposited/withdrew what)
- Guild chat: message table with guild_id, sender_id, message, timestamp

**Critical Security:**
- Transaction logs for storage prevent "he said, she said" disputes
- Only leader can disband guild (prevent hostile takeovers)
- Kick protection: cannot kick players with higher rank

**ToS Lesson:** Dual storage (quest rewards + shared) creates complexity. Single shared storage simpler for MVP.

### Rebirth System

**Table Stakes Implementation:**
- Rebirth counter in character table: `rebirth_count` (integer, default 0)
- Rebirth requirements: level >= 100, optional rebirth scroll item
- Rebirth flow: reset level to 1 → increment rebirth_count → grant bonus stats (e.g., 20 points)
- Stat allocation: redistribute bonus stats manually or auto-distribute to existing ratio
- Display: show rebirth count in character nameplate/profile ("Name [Rebirth 3]")

**Balance Tuning:**
- Diminishing returns: Rebirth 1 = 20 stats, Rebirth 2 = 15 stats, Rebirth 3 = 10 stats, Rebirth 4+ = 5 stats
- First rebirth at level 100, second at 100 again (not cumulative level requirement)

**WLO Lesson:** Class change on rebirth adds massive complexity. Defer to v2; validate stat-only rebirth first.

### Compound Enhancement System

**Table Stakes Implementation:**
- Enhancement level per item: `enhancement_level` (0-10 common max)
- Materials required: defined per enhancement level (e.g., level 1→2 = 5 copper ore, 100 gold)
- Success rate: decreasing % (level 0→1 = 95%, level 9→10 = 10%)
- Failstack mechanic: each failure increments hidden counter, increases next attempt success rate (+5% per fail)
- No item destruction on failure (anti-feature) - instead, reset failstacks or lose enhancement level

**Economy Impact:**
- Creates demand for materials (drives P2P market volume)
- Gold sink through enhancement costs
- Endgame progression without new content (players enhance gear to +10)

**Black Desert Lesson:** Item destruction creates rage-quits. Failstacks without destruction maintain tension without punishment.

## Sources

### Market/Trading Systems
- [12 Best MMOs for Traders and Merchants in 2024](https://mmorpg.gg/mmos-with-the-best-economies/)
- [The Best Open-World Games With Player-Driven Economies](https://gamerant.com/open-world-games-player-driven-economies/)
- [New MMORPG LORDNINE: NEXT Market Trading Platform](https://laotiantimes.com/2025/08/08/new-mmorpg-lordnine-infinite-class-opened-global-trading-platform-next-market/)
- [Face to face trading vs. Auction House — MMORPG.com Forums](https://forums.mmorpg.com/discussion/490877/face-to-face-trading-vs-auction-house)
- [What is your preferred type of MMO marketplace? — MMORPG.com Forums](https://forums.mmorpg.com/discussion/292094/what-is-your-preferred-type-of-mmo-marketplace)
- [Auction House or Player Trading? Community Debate](https://www.maplestoryclassicworld.com/news/updates/auction-house-debate)

### Party Systems
- [MMORPG Party and Roles 101 | Gamer Horizon](https://gamerhorizon.com/2014/08/14/mmorpg-party-roles-101/)
- [9 Turn-Based MMO Games To Check Out](https://www.mmobomb.com/top-turn-based-mmos)
- [Ragnarok Online Party System - Ragnarok Wiki](https://ragnarok.fandom.com/wiki/Party_System)
- [Ragnarok Online/Party Play — StrategyWiki](https://strategywiki.org/wiki/Ragnarok_Online/Party_Play)
- [Broken Ranks Turn-Based Combat System](https://www.mmorpg.com/news/broken-ranks-devs-introduce-the-strategic-turn-based-combat-system-2000124166)

### Guild Systems
- [5 Best Guild Systems In MMOs, Ranked](https://gamerant.com/best-mmo-guild-systems/)
- [What are the MMOs with the best guild systems? — MMORPG.com Forums](https://forums.mmorpg.com/discussion/389695/what-are-the-mmos-with-the-best-guild-systems)
- [Guild System - iRO Wiki](https://irowiki.org/wiki/Guild_System)
- [Guilds - Ragnarok Wiki](https://ragnarok.fandom.com/wiki/Guilds)
- [Guild Bank - Warcraft Wiki](https://warcraft.wiki.gg/wiki/Guild_bank)
- [Tree of Savior Guild Quests](https://treeofsavior.fandom.com/wiki/Guild_Quests)

### Rebirth Systems
- [Massively Overthinking: Prestige systems in MMORPGs](https://massivelyop.com/2016/09/15/massively-overthinking-prestige-systems-in-mmorpgs/)
- [Rebirth System | MMORPG.com](https://www.mmorpg.com/news/rebirth-system-2000067302)
- [Rebirth & Reincarnation - Conquer Online](https://co.99.com/guide/event/2013/rebirth/rebirth-1-6.shtml)
- [Pets - Mabinogi World Wiki (Pet Rebirth)](https://wiki.mabinogiworld.com/view/Pets)

### Enhancement Systems
- [Four Winds: Item Enhance Systems in MMOs](https://massivelyop.com/2022/10/13/four-winds-the-refinement-of-item-enhance-systems-in-mmos-from-granado-espada-to-black-desert/)
- [Blessed Upgrade Scroll Guide 2026](https://www.desertroudies.com/blessed-upgrade-scroll-guide/)
- [Upgrade System (Reinforcement) | DEKARON](https://www.dekaron.asia/main/guides/110)

### WLO-Specific
- [Wonderland Online Beginner's Guide](https://wlopserver.boards.net/thread/89/beginners-guide-wonderland-online)
- [Wonderland Online Guild System](https://wonderlandonline.fandom.com/wiki/Guild)
- [Wonderland Online Game Review](https://mmos.com/review/wonderland-online)

---
*Feature research for: The Wonderland Legacy (TWL) - Turn-based MMORPG multiplayer systems*
*Researched: 2026-02-14*

# Pitfalls Research: MMORPG Multiplayer Systems

**Domain:** MMORPG Market/Trading, Party, Guild, and Rebirth Systems
**Researched:** 2026-02-14
**Confidence:** HIGH (verified with real-world MMORPG disasters and technical research)

## Critical Pitfalls

### Pitfall 1: Race Condition Item/Gold Duplication

**What goes wrong:**
When database transactions are not atomic, players can exploit timing windows to duplicate items or currency. Multiple simultaneous requests can bypass validation checks before any of them complete, allowing withdrawal of the same resource multiple times.

**Why it happens:**
Balance checks and inventory updates happen as separate operations instead of within a single database transaction. The server validates "does player have item?" then later performs "remove item" - between these operations, another request can also pass the validation check.

**Real-world examples:**
- **New World (2021)**: Amazon had to disable all trading, player-to-player transfers, guild treasuries, and the trading post multiple times due to gold duplication exploits. Players exploited connection timing and trade window crashes to duplicate items. Ironically, the trade disable itself enabled another dupe method where town upgrades could be started/cancelled to duplicate gold.
- **MapleStory Europe (2011)**: Currency exploit caused complete economy collapse. Players exploited the "Meso Guard" skill with negative damage values to generate over 2 billion mesos, then bought up entire marketplaces, causing massive inflation. Nexon's response banned legitimate players who unknowingly received duped currency.
- **MapleStory (2025)**: 228 accounts permanently banned for duplication exploit in patch v.260 involving mesos and items.

**How to avoid:**
```csharp
// WRONG: Separate check and update (race condition)
if (player.Gold >= price) {
    await Task.Delay(1); // Network latency
    player.Gold -= price;
}

// CORRECT: Atomic transaction with database-level locking
using var transaction = await db.BeginTransactionAsync(IsolationLevel.Serializable);
try {
    var player = await db.Players
        .Where(p => p.Id == playerId)
        .ForUpdate() // Row-level lock
        .FirstOrDefaultAsync();

    if (player.Gold < price) {
        await transaction.RollbackAsync();
        return Error("Insufficient funds");
    }

    player.Gold -= price;
    buyer.AddItem(itemId);
    seller.Gold += price;
    seller.RemoveItem(itemId);

    await db.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch {
    await transaction.RollbackAsync();
    throw;
}
```

**Warning signs:**
- Players reporting "double charges" or "lost items" during lag spikes
- Unusual wealth accumulation by specific players during server instability
- Market prices spiraling out of control
- Reports of players deliberately lagging themselves (connection manipulation)

**Phase to address:**
Phase 1: Market Foundation - MUST implement transaction safety from day one. Retrofitting atomic transactions is extremely difficult and may require database migration.

---

### Pitfall 2: Incomplete Transaction Rollback (Partial State)

**What goes wrong:**
When a market transaction fails halfway (buyer charged but item not delivered, or vice versa), the system fails to rollback ALL state changes. Players lose items or currency permanently.

**Why it happens:**
Multi-step operations (deduct currency → add item to buyer → remove item from seller → add currency to seller) don't wrap all steps in a transaction. If step 3 fails, steps 1-2 already committed.

**Real-world examples:**
- **Lost Ark (2022-2024)**: Multiple incidents where Founder's Pack items could be redeemed multiple times. Amazon tracked RMT gold through "multiple accounts" and banned entire transaction chains. The Ignite Server Stronghold exploit (2024) affected 2,300 players, with varying punishments based on exploitation severity.

**How to avoid:**
- Use database transactions with Serializable isolation level for ALL multi-party operations
- Implement compensating transactions if rollback fails (log to recovery queue)
- Add idempotency keys to prevent replay attacks during retries
- Test transaction rollback under network partition scenarios

**Warning signs:**
- Players reporting "money disappeared" or "didn't receive item"
- Database showing orphaned records (items with no owner, currency deltas that don't sum to zero)
- Support tickets about incomplete trades

**Phase to address:**
Phase 1: Market Foundation - Core transaction infrastructure must handle all-or-nothing semantics.

---

### Pitfall 3: Missing Idempotency Protection (Replay Attacks)

**What goes wrong:**
Network retries cause duplicate operations. Player buys item once, packet is delayed, client retries, server processes purchase twice. Player charged twice or receives double items.

**Why it happens:**
Server doesn't track operation IDs. Each incoming "BuyItem" request is treated as new, even if it's a retry of a previous request.

**Real-world examples:**
- **Diablo IV (2023)**: Had to disable trading completely after gold and item duplication exploit. Players exploited connection timing to replay trade operations.
- **RuneScape (2003)**: Magenta Party hat duped over 2 million times, affecting economy permanently even decades later.

**How to avoid:**
```csharp
public class MarketTransaction
{
    public string OperationId { get; set; } // Client-generated UUID
    public TransactionState State { get; set; } // Pending/Completed/Failed
    public DateTime Timestamp { get; set; }
}

// On every market operation:
if (_transactions.TryGetValue(operationId, out var tx)) {
    if (tx.State == TransactionState.Completed) {
        return new Result {
            Success = true,
            Message = "Already completed",
            // Return original result (idempotent)
        };
    }
    if (tx.State == TransactionState.Pending) {
        return new Result {
            Success = false,
            Message = "Transaction in progress"
        };
    }
}
```

**Warning signs:**
- Players reporting double-charges during lag
- Same transaction appearing multiple times in logs with identical timestamps
- Items duplicating in inventory after network interruptions

**Phase to address:**
Phase 1: Market Foundation - Idempotency MUST be built into the first market implementation. The codebase already has idempotency in `EconomyManager` - extend this pattern to P2P trading.

---

### Pitfall 4: Guild Bank Permission Escalation

**What goes wrong:**
Guild permission systems allow members to withdraw more than intended, or members can "upgrade" their own permissions through exploits. Trusted members clean out the guild bank.

**Why it happens:**
Permission hierarchies are poorly designed (e.g., "if you can sort items, you can withdraw items"), or permission checks happen client-side, or rank changes don't properly update withdrawal limits.

**Real-world examples:**
- **Turtle WoW (2024)**: Guild bank theft epidemic. Design flaw: withdrawal limits couldn't be set per rank - if you set a limit for a lower rank, all higher ranks automatically got unlimited access. Members would get promoted, then empty the bank.
- **Albion Online (2019)**: 1 billion worth of in-game assets stolen from guild by single player who gained trust over months then ransacked everything.
- **Black Desert Online**: Materials from guild storage could be retrieved by ANY member including apprentices on trial periods. Players joined guilds, grabbed materials, converted to marketable items, then left.

**How to avoid:**
- Implement time-gated permissions (new members wait 1-2 weeks before withdrawal rights)
- Separate "view" vs "withdraw" vs "manage" permissions granularly
- Log all guild bank withdrawals with automatic alerts for large amounts
- Set per-rank daily withdrawal limits that apply independently (not inherited)
- Require two-factor authentication (guild leader approval) for high-value withdrawals

```csharp
public class GuildBankPermissions
{
    public Dictionary<int, RankPermissions> RankPermissions { get; set; }

    public bool CanWithdraw(GuildMember member, int tabId, int itemValue)
    {
        var permissions = RankPermissions[member.RankId];

        // Time gate
        if ((DateTime.UtcNow - member.JoinedAt).TotalDays < 7) {
            return false;
        }

        // Daily limit
        var todayWithdrawn = GetTodayWithdrawals(member.Id);
        if (todayWithdrawn + itemValue > permissions.DailyLimit) {
            return false;
        }

        // Tab-specific permissions
        if (!permissions.AllowedTabs.Contains(tabId)) {
            return false;
        }

        // High-value requires approval
        if (itemValue > 10000 && !HasLeaderApproval(member.Id)) {
            return false;
        }

        return true;
    }
}
```

**Warning signs:**
- New guild members asking for promotions quickly
- Large withdrawals happening right before member leaves guild
- Multiple guilds reporting theft by same player account
- Guild bank emptying out overnight

**Phase to address:**
Phase 2: Guild Foundation - Permission system must be designed correctly from the start. Changing permission semantics after guilds have inventory is extremely disruptive.

---

### Pitfall 5: Party Kick Abuse & Ninja Looting

**What goes wrong:**
Party leaders kick members right before boss dies to steal their loot share, or members use "Need" on all items regardless of class/need (ninja looting).

**Why it happens:**
Loot distribution happens AFTER boss kill, and kick permission isn't restricted during combat. Or loot system allows any player to roll Need without class restrictions.

**Real-world examples:**
- **Neverwinter**: Players abused vote-to-kick feature at or near end of boss fights, reducing the pool of players who could access drops. Led to major system overhaul.
- **World of Warcraft**: Ninja looting epidemic in early years. Party leaders changed loot to "Master Loot" right before boss kill and took everything. Led to implementation of Personal Loot system.

**How to avoid:**
- Disable party kick during combat and for 5 minutes after boss kill
- Lock loot eligibility when boss fight starts (anyone in party at pull gets loot, regardless of later kicks)
- Implement loot lockout: kicked players still get loot from boss they helped kill
- Personal loot system: each player gets individual roll, no competition
- Class-restrict Need rolls: only warriors can Need on warrior gear
- Reputation system: ninja looters get flagged, other players can see their history

```csharp
public class PartyLootSystem
{
    public bool CanKickMember(Party party, int memberId)
    {
        // Cannot kick during combat
        if (party.InCombat) return false;

        // Cannot kick if boss died in last 5 minutes
        if (party.LastBossKillTime != null &&
            (DateTime.UtcNow - party.LastBossKillTime.Value).TotalMinutes < 5) {
            return false;
        }

        return true;
    }

    public List<LootEligiblePlayer> GetLootEligiblePlayers(Boss boss)
    {
        // Lock eligibility at pull, not at kill
        return boss.PlayersAtPullTime; // Not party.CurrentMembers
    }

    public bool CanRollNeed(Player player, Item item)
    {
        // Class restriction
        if (!item.UsableByClasses.Contains(player.Class)) {
            return false; // Can only Greed or Pass
        }

        // Already have better item
        if (player.HasBetterItem(item)) {
            return false;
        }

        return true;
    }
}
```

**Warning signs:**
- Players reporting being kicked right before loot
- Same party leader appearing in multiple kick complaints
- Items going to players who can't use them (wrong class)
- Party finder showing very low completion rates for certain players

**Phase to address:**
Phase 2: Party Foundation - Loot rules must be finalized before party system launches. Changing loot rules mid-game causes massive player backlash.

---

### Pitfall 6: AFK Party Leeching (XP Exploitation)

**What goes wrong:**
Players join parties and go AFK to leech XP/loot from active players. Or high-level players "carry" low-level alts for power-leveling without doing any work.

**Why it happens:**
Party XP sharing doesn't require active participation. Distance check is too lenient. No contribution tracking.

**Real-world examples:**
- **Guild Wars 2**: Sparkfly Fen event allowed AFK leeching for XP/karma/gold. Players would park characters and farm events overnight.
- **MapleStory**: High-level characters kill difficult monsters while low-level characters (same owner) participate for sole purpose of getting XP without contributing.

**How to avoid:**
- Require damage/healing contribution for XP (minimum % of party total)
- Implement proximity requirement: must be within X meters of combat
- Add active input check: if no actions in 2 minutes, no XP share
- Show contribution metrics to party members (transparency prevents abuse)
- Diminishing returns for level gaps: if 20 levels apart, reduced XP share

```csharp
public class PartyXPDistribution
{
    public void DistributeXP(Party party, Enemy enemy, int totalXP)
    {
        var eligibleMembers = new List<PartyMember>();

        foreach (var member in party.Members)
        {
            // Proximity check
            if (Vector2.Distance(member.Position, enemy.Position) > 100) {
                continue;
            }

            // Contribution check (did at least 5% of damage OR healing)
            var contribution = GetContribution(member, enemy);
            if (contribution < 0.05f) {
                continue;
            }

            // Level gap penalty
            var levelDiff = Math.Abs(member.Level - enemy.Level);
            if (levelDiff > 20) {
                continue; // No XP if too far from enemy level
            }

            // Activity check
            if ((DateTime.UtcNow - member.LastActionTime).TotalSeconds > 120) {
                continue; // AFK for 2+ minutes
            }

            eligibleMembers.Add(member);
        }

        // Split XP among eligible members
        var xpPerMember = totalXP / eligibleMembers.Count;
        foreach (var member in eligibleMembers)
        {
            member.AddXP(xpPerMember);
        }
    }

    private float GetContribution(PartyMember member, Enemy enemy)
    {
        var damageContribution = member.DamageDealt / enemy.TotalDamageTaken;
        var healingContribution = member.HealingDone / party.TotalHealingReceived;
        return Math.Max(damageContribution, healingContribution);
    }
}
```

**Warning signs:**
- Players standing still during boss fights but receiving loot
- Party members with 0 damage dealt but full XP
- Reports of "AFK farmers" in popular grinding spots
- Unusual XP gain rates on low-level characters

**Phase to address:**
Phase 2: Party Foundation - XP sharing rules should be strict from launch. Tightening rules later causes player complaints ("you're nerfing my playstyle").

---

### Pitfall 7: Rebirth/Prestige Stat Duplication

**What goes wrong:**
When rebirth resets character level but grants permanent stat bonuses, players exploit the rebirth process to duplicate stat bonuses or reset without losing stats.

**Why it happens:**
Rebirth transaction isn't atomic. Server checks "eligible for rebirth?" → "reset level" → "grant bonus stats" as separate operations. If process fails between steps 2 and 3, player loses everything. If it fails between steps 1 and 2, they can retry and get double bonuses.

**Real-world examples:**
- **Ragnarok Online**: Rebirth system (Transcendent Classes) had various exploits over the years. Players found ways to avoid damage/status effects by manipulating screen positioning during stat recalculation.
- **Myth War 2**: Rebirth at level 120 grants 160 bonus stat points + 8 per level. Exploiting the rebirth timing could duplicate these bonuses.

**How to avoid:**
```csharp
public class RebirthSystem
{
    public async Task<RebirthResult> PerformRebirth(ServerCharacter character)
    {
        // Idempotency check
        if (character.HasRebirthInProgress) {
            return new RebirthResult {
                Success = false,
                Message = "Rebirth already in progress"
            };
        }

        using var transaction = await db.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            // Lock character row
            var charLocked = await db.Characters
                .Where(c => c.Id == character.Id)
                .ForUpdate()
                .FirstOrDefaultAsync();

            // Validation
            if (charLocked.Level < 120) {
                await transaction.RollbackAsync();
                return new RebirthResult {
                    Success = false,
                    Message = "Must be level 120"
                };
            }

            if (charLocked.RebirthCount >= 10) {
                await transaction.RollbackAsync();
                return new RebirthResult {
                    Success = false,
                    Message = "Maximum rebirths reached"
                };
            }

            // Atomic rebirth operation
            var snapshot = charLocked.CreateSnapshot(); // For rollback

            charLocked.RebirthCount++;
            charLocked.Level = 1;
            charLocked.Experience = 0;

            // Grant permanent bonuses BEFORE commit
            var bonusStats = 160 + (charLocked.RebirthCount * 8);
            charLocked.BonusStatPoints += bonusStats;

            // Log rebirth (prevent future exploitation)
            await db.RebirthHistory.AddAsync(new RebirthRecord {
                CharacterId = charLocked.Id,
                Timestamp = DateTime.UtcNow,
                RebirthNumber = charLocked.RebirthCount,
                BonusStatsGranted = bonusStats,
                Snapshot = snapshot // For audit
            });

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return new RebirthResult {
                Success = true,
                BonusStats = bonusStats,
                NewRebirthCount = charLocked.RebirthCount
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            // Log failed rebirth attempt for security monitoring
            SecurityLogger.LogSecurityEvent("RebirthFailed", character.Id,
                $"Exception: {ex.Message}");

            return new RebirthResult {
                Success = false,
                Message = "Rebirth failed, please try again"
            };
        }
    }
}
```

**Warning signs:**
- Players with abnormally high stats for their rebirth count
- Reports of "rebirth failed but I lost my level"
- Multiple rebirth attempts in quick succession (replay attack indicator)
- Database showing rebirth count mismatches with stat bonuses

**Phase to address:**
Phase 3: Rebirth System - Must be atomic from day one. Rebirth exploits are extremely hard to detect and roll back because they compound over time.

---

### Pitfall 8: Marketplace Price Manipulation & Wash Trading

**What goes wrong:**
Players create multiple accounts to fake market activity (wash trading), manipulate prices by buying/selling to themselves, or corner markets by buying all supply and relisting at inflated prices.

**Why it happens:**
No restrictions on self-trading, no detection for suspicious patterns, no limits on market share concentration.

**Real-world examples:**
- **EVE Online**: Players manipulate markets by attacking trade hubs to create shortages, driving up demand and inflating prices.
- **World of Warcraft**: Auction house "farming" - players use addons to discover market niches and monopolize them. Blizzard had to update exploitation policy to restrain auction house farming.

**How to avoid:**
- Ban trading between accounts from same IP/device
- Implement market share limits (one player can't own >30% of an item's listings)
- Add listing fees (non-refundable) to prevent wash trading spam
- Detect suspicious patterns: same buyer/seller repeatedly trading same item
- Delay price changes: new listings can't be <80% or >120% of median price for first hour
- Velocity limits: can't list more than X items of same type per day

```csharp
public class MarketplaceAntiManipulation
{
    public bool CanCreateListing(Player player, Item item, int price)
    {
        // Market share check
        var existingListings = GetListingsForItem(item.Id);
        var playerListings = existingListings.Count(l => l.SellerId == player.Id);
        var marketShare = (float)playerListings / existingListings.Count;

        if (marketShare > 0.3f) {
            return false; // Cannot control >30% of market
        }

        // Price bounds check (first hour)
        var medianPrice = GetMedianPrice(item.Id);
        if (price < medianPrice * 0.8f || price > medianPrice * 1.2f) {
            // Allow but flag for review
            SecurityLogger.LogSecurityEvent("MarketPriceOutlier", player.Id,
                $"Item:{item.Id} Price:{price} Median:{medianPrice}");
        }

        // Velocity check
        var todayListings = GetTodayListings(player.Id, item.Id);
        if (todayListings > 100) {
            return false; // Listing spam
        }

        return true;
    }

    public bool IsWashTrading(int buyerId, int sellerId, int itemId)
    {
        // Same IP/device
        if (GetIPAddress(buyerId) == GetIPAddress(sellerId)) {
            return true;
        }

        // Repeated back-and-forth trades
        var recentTrades = GetRecentTrades(buyerId, sellerId, itemId, days: 7);
        if (recentTrades.Count > 10) {
            return true; // Suspicious pattern
        }

        return false;
    }
}
```

**Warning signs:**
- Same item listed/delisted repeatedly by same player
- New accounts immediately listing high-value items
- Prices spiking without supply shortage
- Player owning majority of listings for valuable item

**Phase to address:**
Phase 1: Market Foundation - Anti-manipulation needs to be in the initial market design. Adding restrictions later causes legitimate traders to complain.

---

### Pitfall 9: Compound Interest Exploit (Listing Fee Arbitrage)

**What goes wrong:**
If marketplace allows free listing cancellations or listing fees are refundable, players exploit compound interest by listing/canceling repeatedly to generate currency or manipulate markets.

**Why it happens:**
Listing fees are refunded on cancellation, or cancellation is free. Players list high-value items, cancel, relist at different price, creating artificial activity and potentially earning interest or bonuses.

**How to avoid:**
- Make listing fees NON-REFUNDABLE (burn the currency)
- Add cooldown: can't relist same item for 1 hour after cancellation
- Limit cancellations: max 5 per day per player
- Escalating fees: each cancellation costs more

```csharp
public class ListingFeeSystem
{
    public bool CancelListing(Player player, Listing listing)
    {
        // Fee is NOT refunded
        var fee = listing.Price * 0.05f; // 5% listing fee was already paid
        // Fee is gone forever (deflationary measure)

        // Cooldown enforcement
        listing.CanceledAt = DateTime.UtcNow;
        AddCooldown(player.Id, listing.ItemId, hours: 1);

        // Count cancellations
        var todayCancellations = GetTodayCancellations(player.Id);
        if (todayCancellations >= 5) {
            return false; // Daily limit reached
        }

        return true;
    }
}
```

**Warning signs:**
- Players listing/canceling same items repeatedly
- Unusual currency generation without corresponding trading activity
- Market flooded with listings that never sell (cancel before expiry)

**Phase to address:**
Phase 1: Market Foundation - Fee structure must be set at launch. Changing fees later disrupts market equilibrium.

---

### Pitfall 10: Guild Chat Command Injection

**What goes wrong:**
Guild chat allows special characters or commands that can be exploited to execute unintended actions (kick members, promote, withdraw from bank) or inject malicious content.

**Why it happens:**
Chat parsing doesn't sanitize input. Commands like "/kick PlayerName" are processed even when sent as chat messages. Or HTML/markdown injection allows phishing links.

**Real-world examples:**
- **New World (2021)**: Players could post unsavory images via in-game chat and use scripting to crash other players or gain gold through chat injection exploits.

**How to avoid:**
```csharp
public class GuildChatSystem
{
    public void ProcessChatMessage(GuildMember sender, string message)
    {
        // Sanitize input
        message = SanitizeInput(message);

        // Disable commands in chat (only work via dedicated command UI)
        if (message.StartsWith("/")) {
            SendError(sender, "Commands cannot be sent via chat");
            return;
        }

        // Length limit
        if (message.Length > 500) {
            message = message.Substring(0, 500);
        }

        // HTML/script injection prevention
        message = HttpUtility.HtmlEncode(message);

        BroadcastToGuild(sender.GuildId, message);
    }

    private string SanitizeInput(string input)
    {
        // Remove null characters
        input = input.Replace("\0", "");

        // Remove SQL injection attempts
        input = Regex.Replace(input, @"[';\""-]", "");

        // Remove script tags
        input = Regex.Replace(input, @"<script.*?>.*?</script>", "",
            RegexOptions.IgnoreCase);

        return input;
    }
}
```

**Warning signs:**
- Reports of "guild chat crashed my game"
- Players getting kicked/promoted through chat messages
- Phishing links appearing in guild chat

**Phase to address:**
Phase 2: Guild Foundation - Input sanitization must be thorough from launch. Chat exploits spread rapidly.

---

## Technical Debt Patterns

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| File-based storage instead of PostgreSQL transactions | Faster initial development | Race conditions, no ACID guarantees, duplication exploits | NEVER for multiplayer trading/currency |
| Client-side validation only | Simpler server code | Trivial to bypass, enables cheating | Never for authoritative actions |
| Optimistic locking instead of pessimistic | Better throughput | Requires retry logic, harder to debug | Only for non-critical reads |
| In-memory transaction tracking only | Fast, no DB overhead | State lost on server crash, no audit trail | Never for real-money transactions |
| Same table for all transaction types | Simpler schema | Poor indexing, slow queries at scale | Only for prototypes/MVPs |
| No rate limiting on market operations | Simpler code | Vulnerable to spam, DoS, dupe exploits | Never in production |

## Performance Traps

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| N+1 queries for party member data | Lag spikes when opening party UI | Eager load party members in single query | >5 members per party |
| Full table scan for marketplace listings | Search gets slower daily | Index on ItemId, Price, Timestamp | >10,000 listings |
| Synchronous transaction commits | Trade confirmation takes 2+ seconds | Use async commits with replication | >100 concurrent trades |
| No pagination on guild member list | Guild UI freezes | Paginate with limit 50 per page | >200 guild members |
| Real-time XP sync to database | Database overwhelmed during raids | Batch XP updates every 10 seconds | >20 party members |
| Linear search for rebirth eligibility | Character screen lag at high level | Index on Level and RebirthCount | Level >100 |

## Security Mistakes

| Mistake | Risk | Prevention |
|---------|------|------------|
| Client sends final gold amount after trade | Client can send ANY amount | Server calculates EVERYTHING, client is display only |
| Guild permissions stored client-side | Trivial permission escalation | All permission checks server-side with DB validation |
| Party kick uses client-provided reason | Reason could be command injection | Sanitize ALL text input, use enum for kick reasons |
| Rebirth stat bonuses trust client stats | Client can claim any bonus | Server calculates bonuses from RebirthCount in DB |
| Marketplace search by user input | SQL injection vulnerability | Use parameterized queries, whitelist search fields |
| No logging for guild bank withdrawals | Theft goes undetected | Log EVERY withdrawal with timestamp, item, member ID |

## UX Pitfalls

| Pitfall | User Impact | Better Approach |
|---------|-------------|-----------------|
| Market listings expire while player offline | Lost listing fees, items disappear | Auto-relist OR send items to mailbox on expiry |
| Party kicked mid-dungeon loses ALL progress | Frustration, quit game | Kicked players keep loot eligibility, can rejoin |
| Guild bank has no withdrawal history | Leaders can't detect theft | Full audit log visible to officers+ |
| Rebirth fails with no clear error | Players lose trust in system | Show detailed error + prevention (level requirement) |
| No confirmation for high-value market listings | Accidental 1 gold listing of rare item | Require confirmation if price <50% of average |
| Party XP share invisible to members | Members don't know if leech is present | Show contribution % to all party members |

## "Looks Done But Isn't" Checklist

- [ ] **Market Transactions:** Often missing idempotency keys — verify by simulating network retry (should not double-charge)
- [ ] **Party Loot:** Often missing kick protection during combat — verify kick button disables when in combat
- [ ] **Guild Permissions:** Often missing time gates for new members — verify new member can't withdraw immediately
- [ ] **Rebirth System:** Often missing transaction rollback — verify failed rebirth doesn't lose player progress
- [ ] **Marketplace Search:** Often missing pagination — verify searching 50,000 items doesn't timeout
- [ ] **Party XP:** Often missing contribution tracking — verify AFK player gets 0 XP
- [ ] **Trade History:** Often missing audit logs — verify every gold/item transfer is logged immutably
- [ ] **Rate Limiting:** Often missing per-user limits — verify single player can't spam 1000 listings/second

## Recovery Strategies

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| Item duplication discovered | HIGH | 1. Disable trading immediately 2. Analyze logs for dupe pattern 3. Remove duped items 4. Rollback affected accounts 5. Ban exploiters |
| Guild bank cleaned out | MEDIUM | 1. Restore from backup snapshot 2. Review withdrawal logs 3. Ban thief 4. Compensate guild if rollback not possible |
| Market wash trading ring | MEDIUM | 1. Identify alt account network via IP/device 2. Ban all accounts 3. Remove manipulated listings 4. Refund legit buyers |
| Party kick abuse | LOW | 1. Restore loot eligibility to kicked player 2. Warn/ban abuser 3. Implement kick protection |
| Rebirth stat exploit | HIGH | 1. Audit all rebirth history records 2. Recalculate correct stats 3. Fix discrepancies 4. Permanent ban for intentional exploitation |
| Economy hyperinflation | VERY HIGH | 1. Emergency gold sink events 2. Adjust drop rates 3. May require server rollback or economy reset |

## Pitfall-to-Phase Mapping

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| Race condition duplication | Phase 1: Market Foundation | Load test: 100 concurrent trades, verify no dupes |
| Incomplete rollback | Phase 1: Market Foundation | Fault injection: kill DB mid-transaction, verify rollback |
| Missing idempotency | Phase 1: Market Foundation | Network simulation: replay requests, verify single charge |
| Guild permission escalation | Phase 2: Guild Foundation | Security test: new member tries to withdraw, should fail |
| Party kick abuse | Phase 2: Party Foundation | Test: kick during combat, should fail |
| AFK leeching | Phase 2: Party Foundation | Test: stand still in party, should get 0 XP |
| Rebirth stat exploit | Phase 3: Rebirth System | Test: fail rebirth mid-process, verify no stat gain |
| Market manipulation | Phase 1: Market Foundation | Monitor: flag players with >30% market share |
| Listing fee exploit | Phase 1: Market Foundation | Test: cancel listing, verify fee not refunded |
| Chat injection | Phase 2: Guild Foundation | Pen test: send command in chat, verify no execution |

## Current Project Vulnerabilities

Based on codebase analysis (`EconomyManager.cs`), the project already has:
- ✅ Transaction idempotency (operationId tracking)
- ✅ Rate limiting
- ✅ Ledger-based audit trail with hash chaining
- ✅ Atomic transaction support
- ✅ Compensating transactions (refund on failure)

**Still needs for multiplayer systems:**
- ❌ Party system XP contribution tracking
- ❌ Guild permission time gates
- ❌ Market share limits & wash trading detection
- ❌ Party kick combat protection
- ❌ Rebirth transaction atomicity
- ❌ P2P trade anti-duplication (extend EconomyManager pattern)

**Existing security gaps to address:**
- Weak movement validation (mentioned in project context) could enable position-based exploits for party XP sharing
- No anti-replay protection (mentioned) - current `operationId` system in `EconomyManager` should be extended to ALL multiplayer operations
- PostgreSQL migration should use `IsolationLevel.Serializable` for all multiplayer transactions

## Sources

### Real-World MMORPG Disasters
- [MapleStory Europe Economy Collapse (2011)](https://www.engadget.com/2011-02-01-maplestory-europes-economy-collapses-due-to-currency-exploit.html)
- [New World Duplication Exploit Forces Trading Disabled](https://www.pcgamer.com/amazon-disables-new-world-wealth-transfers-to-fight-gold-dupe-exploit/)
- [New World Turns Off Economy Over Duplication Exploit](https://comicbook.com/gaming/news/amazon-new-world-turns-off-economy-trading-duplication-exploit/)
- [MapleStory Bans Hundreds Over Exploit](https://mmofallout.com/2025/06/17/maplestory-bans-a-few-hundred-accounts-over-exploit/)
- [Lost Ark Founder's Pack Duplication](https://www.dexerto.com/lost-ark/lost-ark-devs-confirm-punishment-for-duplicate-founders-packs-exploiters-1760715/)
- [Albion Online Guild Theft](https://www.mmorpg.com/news/1-billion-worth-of-in-game-assets-stolen-from-albion-online-guild-by-player-2000119386)
- [Diablo IV Trading Disabled Due to Dupe Exploit](https://www.mmorpg.com/news/diablo-iv-disables-player-trading-thanks-to-gold-and-item-dupe-exploit-2000128708)

### Guild & Party Systems
- [Turtle WoW Guild Bank Theft Discussion](https://forum.turtle-wow.org/viewtopic.php?t=15192)
- [Neverwinter Vote-to-Kick Abuse Fix](https://www.mmorpg.com/news/new-vote-to-kick-features-to-be-added-to-combat-abuse-2000087119)
- [WoW Ninja Looting - Wowpedia](https://wowpedia.fandom.com/wiki/Loot_ninja)
- [Black Desert Online Guild Storage Permissions](https://www.naeu.playblackdesert.com/en-US/Forum/ForumTopic/Detail?_topicNo=149&_opinionNo=571)

### Technical Prevention
- [Race Condition Vulnerability Explained - Snyk](https://learn.snyk.io/lesson/race-condition/)
- [Database Race Conditions - Doyensec](https://blog.doyensec.com/2024/07/11/database-race-conditions.html)
- [Item Duplication Exploits and Prevention](https://munique.net/item-duplication-exploits/)
- [Duping in Video Games - Wikipedia](https://en.wikipedia.org/wiki/Duping_(video_games))

### Economy & Market Manipulation
- [MMO Economy Manipulation - Game Developer](https://www.gamedeveloper.com/production/mmo-economy-manipulation-)
- [Virtual Economic Theory: How MMOs Really Work](https://www.gamedeveloper.com/business/virtual-economic-theory-how-mmos-really-work)
- [MMORPG Inflation Discussion - Ask a Game Dev](https://askagamedev.tumblr.com/post/757353839637790720/mmorpg-ingame-economies-have-inflation-thats)

---
*Pitfalls research for: The Wonderland Legacy (TWL) Multiplayer Systems*
*Researched: 2026-02-14*
*Applies to: P2P Market, Party, Guild, and Rebirth system implementation*

# Architectural Blueprint and Systems Mechanics Documentation for a Wonderland Online Mobile Framework

## Source Extracted: 2026-03-06

This document contains explicit mechanical constraints and mathematical logic extracted from deep research documentation regarding Wonderland Online. It serves as a single source of truth for implementing accurate mechanics in the MonoGame/C# architecture.

### 1. Core Combat & State Machine
- **Determinism**: The combat engine is a 2-phase turn-based loop (Command Phase, Action Phase).
- **Priority Queue**: Action Phase execution order is strictly determined by descending Speed attribute at the exact start of the phase.
- **Overkill Rule**: In a single hit, damage exceeding a specific max HP threshold immediately forcibly respawns players or removes pets from the instance without further turn participation.

### 2. Elemental Matrix
- **Types**: Fire, Water, Earth, Wind.
- **Cycle**: Fire > Wind > Earth > Water > Fire.
- **Logic**: Element determines base stat multipliers, available skill trees, and combat multipliers. Elemental buffs stack multiplicatively. Damage calculation happens in Action Phase using a 4x4 resolution matrix.

### 3. "Bursting" (Exponential XP) & Automation Edge Cases
- **Mechanic**: Achieving massive overkill damage to yield exponential XP that bypasses linear grinding constraints.
- **Trigger Condition**: Target must have an active DEF debuff state (e.g., specific skills that truncate mitigation attributes).
- **Execution**: Three high-damage element characters (e.g., Fire) deal synchronized max damage in the same Action Phase queue. Overkill damage is mathematically pooled into an accumulator. 
- **Party Composition Math**: Uses four Level 1 pets to keep the `PartyTotalLevel` average artificially low while maximizing the `PartyBonusMultiplier` (8 actors present). These pets *must* have lower Speed than players so they act last and don't break the overkill queue sequence.
- **Party Disband / Automation Fallback**: If the party leader disconnects, the server must automatically execute a "party disbandment protocol". Additionally, if a client's automated combat routine (bot) exhausts resource reserves (SP), it forces a basic physical attack fallback, which inherently breaks the Action Phase overkill chain.

### 4. Entity Bifurcation (Pets vs Mobs)
- **Creature Pets (Mobs)**: Can be captured. Can have the `MountableComponent`. When equipped with Saddles (e.g., Swift Saddle), they pass a precise percentage (e.g., 33.3%) of their specific attribute to the rider. Cannot participate in battle while mounted (mutual exclusivity).
- **Human Pets**: Acquired via narrative quests. Cannot be mounted. Superior stats. Exclusive access to the Rebirth progression system.

### 5. Pet Stats & Amity Penalties
- **Level Up RNG**: Pet stat points are randomly clustered—a single point goes to a random index among the pet's *top three highest existing attributes*.
- **Washing**: Players must use "Lethe Scrolls" (only usable if pet >= Level 20) to manually decrement and reallocate bad stat rolls.
- **Amity Penalty**: Desertion occurs instantly if Amity <= 20, permanently deleting the pet AND all equipped items (including premium cash shop gear) from the database. Trade causes an immediate -10 Amity. "Friendship Brooch" halts Amity drain, but operates *only* if Amity is exactly 100.

### 6. Progression Systems (Evolution vs Rebirth)
- **Creature Evolution**: Uses tier-specific Evolution Stones (mapped to the 4 base elements) + requires all skill points allocated on the pet. 100% success rate to advance to next stage (Base -> Stage 2 -> Stage 3 -> Stage 4).
  - *Future Consideration*: The system can theoretically support non-standard elements (e.g., Light/Dark or Sun/Moon) in future expansions, but the core V1 relies strictly on the 4 base elements.
- **Human Pet Rebirth**: Gated by a high level requirement (Lv 100). Multi-step narrative sequence: 
  - 1. A mandatory "Death Quest" where the companion is mechanically killed and removed from the active party.
  - 2. A restoration phase (trading premium or rare currency to an NPC) to restore the physical body.
  - 3. A final soul-merging phase in a hidden location.
  - *Reward*: Unlocks a highly powerful, character-specific "Signature Skill" and significantly boosts late-game stat scaling.
- **Character Rebirth**: High-level gate (Lv 100). Requires surviving a grueling multi-round boss gauntlet. 
  - *Boss AI Scripts & Anti-Exploit Mechanics*: Boss AI should be highly deterministic rather than purely random. Endgame bosses actively monitor their own resource (SP) pools; if a player attempts to use an SP-drain item to bypass mechanics, an override function triggers unleashing a geometrically scaled squad-wipe attack.
- **Jobs / Specializations**: After the Rebirth gauntlet, the system checks primary stats (e.g., STR vs INT). A physical bias validates unlocking Fighter archetypes; a magical bias unlocks Mage archetypes. Grants specific stat-scaling artifacts equivalent to Job Capes.
  - *Stat Min-Maxing*: The stat validation is a one-time check at the exact moment of Rebirth, allowing players to manipulate stats (e.g., building Speed, keeping STR just 1 point over INT to get a Fighter class, but playing as a Mage post-rebirth).
  - *Hidden Passives*: Certain classes have mathematically hidden defensive mitigation (e.g., 30% reduction scaling on Wisdom) or unique synergies with specific mount types.

### 7. Alchemy & Compounding Math
- **Item Rank Formula**: Equivalent to exactly one-half (0.5x) of the item's equippable character level.
- **Base Groups**: Items are categorized by material (Titanium, Wood, Flower, etc.).
- **Skill Levels**: Primary (+4 max rank jump, -8 drop on failure), Junior (+4 max, -8 drop), Superior (+5 max rank jump, -7 drop on failure).
- **Alchemy Books 1-4**: Artificially reduce the Effective Rank Jump target. *Effective Rank = Target Rank Jump - Book Value*. Achiving exactly 0 Effective Rank jump guarantees 100% success mapping to the base tier probabilities.
- **Garbage Generation**: Catastrophic sub-minimum rank failures universally generate Common Stone or Straw Mushrooms.

### 8. Death & Degradation
- **Death**: -1% of current accumulated EXP towards the next level. Crystal of Efforts reduces this by 50% for 8 hours real-time.
- **Equipment Breakage**: Highly punitive check if `Level_Mob >= Level_Player + 15`. At 0 durability, item enters "broken" state granting zero stats until specifically repaired.