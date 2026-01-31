# Gameplay Contracts & System Rules (SSOT)

**Status**: FINAL (Production V1)
**Last Updated**: 2024-05-22

This document serves as the Single Source of Truth (SSOT) for gameplay logic and system architecture constraints. All implementation must adhere to these rules. Deviations require explicit approval.

---

## 1. Market Architecture
**Type**: **Hybrid Market** (Strict Requirement)

*   **Primary System**: A global, centralized **Listing Ledger** (Auction House style).
    *   Features: Searchable, Buyout Listings, Expiry Times, Transaction Taxes.
    *   Authority: All market transactions are settled against this single ledger.
*   **Secondary Frontend**: **Player Stalls / Tents**.
    *   Role: purely an alternative UI/Frontend for the Centralized Ledger.
    *   Constraint: Stalls **must not** create a parallel economy or separate inventory state. They simply publish listings to the global ledger and display a subset of that ledger visually in the world.

## 2. Instance Lockouts
**Model**: **Count-Based**
**Limit**: **5 Runs per Day** per Character.

*   **Reset**: Daily at a fixed Server-Day boundary (e.g., 00:00 UTC).
*   **Enforcement**: Server-Authoritative.
    *   Must be idempotent (reconnecting or retrying does not grant extra runs).
    *   Deducted upon successful entry or boss engage (TBD, but must be exploitable-proof).
*   **Future**: Time-window events may exist, but the baseline is always 5/day.

## 3. Player Death Penalty
**Severity**: **Softcore / Mid-Core**

*   **Consequences**:
    *   **Respawn**: At the nearest "Safe Spawn" or defined bind point.
    *   **Durability**: Equipment suffers durability loss.
        *   *Exception*: "Starter" gear or specific cosmetic items are indestructible.
    *   **Experience**: Small % EXP loss (TBD), but **no de-leveling** (optional) or irreversible stat loss.
*   **Pets**:
    *   Pet Death (KO) -> Revive needed.
    *   Combat End while KO -> **Amity Loss** (Already implemented).
    *   Excessive Damage (>= 1.5x MaxHP) -> **Despawn** (Already implemented).

## 4. PvP Scope
**Type**: **Open World with Opt-In**

*   **Mechanism**: `PKEnabled` Toggle.
*   **Rules**:
    *   Attacks can ONLY be initiated if the target has `PKEnabled = true`.
    *   **Mandatory**: Server-side handshake validation, cooldowns, and anti-spam protection.
*   **Exclusions**: No complex MMR, Ranked Ladders, or instanced Arenas required for V1.

## 5. Skill Progression
**System**: **Mastery-Based** (No Skill Points for Unlocking)

*   **Unlocking**:
    *   Based strictly on **Stat Requirements** (e.g., STR > 10) and **Quest Flags** (for special skills).
    *   Level is a prerequisite but not the currency.
*   **Upgrading**:
    *   **Mastery-by-Use**: Using a skill increases its Rank.
    *   **Stage Evolution**: Reaching specific Ranks unlocks the next Stage (e.g., Fireball I -> Fireball II).
*   **Stat Points**: Used solely for increasing Base Attributes (Str, Con, Int, Wis, Agi) on Level Up.

## 6. Quest Failure
**Philosophy**: **Explicit Conditions Only**

Quests generally do not fail from simple combat loss. They only fail under:
1.  **Time Limit Exceeded**: If `TimeLimitSeconds` expires -> Fail.
2.  **Key NPC Death**: Escort/Protect missions -> Fail.
3.  **Instance Wipe**: If the quest is bound to a specific instance run and the party wipes/timeouts -> Fail.

**Recovery**: Failed quests must be manually restarted (abandon & retake or specific retry mechanic).
