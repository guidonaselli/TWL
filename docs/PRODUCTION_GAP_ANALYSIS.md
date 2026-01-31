# Production Gap Analysis & Execution Plan

**Date**: 2024-05-22
**Auditor**: Jules
**Target**: WLO-like Server-Authoritative JRPG

## A) Executive Summary

**Current Maturity Level**: **Prototype / Vertical Slice**

**Justification**:
The codebase currently implements a functional "Vertical Slice" with clear architectural separation (Client/Server/Shared). Core loops such as Networking, Combat, and Quest systems are testable and operational. However, the system is **not production-ready** due to significant gaps in persistence, scalability, and content integrity:
1.  **Persistence Risk**: `FilePlayerRepository` relies on local JSON files, which is unsuitable for concurrent production loads (Data Loss/Race Condition risk).
2.  **Content Integrity**: **8/292 tests fail**, specifically in Quest definitions and Localization, indicating a broken content pipeline.
3.  **Missing Critical Systems**:
    *   **Economy**: No Market/Auction House exists (only a basic Ledger).
    *   **World**: No Instance Isolation (Dungeons are shared world).
    *   **Social**: No Party or Guild systems.
4.  **Security**: While rate-limiting exists, authoritative Movement Validation and strict Inventory Transaction Ledgers are missing.

**Top 5 Technical Risks**:
1.  **Data Persistence**: Reliance on `System.IO.File` for player saves.
2.  **Test Harness Stability**: Brittle file-path resolution in tests (e.g., `Content/Data` copying).
3.  **Dependency Injection**: `GameServer` manually instantiates services, hampering unit testing of complex components.
4.  **Concurrency**: Lack of global locking strategies for complex interactions (Trade + Combat).
5.  **Map Streaming**: Full-world memory loading (`WorldTriggerService`) poses a scalability limit.

**Top 5 Gameplay/System Risks**:
1.  **Economy Integrity**: No transactional ledger for "High Value" operations (Gems/Trading).
2.  **Instance Isolation**: Lack of private dungeon instances (5 runs/day contract cannot be enforced yet).
3.  **Market Architecture**: The "Hybrid Market" (Centralized Ledger + Player Stalls) is unimplemented.
4.  **Quest Flow**: Critical "Starter" and "Puerto Roca" questlines are failing validation.
5.  **PvP Controls**: No "Opt-In" PK system with server-side handshake.

**Single Most Important Bottleneck**: **Persistence Layer Migration**. Moving to a relational database (PostgreSQL) is a prerequisite for safe Economy, Social, and Instance features.

---

## B) Feature Coverage Matrix

| Domain | Status | Evidence | Production Gaps | Recommended Next Step |
| :--- | :--- | :--- | :--- | :--- |
| **Combat** | **Partial** | `CombatManager`, `StandardCombatResolver`, `StatusEngine` | - Missing complex Skill scopes (Row/Column).<br>- No PvP Handshake/Toggle.<br>- No Pet AI logic (Attack/Defend). | Implement `PKEnabled` toggle and `PetCombatAI`. |
| **Skills** | **Partial** | `SkillService`, `SkillRegistry` | - Upgrade rules (`StageUpgradeRules`) implemented but untested.<br>- Goddess Skills logic exists but `GrantGoddessSkills` is hardcoded. | Validate `StageUpgradeRules` with a test case. |
| **Quests** | **Partial** | `ServerQuestManager`, `PlayerQuestComponent` | - **8 Tests Failing** (Missing IDs/Keys).<br>- No "Instance Wipe" failure logic.<br>- No "Time Limit" enforcement. | Fix `quests.json` definitions to pass `PuertoRocaQuestTests`. |
| **Pets** | **Feature Complete** | `PetService`, `ServerPet` | - Content Missing: Capture/Amity/Rebirth logic is code-complete, but Data/Defs are sparse. | Populate `pets.json` with actual creature data. |
| **Inventory** | **Partial** | `ServerCharacter.Inventory` | - No "Bind" policy enforcement (BoP/BoE) on Equip.<br>- No Weight limit checks.<br>- No Equipment stats calculation. | Implement `BindPolicy` check on Equip/Trade. |
| **Economy** | **Stub** | `EconomyManager` | - **Missing Hybrid Market** (Centralized Ledger + Stalls).<br>- Ledger exists but is file-based.<br>- No Anti-Dupe transactional logic. | Implement `MarketListing` schema (DB) and `MarketManager`. |
| **Social** | **Missing** | N/A | - No Party system.<br>- No Guild system.<br>- No Friends list. | Implement `PartyManager` (Leader, Members, LootShare). |
| **World** | **Partial** | `WorldTriggerService`, `MapLoader` | - **No Instance Isolation** (Shared world only).<br>- Spawns are static.<br>- No 5/day Lockout logic. | Implement `InstanceManager` to clone maps dynamically. |
| **Persistence** | **Stub** | `FilePlayerRepository` | - JSON File storage.<br>- No Atomic transactions. | Migrate to `PostgresPlayerRepository` (Dapper/EF). |
| **Security** | **Stub** | `RateLimiter` | - No Movement Validation (Speedhack/Teleport).<br>- No Packet Integrity checks. | Implement `MovementValidator` (Speed/Distance check). |
| **Observability**| **Partial**| `ServerMetrics`, `WorldScheduler` | - Basic Tick metrics exist.<br>- No structured logging (Serilog is configured but mostly unused). | Instrument `CombatManager` with structured logs. |
| **Localization** | **Partial**| `LocalizationAuditor` | - **Test Failure**: Key mismatches.<br>- Hardcoded strings in code. | Enforce `LocalizationAuditor` in CI pipeline. |

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
3.  **Hybrid Market System**
    *   **Obj**: Centralized Listings (DB) + Player Stalls (View).
    *   **Criteria**: Players can list items. Items are removed from inventory. Buyers can search and buy. Stalls view valid DB listings.
    *   **Blast Radius**: `MarketManager` (New), `EconomyManager`.

### P1 - Gameplay Contracts (Vertical Slice)
4.  **Instance Isolation & Lockouts**
    *   **Obj**: Party-specific Dungeon instances with Daily Limits.
    *   **Criteria**: Entering a "Dungeon" portal creates a new `InstanceId`. 5 Runs/Day limit enforced (Server Day).
    *   **Blast Radius**: `WorldTriggerService`, `GameServer` (Map Registry).
5.  **Party System**
    *   **Obj**: Basic Team formation.
    *   **Criteria**: Invite/Accept/Kick. Shared Quest Kill credit. Shared Instance entry.
    *   **Blast Radius**: `PartyManager` (New), `ClientSession`.
6.  **PvP Toggle & Handshake**
    *   **Obj**: Opt-in Open World PvP.
    *   **Criteria**: `PKEnabled` toggle. Combat initiation check. Server-side cooldowns.
    *   **Blast Radius**: `CombatManager`, `ServerCombatant`.

### P2 - Depth & Polish
7.  **Pet Data Population**
    *   **Obj**: Fill `pets.json` with capturable mobs.
    *   **Criteria**: 20+ Catchable pets with stats, elements, and skills.
    *   **Blast Radius**: `Content/Data/pets.json`.
8.  **Quest Failure Conditions**
    *   **Obj**: Enforce Time Limits and NPC Death.
    *   **Criteria**: Timer expiry fails quest. Escort target death fails quest. Restart logic.
    *   **Blast Radius**: `ServerQuestManager`, `PlayerQuestComponent`.

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
