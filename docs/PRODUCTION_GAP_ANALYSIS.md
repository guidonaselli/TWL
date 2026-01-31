# Production Gap Analysis & Execution Plan

**Date**: 2024-05-22
**Auditor**: Jules
**Target**: WLO-like Server-Authoritative JRPG
**Status**: **Prototype / Vertical Slice**

## A) Executive Summary

**Current Maturity Level**: **Prototype**
The codebase implements a functional vertical slice with clear architectural separation (Client/Server/Shared). Core loops (Networking, Combat, Quests) are testable and operational. However, the system is **not production-ready** due to critical gaps in persistence, scalability, and content integrity.

**Top 5 Technical Risks**:
1.  **Data Persistence**: Reliance on `System.IO.File` (`FilePlayerRepository`) is a critical data loss risk for any concurrent load.
2.  **Content Integrity**: **7 Tests Failing** (Quest/Localization), indicating a broken content pipeline that allows invalid keys/IDs.
3.  **Dependency Injection**: `GameServer` manually instantiates services, preventing effective unit testing of the core loop.
4.  **Concurrency**: Lack of global locking strategies for complex interactions (Trade + Combat).
5.  **Map Streaming**: Full-world memory loading (`WorldTriggerService`) poses a severe RAM bottleneck.

**Top 5 Gameplay/System Risks**:
1.  **Economy Integrity**: No transactional ledger for "High Value" operations; strict "Hybrid Market" is missing.
2.  **Instance Isolation**: Dungeons are currently part of the shared world; no private copies or 5/day lockout enforcement.
3.  **Social Vacuum**: No Party or Guild systems exist, blocking all group content.
4.  **PvP Controls**: No "Opt-In" PK system with server-side handshake/cooldowns.
5.  **Quest Flow**: Critical "Starter" and "Puerto Roca" questlines are failing validation.

**Bottleneck**: **Persistence Layer Migration** (JSON -> PostgreSQL) is the single most important prerequisite for safe Economy, Social, and Instance features.

---

## B) Feature Coverage Matrix

| Domain | Status | Evidence | Production Gaps | Recommended Next Step |
| :--- | :--- | :--- | :--- | :--- |
| **Combat** | **Partial** | `CombatManager`, `StatusEngine` | - No PvP Handshake/Toggle.<br>- Pet turn consumption not enforced.<br>- No Row/Column AoE logic. | Implement `PKEnabled` toggle and `PetCombatAI`. |
| **Skills** | **Partial** | `SkillService`, `SkillRegistry` | - Upgrade rules (`StageUpgradeRules`) implemented but untested.<br>- Goddess Skills logic exists but `GrantGoddessSkills` is hardcoded. | Validate `StageUpgradeRules` with a test case. |
| **Quests** | **Partial** | `ServerQuestManager` | - **7 Tests Failing** (Missing IDs/Keys).<br>- No "Instance Wipe" failure logic.<br>- No "Time Limit" enforcement. | Fix `quests.json` definitions to pass `PuertoRocaQuestTests`. |
| **Pets** | **Feature Complete** | `PetService`, `ServerPet` | - Content Missing: Capture/Amity/Rebirth logic is code-complete, but Data/Defs are sparse. | Populate `pets.json` with actual creature data. |
| **Inventory** | **Partial** | `ServerCharacter.Inventory` | - No `BindOnEquip` / `BindOnPickup` enforcement.<br>- No Weight limit checks. | Implement `BindPolicy` check on Equip/Trade. |
| **Economy** | **Stub** | `EconomyManager` | - **Missing Hybrid Market** (Centralized Ledger + Stalls).<br>- Ledger exists but is file-based.<br>- No Anti-Dupe transactional logic. | Implement `MarketListing` schema (DB) and `MarketManager`. |
| **Social** | **Missing** | N/A | - No Party system.<br>- No Guild system.<br>- No Friends list. | Implement `PartyManager` (Leader, Members, LootShare). |
| **World** | **Partial** | `WorldTriggerService` | - **No Instance Isolation** (Shared world only).<br>- Spawns are static.<br>- No 5/day Lockout logic. | Implement `InstanceManager` to clone maps dynamically. |
| **Persistence** | **Stub** | `FilePlayerRepository` | - JSON File storage (Non-Atomic).<br>- No Transactional support. | Migrate to `PostgresPlayerRepository` (Dapper/EF). |
| **Security** | **Stub** | `RateLimiter` | - No Movement Validation (Speedhack/Teleport).<br>- No Packet Integrity checks. | Implement `MovementValidator` (Speed/Distance check). |
| **Observability**| **Partial**| `ServerMetrics` | - Basic Tick metrics exist.<br>- No structured logging (Serilog is configured but mostly unused). | Instrument `CombatManager` with structured logs. |
| **Localization** | **Partial**| `LocalizationAuditor` | - **18 Errors**: Key mismatches in Audit.<br>- Hardcoded strings in code. | Enforce `LocalizationAuditor` in CI pipeline. |

---

## C) Production Backlog (Prioritized)

### P0 - Must Have (Architecture & Safety)
1.  **Persistence Migration (PostgreSQL)**
    *   **Obj**: Replace `FilePlayerRepository` with `PostgresPlayerRepository`.
    *   **Criteria**: Player data (Skills, Quests, Items) persists to DB. Atomic transactions for Item/Gold changes.
    *   **Blast Radius**: `TWL.Server/Persistence`, `GameServer.cs`.
2.  **Content Integrity Fixes**
    *   **Obj**: Fix `quests.json` and `skills.json` to pass all 300+ tests.
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
5.  **Party & Guild System (V1)**
    *   **Obj**: Basic Team formation and Guild Roster.
    *   **Criteria**: Invite/Accept/Kick. Shared Quest Kill credit. Guild Chat & Storage.
    *   **Blast Radius**: `PartyManager`, `GuildManager` (New).
6.  **PvP Toggle & Handshake**
    *   **Obj**: Opt-in Open World PvP.
    *   **Criteria**: `PKEnabled` toggle. Combat initiation check (Both enabled). Server-side cooldowns.
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
2.  **Localization Audit**: Run `LocalizationValidationTests` to catch missing keys.
3.  **Code Format**: `dotnet format --verify-no-changes`.

**Deferred Checks (Enable Later)**:
1.  **Map Validation**: TMX Layer checks (Ground/Collision). Enable once Map Pipeline is mature.
2.  **Economy Audit**: Automated check for missing Idempotency Keys in new Economy methods.
