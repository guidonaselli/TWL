# Production V1 Backlog

**Priority Definition**:
*   **P0 (Blocker)**: Server cannot launch safely or reliably without this.
*   **P1 (Critical)**: Feature required for the "WLO-like" loop defined in SSOT.
*   **P2 (Required)**: Required for V1 scope but can be developed in parallel with content.

---

## 1. P0 - Infrastructure & Security (The "Must Haves")

### [INFRA-001] Persistence Layer Migration
*   **Objective**: Replace `FilePlayerRepository` (JSON) with PostgreSQL to ensure data integrity and atomicity.
*   **Acceptance Criteria**:
    *   `DbService` connects to a real PostgreSQL instance.
    *   Player Data (Stats, Items, Quests, Skills) is saved/loaded from DB.
    *   Save operations use transactions (Atomic Commit).
    *   `FilePlayerRepository` is removed or kept only as a dev fallback.
*   **Dependencies**: `docs/core/ROADMAP.md` (PERS-001)

### [SEC-001] Authoritative Movement Validation
*   **Objective**: Prevent speed-hacks and teleportation.
*   **Acceptance Criteria**:
    *   Server calculates distance between last known pos and new pos.
    *   Reject `MoveRequest` if distance > `MaxSpeed * DeltaTime + Buffer`.
    *   Log security event on violation.
*   **Dependencies**: `docs/core/ROADMAP.md` (CORE-003)

### [SEC-002] Packet Replay Protection
*   **Objective**: Prevent attackers from re-sending valid packets (e.g., "Buy Item") to duplicate effects.
*   **Acceptance Criteria**:
    *   `NetMessage` includes a `Sequence` or `Nonce`.
    *   Server tracks last processed sequence per client.
    *   Reject packets with `Seq <= LastSeq`.
*   **Dependencies**: `docs/core/ROADMAP.md` (CORE-002)

### [QUAL-001] Fix Content Validation Tests
*   **Objective**: Ensure the build is green and content is valid.
*   **Acceptance Criteria**:
    *   `LocalizationValidationTests` pass (All keys exist).
    *   `HiddenCoveTests`, `QuestRuinsExpansionTests`, `HiddenRuinsQuestTests` pass.
    *   Broken quest chains are fixed in JSON/Logic.
*   **Dependencies**: `docs/quests/ROADMAP.md`

---

## 2. P1 - Core Gameplay Systems

### [ECO-001] Hybrid Market System
*   **Objective**: Implement Player-to-Player trading via Centralized Ledger.
*   **Acceptance Criteria**:
    *   `MarketService` allows `PostListing(itemId, price, qty)`.
    *   `MarketService` allows `BuyListing(listingId)`.
    *   Currency is transferred atomically (Buyer -> Seller - Tax).
    *   Items are transferred atomically (Seller -> Buyer).
    *   Listing expiration logic.
*   **Dependencies**: `docs/rules/GAMEPLAY_CONTRACTS.md` (Section 1)

### [SOC-001] Party System
*   **Objective**: Allow players to group up for shared XP and Loot.
*   **Acceptance Criteria**:
    *   `PartyService` handles Invite/Join/Leave/Kick.
    *   Max 4 players (or SSOT defined limit).
    *   Shared XP distribution logic in `CombatManager`.
*   **Dependencies**: `docs/core/ROADMAP.md` (SOC-001)

### [SOC-002] Guild System (V1 Scope)
*   **Objective**: Social communities with chat and storage.
*   **Acceptance Criteria**:
    *   Create/Join/Leave Guild.
    *   Guild Chat Channel.
    *   Guild Storage (Deposit/Withdraw items).
    *   **Exclude**: Housing/Territory (V1 Scope).
*   **Dependencies**: `docs/rules/GAMEPLAY_CONTRACTS.md` (Section 8)

### [COMB-001] Death Penalty Implementation
*   **Objective**: Enforce consequences for failure.
*   **Acceptance Criteria**:
    *   On Death: Deduct 1% of current level EXP (Floor 0%).
    *   On Death: Deduct 1 Durability from all equipped items.
    *   If Durability == 0, item stats are disabled (`Broken` state).
*   **Dependencies**: `docs/rules/GAMEPLAY_CONTRACTS.md` (Section 3)

### [INST-001] Instance Lockouts
*   **Objective**: Limit dungeon runs to 5 per day.
*   **Acceptance Criteria**:
    *   `InstanceService` tracks runs per Character per Day.
    *   Reset runs at Server Midnight (00:00 UTC).
    *   Reject entry if Limit >= 5.
*   **Dependencies**: `docs/rules/GAMEPLAY_CONTRACTS.md` (Section 2)

---

## 3. P2 - Content & Polish

### [SKL-001] Skill Packs T1 (Elements)
*   **Objective**: Implement full T1 skill trees for all elements.
*   **Acceptance Criteria**:
    *   Earth, Water, Fire, Wind T1 packs implemented in JSON.
    *   Validated against `SkillService`.
*   **Dependencies**: `docs/skills/ROADMAP.md`

### [PET-001] Pet AI & Data Population
*   **Objective**: Make pets functional in combat.
*   **Acceptance Criteria**:
    *   `pets.json` populated with 20+ starter mobs.
    *   `PetCombatAI` selects skills intelligently (not just random).
    *   Amity loss on KO (-1).
*   **Dependencies**: `docs/pets/ROADMAP.md`
