# SECURITY AUDIT REPORT - 2026-02-21

## 1) RESULT: REPORT

Due to the **Anti-Collision Clause** (uncertainty about daily task existence), this job produces a **REPORT** instead of a PR, despite identifying critical vulnerabilities with small potential fixes.

## 2) THREATS

### [P0] Pre-Login Replay Attack (Authentication Bypass)
**Scenario:** An attacker captures a valid `LoginRequest` packet (containing `username` and `passHash`). The attacker replays this packet multiple times.
**Vulnerability:** In `ClientSession.HandleMessageAsync`, `ReplayGuard.Validate` is called with `UserId` (initially -1) or `msg.GetHashCode()`.
Since `NetMessage` does not override `GetHashCode`, every deserialized packet instance has a unique hash.
`ReplayGuard` treats each packet as a **new session**, creating a fresh `SessionNonceCache` for it.
Consequently, the `nonce` check always passes (as the cache is empty), allowing the replay to succeed.
**Impact:** Attacker can replay login attempts, potentially locking accounts, spamming logs, or if `nonce` was used for shared secrets, compromising session integrity.

### [P0] Movement Speed / Teleport Hack
**Scenario:** An attacker modifies the client to send a `MoveRequest` with `dx: 1000, dy: 1000`.
**Vulnerability:** `ClientSession.HandleMoveAsync` directly adds `dx` and `dy` to `Character.X/Y` without validation:
```csharp
Character.X += moveDto.dx;
Character.Y += moveDto.dy;
```
There is no check for maximum speed, distance traveled, or collision with walls.
**Impact:** Players can teleport instantly across the map, bypass obstacles, and speed-hack to gain unfair advantages (gathering, kiting mobs).

### [P1] Interaction Proximity Bypass (Remote Interaction)
**Scenario:** An attacker sends an `InteractRequest` with `TargetName="RareChest"` or `TargetName="QuestNPC"` while standing at the start of the map.
**Vulnerability:** `InteractHandler` and `InteractionManager` process the request based solely on `TargetName`. There is no check to verify if the player is within interaction range of the target entity.
**Impact:** Players can loot chests, complete quests, or use shops from safe zones without traversing dangerous areas.

### [P1] Consumable Item Spam (No Cooldowns)
**Scenario:** An attacker sends 50 `UseItemRequest` packets for a generic "Health Potion" in 1 second.
**Vulnerability:** `ServerCharacter.UseItem` decrements quantity and applies effects (if handled) but does not check or set any cooldown timestamp for the item type.
**Impact:** Players can achieve near-infinite health regeneration by spamming potions, breaking combat balance.

### [P2] Remote Quest Reward Claiming
**Scenario:** Similar to Interaction Bypass, an attacker sends `ClaimRewardRequest` for a completed quest without returning to the NPC.
**Vulnerability:** `ClientSession.HandleClaimRewardAsync` calls `QuestComponent.ClaimReward` without validating NPC proximity.
**Impact:** Bypasses the "return to quest giver" gameplay loop.

## 3) MITIGATIONS

### Fix [P0] Pre-Login Replay Attack
**Solution:** Use a stable connection-scoped identifier for `ReplayGuard` before authentication.
**Implementation:**
In `ClientSession`, generate a `_sessionId` (Guid or Random Int) in the constructor.
Pass this `_sessionId` to `ReplayGuard.Validate` instead of `msg.GetHashCode()` when `UserId` is not yet set.

### Fix [P0] Movement Speed / Teleport Hack
**Solution:** Validate `dx` and `dy` magnitude.
**Implementation:**
In `ClientSession.HandleMoveAsync`:
1. Calculate distance `d = Sqrt(dx*dx + dy*dy)`.
2. Check if `d <= MaxSpeed * DeltaTime` (approximate, or just a hard cap like `1.0` per tick).
3. If `d` is excessive, reject the move and force a position correction (teleport back).

### Fix [P1] Interaction Proximity Bypass
**Solution:** Enforce distance check in `InteractHandler`.
**Implementation:**
1. `InteractHandler` needs access to the target's position.
2. Maintain a spatial index or lookup (e.g., inside `MapRegistry` or `SpawnManager`) to find entity position by `TargetName`.
3. In `InteractHandler.Handle`, calculate `Distance(Character, Target)`.
4. If `Distance > InteractionRange` (e.g., 2.0 tiles), reject the request.

### Fix [P1] Consumable Item Spam
**Solution:** Add Cooldown system to `ServerCharacter`.
**Implementation:**
1. Add `Dictionary<int, DateTime> _itemCooldowns` to `ServerCharacter`.
2. In `UseItem`, check `if (Now < _itemCooldowns[itemId]) return false;`.
3. If used, set `_itemCooldowns[itemId] = Now + Item.Cooldown`. (Need to add `Cooldown` property to `Item` model or lookup).

## 4) IMPLEMENTATION NOTES

- **ClientSession.cs**: Critical fixes for Replay and Movement.
- **ServerCharacter.cs**: Add Cooldown logic.
- **InteractHandler.cs**: Add proximity logic (requires dependency on `IMapRegistry` or similar).
- **NetMessage.cs**: No changes needed, but be aware of `GetHashCode` behavior.

## 5) NEXT STEPS (Tickets)

1. **[SEC-001] Fix ReplayGuard Session ID**: Implement stable session ID in `ClientSession` to enable pre-login replay protection. (Estimated: 1h)
2. **[SEC-002] Implement Movement Validation**: Add speed/distance limits in `HandleMoveAsync` and server-side position correction. (Estimated: 2h)
3. **[SEC-003] Enforce Interaction Proximity**: Implement spatial lookup for entities and validate distance in `InteractHandler`. (Estimated: 4h)
