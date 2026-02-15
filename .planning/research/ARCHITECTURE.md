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
