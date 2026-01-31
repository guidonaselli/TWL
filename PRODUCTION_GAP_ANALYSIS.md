# Production Gap Analysis (V1)

**Date**: 2024-05-22
**Status**: Proto-Alpha / Vertical Slice (Incomplete)
**SSOT Compliance**: Low

## 1. Executive Summary

The current codebase represents a **Vertical Slice prototype** with foundational systems (Networking, Basic Combat, Quests, Pets, Premium Economy) in place. It is **NOT production-ready**.

**Maturity Level**: **Proto-Alpha**
*   **Strengths**: Clean architecture (Server-Authoritative, Data-Driven), Basic Networking, initial content pipelines.
*   **Weaknesses**: Missing Critical Features (Market, Persistence, Social), weak Security enforcement (Movement, Anti-Replay), failing content validation.

**Top 5 Production Risks**:
1.  **Persistence Integrity**: Reliance on `FilePlayerRepository` (JSON) is unfit for production. DB migration is stubbed.
2.  **Market/Economy**: Completely missing Player-to-Player economy (Centralized Ledger + Stalls) and Compound system.
3.  **Security**: Lack of authoritative movement validation (Speed/Teleport hacks possible) and Packet Replay protection.
4.  **Content Quality**: 8 Failing Tests indicating broken Quest chains and Localization keys.
5.  **Social Systems**: Missing Party and Guild systems (Chat, Storage, Formation) required for multiplayer loop.

**Single Biggest Bottleneck**: **Persistence Layer**. Without a proper DB (PostgreSQL), all other systems (Market, Guilds, Inventory) cannot be reliably implemented or tested for concurrency.

---

## 2. Feature Coverage Matrix (SSOT-Aligned)

| Domain | Status | Evidence / Notes | Next Best Action |
| :--- | :--- | :--- | :--- |
| **Persistence** | **Prototype** | `FilePlayerRepository.cs` (JSON). `DbService.cs` is a stub. | **Migrate to PostgreSQL** (Atomic Trans, JSONB). |
| **Combat** | **Partial** | `CombatManager.cs` handles skills/death. **Missing**: 1% EXP Loss, 1 Durability Loss. | Implement Death Penalty hooks in `CombatManager`. |
| **Skills** | **Partial** | System exists. **Missing**: Earth/Water/Fire/Wind T1 Packs, Goddess Skills logic. | Populate Skill JSONs & Validators. |
| **Quests** | **Broken** | `ServerQuestManager` exists. **Failing Tests**: 8 tests failing (Chains, Loc). | Fix `PuertoRoca` & `HiddenCove` chains. |
| **Pets** | **Partial** | `PetService` exists. **Missing**: AI Logic, `pets.json` population, Riding. | Implement `PetCombatAI` & populate data. |
| **Economy (Premium)** | **Implemented** | `EconomyManager.cs` handles Gems/Shop (Secure/Idempotent). | Verify Ledger rotation/archival. |
| **Economy (Market)** | **Missing** | **No code found**. SSOT requires Hybrid Market (Ledger + Stalls). | Implement `MarketService` (Listings, Tax). |
| **Social** | **Missing** | No Party/Guild logic in `TWL.Server`. | Implement `PartyManager` & `GuildService`. |
| **World/Instances** | **Stub** | `InstanceService` is empty. No Lockout logic (5/day). | Implement `InstanceLockoutService` (Daily Reset). |
| **Security** | **Weak** | `RateLimiter` exists. **Missing**: Movement Validation, Anti-Replay. | Implement `MovementValidator` & Nonce check. |
| **Observability** | **Partial** | `ServerMetrics` exists. `SecurityLogger` used. | Extend Serilog to all critical paths. |

---

## 3. Contradictions & Blockers Report

### A. SSOT Mismatches (Blockers)
1.  **Instance Lockouts**:
    *   **SSOT**: "5 Runs per Day per Character, resetting at Server-Day".
    *   **Code**: `InstanceService.cs` has no tracking or limit logic.
    *   **Action**: Must implement persistent counters.

2.  **Death Penalty**:
    *   **SSOT**: "1% EXP Loss + 1 Durability Loss per equipped item".
    *   **Code**: `CombatManager.cs` invokes `OnCombatantDeath` but only notifies Quests. No penalty logic.
    *   **Action**: Implement `DeathService` to apply penalties.

3.  **Market Architecture**:
    *   **SSOT**: "Hybrid Market (Centralized Ledger + Player Stalls)".
    *   **Code**: `EconomyManager.cs` only handles Premium/Shop. No P2P market exists.
    *   **Action**: Build `MarketService` from scratch.

### B. Content Integrity (Failing Tests)
The following tests are **FAILING** and block release:
*   `TWL.Tests.Localization.LocalizationValidationTests` (Missing Keys)
*   `TWL.Tests.HiddenCoveTests` (Quest Chain Broken)
*   `TWL.Tests.QuestRuinsExpansionTests` (Quest Chain Broken)
*   `TWL.Tests.HiddenRuinsQuestTests` (Quest Chain Broken)
*   `TWL.Tests.Reliability.WorldLoopObservabilityTests` (Metrics Failure)

### C. Architecture Gaps
*   **Database**: No migrations, no ORM/SQL mapping.
*   **Protocol**: `NetMessage` has no `Sequence` or `Nonce` field, leaving the server vulnerable to Replay Attacks.

---

## 4. Conclusion
The project is in a **pre-production state**. Immediate focus must shift from "adding features" to **stabilizing the core** (Persistence, Security) and **filling critical gaps** (Market, Social) before content expansion.
