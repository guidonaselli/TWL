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
