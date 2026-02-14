# Codebase Concerns

**Analysis Date:** 2026-02-14

## Tech Debt

### Console.WriteLine Debug Output in Production Code

**Issue:** 92 instances of Console.WriteLine and Debug.Print statements scattered across server and client code. These remain in production builds and clutter output.

**Files:** `TWL.Client/Presentation/Views/PlayerView.cs` (lines 229, 238, 242-244), `TWL.Server/Simulation/Managers/EconomyManager.cs` (lines 560, 569, 587, 591) and 80+ additional locations

**Impact:**
- Server logs are difficult to parse and monitor in production
- Performance degradation from excessive I/O to console
- No structured logging discipline across codebase
- Client debug output pollutes console unnecessarily

**Fix approach:** Replace all Console.WriteLine with structured logging via Serilog/ILogger. Remove debug output or gate it behind IsDevelopment checks.

---

### Unimplemented TODO/FIXME Comments

**Issue:** Feature gaps intentionally left incomplete.

**Files:**
- `TWL.Server/Services/World/Actions/Handlers/MessageActionHandler.cs:25` - TODO: Implement SystemMessage opcode
- `TWL.Server/Features/Combat/SkillTargetingHelper.cs:47` - TODO: Implement grid-based targeting
- `TWL.Client/Presentation/Views/PlayerView.cs:176` - TODO: If we want to palette swap items (e.g. dyed armor)

**Impact:**
- SystemMessage opcode incomplete - chat/messaging features may not work fully
- Grid-based skill targeting not implemented - combat targeting limited to basic logic
- Item palette swapping not implemented - visual customization unavailable

**Fix approach:** Complete grid-based targeting system, implement SystemMessage opcode, add palette swapping for items.

---

### Hardcoded Asset Paths and Configuration

**Issue:** Hardcoded paths and magic values throughout codebase instead of using configuration.

**Files:** `TWL.Client/Presentation/Views/PlayerView.cs:83` - hardcoded "Sprites/Characters/RegularMale/Base/Idle" path, `TWL.Server/Simulation/Managers/EconomyManager.cs:35-49` - mock shop items defined inline

**Impact:**
- Client supports only one character model (vertical slice limitation)
- Configuration changes require code recompilation
- Mock economy data embedded in code

**Fix approach:** Move asset paths and shop configurations to external config files, add support for multiple character models.

---

## Known Bugs

### Unchecked Exception in SkillTargetingHelper

**Issue:** Grid-based targeting logic incomplete, may silently fail.

**Files:** `TWL.Server/Features/Combat/SkillTargetingHelper.cs:47`

**Symptoms:** Skills with grid-based targeting fall through to unimplemented code path

**Trigger:** Any skill using grid-based targeting system (WatersplashSkill, similar AOE skills)

**Workaround:** Use basic single-target or radius-based targeting only

---

### Empty Catch Blocks Hiding Errors

**Issue:** Multiple empty catch blocks in scheduler and worker threads that swallow exceptions silently.

**Files:**
- `TWL.Server/Services/WorldScheduler.cs:55-57` - Empty AggregateException handler
- `TWL.Server/Simulation/ServerWorker.cs:149` - Empty TaskCanceledException handler

**Symptoms:** Scheduler/world loop stops processing silently, no diagnostic information

**Trigger:** WorldScheduler or ServerWorker encounters cancellation or aggregate exceptions

**Workaround:** Monitor server logs for missing ProcessTick calls

---

## Security Considerations

### Economy Manager - String-Based Ledger Parsing

**Risk:** Regular expression-based transaction log parsing is fragile and could be exploited via malformed entries.

**Files:** `TWL.Server/Simulation/Managers/EconomyManager.cs:20-22, 595-649`

**Current mitigation:** Regex validation exists, file-based storage, basic integrity check via hash chain

**Recommendations:**
- Migrate to binary ledger format (MessagePack or similar) instead of regex parsing
- Add cryptographic signatures to ledger entries, not just hash chain
- Implement transaction tampering detection tests
- Add rate limiting per transaction type to prevent ledger spam

---

### Ledger Integrity Check Is One-Way

**Risk:** Ledger integrity check verifies tampering but cannot recover from it.

**Files:** `TWL.Server/Simulation/Managers/EconomyManager.cs:735-780`

**Current mitigation:** Throws SecurityException when tamper detected, but no recovery path

**Recommendations:**
- Store signed snapshots periodically to recover from corruption
- Implement transaction rollback to last verified snapshot
- Add audit trail with player IP/session info

---

### Mock Economy Data in Code

**Risk:** Mock shop items and pricing logic uses hardcoded data in EconomyManager constructor - no validation against actual item definitions.

**Files:** `TWL.Server/Simulation/Managers/EconomyManager.cs:35-49`

**Current mitigation:** Mock data only (not production), basic ItemId references

**Recommendations:**
- Validate shop items against ItemCatalog at initialization
- Load shop inventory from configuration file, not code
- Add tests for shop item references validity

---

## Performance Bottlenecks

### PlayerQuestComponent - 1514 Lines, Multiple Lock Contention Points

**Problem:** Largest server-side class combines quest state, progress tracking, and world flags in single monolithic component with multiple lock statements.

**Files:** `TWL.Server/Simulation/Networking/Components/PlayerQuestComponent.cs` (1514 lines)

**Cause:**
- Quest state, progress lists, and flags all updated via PlayerQuestComponent
- Each operation locks entire component
- No lock-free patterns or read-write separation
- OnFlagAdded event fires within lock, potentially blocking world triggers

**Improvement path:**
- Split into QuestStateComponent, ProgressTracker, FlagStore classes
- Use ConcurrentDictionary for quest states instead of manual locking
- Make OnFlagAdded fire outside of lock
- Implement copy-on-write for progress lists

---

### WorldScheduler - Spiral of Death Recovery Drops Ticks

**Problem:** When world loop falls behind, ticks are silently discarded instead of recovered gradually.

**Files:** `TWL.Server/Services/WorldScheduler.cs:145-153`

**Cause:** MaxTicksPerFrame = 10 limit means if frame takes >500ms, remaining ticks are dropped to prevent loop starvation

**Impact:** AI decisions, spawn events, quest triggers may be missed if server stutters

**Improvement path:**
- Implement tick buffering instead of dropping
- Prioritize critical ticks (NPC decisions, quest events) over non-critical (stats updates)
- Add telemetry for dropped tick events
- Reduce TickRateMs from 50 (20 TPS) if load is predictable

---

### EconomyManager - Regex String Parsing on Every Transaction Replay

**Problem:** Every time server starts, entire ledger file is replayed line-by-line with regex matching for initialization.

**Files:** `TWL.Server/Simulation/Managers/EconomyManager.cs:595-649`

**Cause:** Ledger stored as CSV-like strings, requires regex parse on each load

**Impact:** Server startup scales linearly with ledger size. 100K transactions = significant startup delay.

**Improvement path:**
- Binary ledger format with snapshot mechanism
- Lazy-load only recent transactions
- Compress archived ledger segments
- Cache parsed ledger in memory after first load

---

### Parallel.ForEachAsync with MaxDegreeOfParallelism=20 in PlayerService Flush

**Problem:** Hard-coded max 20 concurrent saves during persistence flush. May cause thread pool starvation or underutilization.

**Files:** `TWL.Server/Persistence/Services/PlayerService.cs:115`

**Cause:** Fixed parallelism limit not tuned to system

**Impact:** Large player counts (500+ concurrent) may not utilize all cores; small servers waste resources

**Improvement path:**
- Calculate MaxDegreeOfParallelism based on Environment.ProcessorCount
- Monitor flush duration and adjust dynamically
- Use adaptive batching (smaller batches if flush exceeds time budget)

---

## Fragile Areas

### ClientSession - 894 Lines, Handles 15+ Responsibilities

**Files:** `TWL.Server/Simulation/Networking/ClientSession.cs` (894 lines)

**Why fragile:**
- Manages TCP stream, JSON parsing, command dispatching, combat, quests, interactions, economy, world triggers
- Large number of dependencies injected (TcpClient, DbService, CombatManager, InteractionManager, PlayerService, IEconomyService, ServerMetrics, PetService, IMediator, IWorldTriggerService, SpawnManager)
- JsonException caught in multiple places but error handling inconsistent
- Null checks scattered throughout (Character, Stream state, etc.)

**Safe modification:**
- Extract payment/economy operations to separate EconomySessionHandler
- Extract combat operations to CombatSessionHandler
- Extract quest/interaction operations to QuestInteractionSessionHandler
- Keep only core message routing in ClientSession

**Test coverage:** Minimal - only 1 test file exists for sessions, no concurrency tests for socket operations

---

### EconomyManager - 933 Lines, Mixes Concerns

**Files:** `TWL.Server/Simulation/Managers/EconomyManager.cs` (933 lines)

**Why fragile:**
- Combines transaction ledger, snapshot persistence, integrity verification, rate limiting, and purchase processing
- Multiple background tasks (logging via Channel)
- Complex state machine for transactions (OrderId tracking, expiration)
- Regex parsing fragile to ledger format changes
- Embedded mock data (shop prices, items)

**Safe modification:**
- Extract rate limiter to separate RateLimitManager
- Extract ledger persistence to LedgerStorage class
- Extract snapshot logic to SnapshotManager
- Keep only transaction processing in EconomyManager

**Test coverage:** Basic transaction tests exist but no ledger corruption tests, no concurrent purchase tests, no rate limit evasion tests

---

### ServerCharacter - 1171 Lines, Multiple Concurrent Collections

**Files:** `TWL.Server/Simulation/Networking/ServerCharacter.cs` (1171 lines)

**Why fragile:**
- Manages inventory, equipment, bank, pets, quest progress, world flags
- Uses manual locks (_progressLock, _orderLock) mixed with collections (_inventory List, _pets List)
- IsDirty property override complex, depends on child dirtyness
- GetSaveData serializes entire object graph, potential for forgetting new fields

**Safe modification:**
- Never add new state without also updating GetSaveData and IsDirty logic
- Use ConcurrentBag or ConcurrentQueue for inventory/equipment instead of manual locks
- Add unit tests for IsDirty state transitions

**Test coverage:** Limited coverage of inventory operations, no concurrency tests for simultaneous item moves

---

### SkillTargetingHelper - Grid Targeting Incomplete

**Files:** `TWL.Server/Features/Combat/SkillTargetingHelper.cs`

**Why fragile:**
- Targeting logic incomplete, will throw NotImplementedException if grid-based targeting used
- No fallback behavior defined
- Tests exist but don't cover grid scenario

**Safe modification:**
- Implement grid-based targeting or remove from public API
- Add unit tests for all targeting types before adding new skill types
- Add unit tests that verify SkillTargetingHelper throws for unsupported targeting types

---

## Scaling Limits

### In-Memory Quest Manager Dictionary

**Current capacity:** Loaded at startup from all .json files in Content/Quests

**Limit:** Linear memory growth with quest count (approx. 1KB per quest definition)

**Scaling path:**
- With 10,000 quests: ~10MB memory (acceptable)
- With 100,000+ quests: May exceed 100MB+ for definitions alone
- Lazy-load quests by zone or region
- Cache only active player quests in memory
- Archive old quest definitions to database

---

### Ledger File Linear Growth

**Current capacity:** Single economy_ledger.log file grows indefinitely

**Limit:**
- Read entire ledger on server startup
- With 1M transactions: startup could take several seconds
- File I/O becomes bottleneck

**Scaling path:**
- Implement ledger rotation every N transactions or daily
- Archive old ledgers to separate storage
- Keep only recent 100K transactions in hot storage
- Implement binary format instead of CSV regex parsing

---

### ConcurrentDictionary for Combatants and Encounters

**Current capacity:** Unbound concurrent dictionaries for active combatants and encounter mappings

**Limit:** With 1000+ concurrent combats, dictionary lookups remain O(1) but memory grows linearly

**Scaling path:**
- Implement encounter cleanup - remove completed encounters after delay
- Add telemetry for maximum concurrent dictionaries reached
- Consider sharding encounters by zone if single-server deployment

---

## Dependencies at Risk

### LiteNetLib v1.3.5 (Outdated)

**Risk:** Last release May 2020, project appears unmaintained. Custom TCP-based networking in ClientSession may have security gaps.

**Impact:** No bug fixes or security patches for networking library

**Migration plan:**
- Evaluate gRPC or WebSockets for cross-platform compatibility
- Or switch to more maintained networking library (Netcode.IO.NET, Mirror)
- Monitor for critical CVEs in LiteNetLib

---

### MessagePack v3.1.4 (Compatibility Risk)

**Risk:** Version 3.x has breaking changes from 2.x. No tests verify serialization compatibility.

**Impact:** Player save data deserialization could fail if schema changes

**Migration plan:**
- Add versioning to MessagePack schema
- Test serialization round-trip for player save data
- Add migration code for schema upgrades

---

### MonoGame Framework v3.8.4.1 (Client Technology)

**Risk:** MonoGame is community-maintained. Official XNA support has been deprecated for 10+ years.

**Impact:** Limited platform support, smaller ecosystem compared to modern game engines

**Migration plan:**
- Evaluate if long-term client roadmap requires engine migration
- Current implementation is sufficient for vertical slice

---

## Missing Critical Features

### SystemMessage Opcode Implementation

**Problem:** Chat/system messages not implemented. Players cannot communicate in-game.

**Blocks:**
- Social gameplay features
- Admin announcements
- Server maintenance notifications

---

### Grid-Based Skill Targeting System

**Problem:** Most AOE skills default to basic single-target or radius. Grid-based tactics unavailable.

**Blocks:**
- Strategic combat gameplay (positioning matters)
- Advanced skill tiers that require position-based mechanics

---

## Test Coverage Gaps

### Networking/Protocol Tests

**What's not tested:** JSON message parsing edge cases, malformed payloads, message ordering under load, reconnection scenarios

**Files:** `TWL.Server/Simulation/Networking/ClientSession.cs` (894 lines with ~5 test methods)

**Risk:** Network protocol bugs could cause data corruption or crashes

**Priority:** High - affects all client-server interaction

---

### Concurrency Tests for Quest System

**What's not tested:**
- Simultaneous quest flag updates from multiple threads
- Quest progress updates during combat
- Race conditions between flag-based triggers and quest completion

**Files:** `TWL.Server/Simulation/Networking/Components/PlayerQuestComponent.cs` (1514 lines)

**Risk:** Quest state corruption under load

**Priority:** High - quest system critical to gameplay

---

### Economy Transaction Ledger Corruption

**What's not tested:**
- Ledger corruption recovery
- Tampered ledger detection and response
- Concurrent transaction writes
- Ledger file size limits

**Files:** `TWL.Server/Simulation/Managers/EconomyManager.cs` (933 lines)

**Risk:** Economy exploits, data loss, server crashes

**Priority:** Critical - financial data at risk

---

### Server Persistence Flush Failure Scenarios

**What's not tested:**
- Failed saves during flush don't lose data
- Concurrent session removal during flush
- Partial flush recovery if service crashes mid-flush

**Files:** `TWL.Server/Persistence/Services/PlayerService.cs` (230 lines)

**Risk:** Player data loss, character progression loss

**Priority:** Critical - data integrity at stake

---

### Combat Manager Concurrency

**What's not tested:**
- Multiple encounters updating simultaneously
- Combatant death during turn resolution
- Race conditions between AutoBattleManager and manual commands

**Files:** `TWL.Server/Simulation/Managers/CombatManager.cs` (440 lines)

**Risk:** Combat state corruption, incorrect damage calculations

**Priority:** High - core gameplay system

---

### World Loop Recovery Tests

**What's not tested:**
- Behavior when scheduled tasks exceed tick budget
- Recovery from spike load (dropped ticks)
- Metrics accuracy under failure

**Files:** `TWL.Server/Services/WorldScheduler.cs` (280 lines)

**Risk:** Unfair gameplay if some players experience dropped ticks

**Priority:** Medium - fairness issue

---

*Concerns audit: 2026-02-14*
