# Production Gap Analysis & Execution Plan

**Date**: 2024-05-22
**Auditor**: Jules
**Target**: WLO-like Server-Authoritative JRPG

## A) Executive Summary

**Current Maturity Level**: **Prototype / Pre-Alpha**

**Justification**:
The codebase demonstrates a functional "Vertical Slice" architecture with a clear separation of concerns (Client/Server/Shared). Core loops (Networking, Combat, Quests) are implemented and testable. However, the system is **not production-ready**:
1.  **Persistence is File-Based**: `FilePlayerRepository` uses JSON files, which is unsuitable for concurrent production load (Data Loss/Race Condition risk).
2.  **Content Integrity is Broken**: 8/292 tests fail due to missing Quest definitions (`quests.json`) and localization mismatches.
3.  **Security is Minimal**: While `RateLimiter` exists, there is no authoritative Movement Validation (client can teleport) or Inventory Transaction Ledger (Dupe risk).
4.  **Missing Core Systems**: Market (Auction House), Party/Guilds, and Instance Isolation are not implemented.

**Top 5 Technical Risks**:
1.  **Data Persistence**: Reliance on `System.IO.File` for player saves.
2.  **Test Harness Stability**: brittle file-path resolution in tests (Fixed in this review, but indicates fragile pipeline).
3.  **Dependency Injection**: `GameServer` manually instantiates services, making unit testing complex components (like Economy) difficult without a DI container.
4.  **Concurrency**: `CombatManager` uses `ConcurrentDictionary` but complex interactions (Trade + Combat) lack global locking strategies.
5.  **Map Streaming**: All maps are loaded into memory at startup (`WorldTriggerService`). Scalability risk.

**Top 5 Gameplay/System Risks**:
1.  **Economy Integrity**: No transactional ledger for "High Value" operations (Gems/Trading).
2.  **Instance Isolation**: Current map system is shared world. No logic found to create "Copies" of maps for parties.
3.  **Pet Systems**: "Amity" and "Death" logic is not implemented in `PetService` or `CombatManager`.
4.  **Quest Flow**: Critical "Starter" and "Puerto Roca" questlines are defined but fail validation tests.
5.  **Market**: No implementation of the "Centralized Listing" system (Hybrid Market contract).

**Single Most Important Bottleneck**: **Persistence Layer Migration**. Moving to a real DB (PostgreSQL) is required to build safe Economy and Social features.

---

## B) Feature Coverage Matrix

| Domain | Status | Evidence | Production Gaps | Recommended Next Step |
| :--- | :--- | :--- | :--- | :--- |
| **Combat** | **Partial** | `CombatManager`, `StandardCombatResolver`, `StatusEngine` | - Missing complex Skill scopes (Row/Column).<br>- No Pet AI logic (Attack/Defend).<br>- No PVP handshake. | Implement `PetCombatAI` and verify Pet turns. |
| **Skills** | **Partial** | `SkillService`, `SkillRegistry` | - Upgrade rules (`StageUpgradeRules`) implemented but untested.<br>- Goddess Skills logic exists but `GrantGoddessSkills` is hardcoded. | Validate `StageUpgradeRules` with a test case. |
| **Quests** | **Partial** | `ServerQuestManager`, `PlayerQuestComponent` | - **8 Tests Failing** (Missing IDs/Keys).<br>- No "Instance" completion logic.<br>- No "Fail" conditions (Time/Death). | Fix `quests.json` definitions to pass `PuertoRocaQuestTests`. |
| **Pets** | **Stub** | `PetService`, `PetDefinition` | - No Amity system.<br>- No Death/Revive state logic.<br>- No Capture mechanic. | Implement `Amity` property and Death penalty logic. |
| **Inventory** | **Partial** | `ServerCharacter.Inventory` | - No "Bind" policy enforcement (BoP/BoE).<br>- No Weight limit checks.<br>- No Equipment stats calculation. | Implement `BindPolicy` check on Trade/Equip. |
| **Economy** | **Stub** | `EconomyManager` | - No Market/Listing system.<br>- No Trade window logic.<br>- No Anti-Dupe ledger. | Implement `MarketListing` schema and DB table. |
| **Social** | **Missing** | N/A | - No Party system.<br>- No Guild system.<br>- No Friends list. | Implement `PartyManager` (Leader, Members, LootShare). |
| **World** | **Partial** | `WorldTriggerService`, `MapLoader` | - **No Instance Isolation** (Shared world only).<br>- Spawns are static. | Implement `InstanceManager` to clone maps dynamically. |
| **Persistence** | **Stub** | `FilePlayerRepository` | - JSON File storage.<br>- No Atomic transactions. | Migrate to `PostgresPlayerRepository` (Dapper/EF). |
| **Security** | **Stub** | `RateLimiter` | - No Movement Validation.<br>- No Packet Integrity checks (Sequence/Nonce). | Implement `MovementValidator` (Speed/Distance check). |
| **Observability**| **Partial**| `ServerMetrics`, `WorldScheduler` | - Basic Tick metrics exist.<br>- No structured logging (Serilog is configured but mostly unused). | Instrument `CombatManager` with structured logs. |
| **Localization** | **Partial**| `LocalizationAuditor` | - **Test Failure**: "Into the Green" vs "El Camino...".<br>- Hardcoded strings in code. | Enforce `LocalizationAuditor` in CI pipeline. |

---

## C) Production Backlog (Prioritized)

### P0 - Must Have (Architecture & Safety)
1.  **Persistence Migration (PostgreSQL)**
    *   **Obj**: Replace `FilePlayerRepository` with `PostgresPlayerRepository`.
    *   **Criteria**: Player data (Skills, Quests, Items) persists to DB. Atomic transactions for Item/Gold changes.
    *   **Blast Radius**: `TWL.Server/Persistence`, `GameServer.cs`.
2.  **Content Integrity Fixes**
    *   **Obj**: Fix `quests.json` and `skills.json` to pass all 292 tests.
    *   **Criteria**: `dotnet test` returns 0 failures. `Puerto Roca` quests are playable.
    *   **Blast Radius**: `Content/Data/*.json`.
3.  **Secure Economy Ledger**
    *   **Obj**: Implement `EconomyTransaction` log for all Gem/Gold/Trade events.
    *   **Criteria**: Every currency change generates a non-deletable audit log. Idempotency keys used.
    *   **Blast Radius**: `EconomyManager`, `TradeManager`.

### P1 - Gameplay Contracts (Vertical Slice)
4.  **Hybrid Market System**
    *   **Obj**: Centralized Listings + Player Stalls view.
    *   **Criteria**: Players can list items. Items are removed from inventory. Buyers can search and buy. Gold/Item delivery is atomic.
    *   **Blast Radius**: `MarketManager` (New), `EconomyManager`.
5.  **Instance Isolation**
    *   **Obj**: Party-specific Dungeon instances.
    *   **Criteria**: Entering a "Dungeon" portal creates a new `InstanceId`. Only Party members see each other. Monsters are local to instance.
    *   **Blast Radius**: `WorldTriggerService`, `GameServer` (Map Registry).
6.  **Pet Lifecycle (Death & Amity)**
    *   **Obj**: Soft Death + Amity Penalty.
    *   **Criteria**: Pet HP < 0 -> KO. End Combat KO -> Amity -1. 1.5x MaxHP Dmg -> Despawn.
    *   **Blast Radius**: `CombatManager`, `PetService`, `ServerPet`.
7.  **Party System**
    *   **Obj**: Basic Team formation.
    *   **Criteria**: Invite/Accept/Kick. Shared Quest Kill credit. Shared Instance entry.
    *   **Blast Radius**: `PartyManager` (New), `ClientSession`.

### P2 - Depth & Polish
8.  **PvP & Anti-Cheat**
    *   **Obj**: Opt-in Open World PvP.
    *   **Criteria**: `PKEnabled` toggle. Combat initiation check. Movement Validation.
    *   **Blast Radius**: `CombatManager`, `MovementHandler`.
9.  **Skill Mastery & Unlocks**
    *   **Obj**: Usage-based leveling.
    *   **Criteria**: Skill use -> XP gain. Rank up -> Stat increase.
    *   **Blast Radius**: `CombatManager`, `SkillService`.

---

## D) Validation & CI Recommendations

**Immediate CI Gates (Enable Now)**:
1.  **Content Validation**: Run `TWL.Tests.ContentValidationTests` on every PR. (Currently failing).
2.  **Path Resolution**: Ensure CI environment copies `Content/Data` to build output (Verified fix in `TWL.Tests.csproj`).
3.  **Code Format**: `dotnet format --verify-no-changes`.

**Deferred Checks (Enable Later)**:
1.  **Map Validation**: TMX Layer checks (Ground/Collision). Enable once Map Pipeline is mature.
2.  **Economy Audit**: Automated check for missing Idempotency Keys in new Economy methods.

**Missing Validators**:
-   `QuestGraphValidator`: Check for circular dependencies or unreachable quests.
-   `LootTableValidator`: Verify Drop Rates sum to 1.0 (or intended behavior) and ItemIds exist.
