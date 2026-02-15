# Pitfalls Research: MMORPG Multiplayer Systems

**Domain:** MMORPG Market/Trading, Party, Guild, and Rebirth Systems
**Researched:** 2026-02-14
**Confidence:** HIGH (verified with real-world MMORPG disasters and technical research)

## Critical Pitfalls

### Pitfall 1: Race Condition Item/Gold Duplication

**What goes wrong:**
When database transactions are not atomic, players can exploit timing windows to duplicate items or currency. Multiple simultaneous requests can bypass validation checks before any of them complete, allowing withdrawal of the same resource multiple times.

**Why it happens:**
Balance checks and inventory updates happen as separate operations instead of within a single database transaction. The server validates "does player have item?" then later performs "remove item" - between these operations, another request can also pass the validation check.

**Real-world examples:**
- **New World (2021)**: Amazon had to disable all trading, player-to-player transfers, guild treasuries, and the trading post multiple times due to gold duplication exploits. Players exploited connection timing and trade window crashes to duplicate items. Ironically, the trade disable itself enabled another dupe method where town upgrades could be started/cancelled to duplicate gold.
- **MapleStory Europe (2011)**: Currency exploit caused complete economy collapse. Players exploited the "Meso Guard" skill with negative damage values to generate over 2 billion mesos, then bought up entire marketplaces, causing massive inflation. Nexon's response banned legitimate players who unknowingly received duped currency.
- **MapleStory (2025)**: 228 accounts permanently banned for duplication exploit in patch v.260 involving mesos and items.

**How to avoid:**
```csharp
// WRONG: Separate check and update (race condition)
if (player.Gold >= price) {
    await Task.Delay(1); // Network latency
    player.Gold -= price;
}

// CORRECT: Atomic transaction with database-level locking
using var transaction = await db.BeginTransactionAsync(IsolationLevel.Serializable);
try {
    var player = await db.Players
        .Where(p => p.Id == playerId)
        .ForUpdate() // Row-level lock
        .FirstOrDefaultAsync();

    if (player.Gold < price) {
        await transaction.RollbackAsync();
        return Error("Insufficient funds");
    }

    player.Gold -= price;
    buyer.AddItem(itemId);
    seller.Gold += price;
    seller.RemoveItem(itemId);

    await db.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch {
    await transaction.RollbackAsync();
    throw;
}
```

**Warning signs:**
- Players reporting "double charges" or "lost items" during lag spikes
- Unusual wealth accumulation by specific players during server instability
- Market prices spiraling out of control
- Reports of players deliberately lagging themselves (connection manipulation)

**Phase to address:**
Phase 1: Market Foundation - MUST implement transaction safety from day one. Retrofitting atomic transactions is extremely difficult and may require database migration.

---

### Pitfall 2: Incomplete Transaction Rollback (Partial State)

**What goes wrong:**
When a market transaction fails halfway (buyer charged but item not delivered, or vice versa), the system fails to rollback ALL state changes. Players lose items or currency permanently.

**Why it happens:**
Multi-step operations (deduct currency → add item to buyer → remove item from seller → add currency to seller) don't wrap all steps in a transaction. If step 3 fails, steps 1-2 already committed.

**Real-world examples:**
- **Lost Ark (2022-2024)**: Multiple incidents where Founder's Pack items could be redeemed multiple times. Amazon tracked RMT gold through "multiple accounts" and banned entire transaction chains. The Ignite Server Stronghold exploit (2024) affected 2,300 players, with varying punishments based on exploitation severity.

**How to avoid:**
- Use database transactions with Serializable isolation level for ALL multi-party operations
- Implement compensating transactions if rollback fails (log to recovery queue)
- Add idempotency keys to prevent replay attacks during retries
- Test transaction rollback under network partition scenarios

**Warning signs:**
- Players reporting "money disappeared" or "didn't receive item"
- Database showing orphaned records (items with no owner, currency deltas that don't sum to zero)
- Support tickets about incomplete trades

**Phase to address:**
Phase 1: Market Foundation - Core transaction infrastructure must handle all-or-nothing semantics.

---

### Pitfall 3: Missing Idempotency Protection (Replay Attacks)

**What goes wrong:**
Network retries cause duplicate operations. Player buys item once, packet is delayed, client retries, server processes purchase twice. Player charged twice or receives double items.

**Why it happens:**
Server doesn't track operation IDs. Each incoming "BuyItem" request is treated as new, even if it's a retry of a previous request.

**Real-world examples:**
- **Diablo IV (2023)**: Had to disable trading completely after gold and item duplication exploit. Players exploited connection timing to replay trade operations.
- **RuneScape (2003)**: Magenta Party hat duped over 2 million times, affecting economy permanently even decades later.

**How to avoid:**
```csharp
public class MarketTransaction
{
    public string OperationId { get; set; } // Client-generated UUID
    public TransactionState State { get; set; } // Pending/Completed/Failed
    public DateTime Timestamp { get; set; }
}

// On every market operation:
if (_transactions.TryGetValue(operationId, out var tx)) {
    if (tx.State == TransactionState.Completed) {
        return new Result {
            Success = true,
            Message = "Already completed",
            // Return original result (idempotent)
        };
    }
    if (tx.State == TransactionState.Pending) {
        return new Result {
            Success = false,
            Message = "Transaction in progress"
        };
    }
}
```

**Warning signs:**
- Players reporting double-charges during lag
- Same transaction appearing multiple times in logs with identical timestamps
- Items duplicating in inventory after network interruptions

**Phase to address:**
Phase 1: Market Foundation - Idempotency MUST be built into the first market implementation. The codebase already has idempotency in `EconomyManager` - extend this pattern to P2P trading.

---

### Pitfall 4: Guild Bank Permission Escalation

**What goes wrong:**
Guild permission systems allow members to withdraw more than intended, or members can "upgrade" their own permissions through exploits. Trusted members clean out the guild bank.

**Why it happens:**
Permission hierarchies are poorly designed (e.g., "if you can sort items, you can withdraw items"), or permission checks happen client-side, or rank changes don't properly update withdrawal limits.

**Real-world examples:**
- **Turtle WoW (2024)**: Guild bank theft epidemic. Design flaw: withdrawal limits couldn't be set per rank - if you set a limit for a lower rank, all higher ranks automatically got unlimited access. Members would get promoted, then empty the bank.
- **Albion Online (2019)**: 1 billion worth of in-game assets stolen from guild by single player who gained trust over months then ransacked everything.
- **Black Desert Online**: Materials from guild storage could be retrieved by ANY member including apprentices on trial periods. Players joined guilds, grabbed materials, converted to marketable items, then left.

**How to avoid:**
- Implement time-gated permissions (new members wait 1-2 weeks before withdrawal rights)
- Separate "view" vs "withdraw" vs "manage" permissions granularly
- Log all guild bank withdrawals with automatic alerts for large amounts
- Set per-rank daily withdrawal limits that apply independently (not inherited)
- Require two-factor authentication (guild leader approval) for high-value withdrawals

```csharp
public class GuildBankPermissions
{
    public Dictionary<int, RankPermissions> RankPermissions { get; set; }

    public bool CanWithdraw(GuildMember member, int tabId, int itemValue)
    {
        var permissions = RankPermissions[member.RankId];

        // Time gate
        if ((DateTime.UtcNow - member.JoinedAt).TotalDays < 7) {
            return false;
        }

        // Daily limit
        var todayWithdrawn = GetTodayWithdrawals(member.Id);
        if (todayWithdrawn + itemValue > permissions.DailyLimit) {
            return false;
        }

        // Tab-specific permissions
        if (!permissions.AllowedTabs.Contains(tabId)) {
            return false;
        }

        // High-value requires approval
        if (itemValue > 10000 && !HasLeaderApproval(member.Id)) {
            return false;
        }

        return true;
    }
}
```

**Warning signs:**
- New guild members asking for promotions quickly
- Large withdrawals happening right before member leaves guild
- Multiple guilds reporting theft by same player account
- Guild bank emptying out overnight

**Phase to address:**
Phase 2: Guild Foundation - Permission system must be designed correctly from the start. Changing permission semantics after guilds have inventory is extremely disruptive.

---

### Pitfall 5: Party Kick Abuse & Ninja Looting

**What goes wrong:**
Party leaders kick members right before boss dies to steal their loot share, or members use "Need" on all items regardless of class/need (ninja looting).

**Why it happens:**
Loot distribution happens AFTER boss kill, and kick permission isn't restricted during combat. Or loot system allows any player to roll Need without class restrictions.

**Real-world examples:**
- **Neverwinter**: Players abused vote-to-kick feature at or near end of boss fights, reducing the pool of players who could access drops. Led to major system overhaul.
- **World of Warcraft**: Ninja looting epidemic in early years. Party leaders changed loot to "Master Loot" right before boss kill and took everything. Led to implementation of Personal Loot system.

**How to avoid:**
- Disable party kick during combat and for 5 minutes after boss kill
- Lock loot eligibility when boss fight starts (anyone in party at pull gets loot, regardless of later kicks)
- Implement loot lockout: kicked players still get loot from boss they helped kill
- Personal loot system: each player gets individual roll, no competition
- Class-restrict Need rolls: only warriors can Need on warrior gear
- Reputation system: ninja looters get flagged, other players can see their history

```csharp
public class PartyLootSystem
{
    public bool CanKickMember(Party party, int memberId)
    {
        // Cannot kick during combat
        if (party.InCombat) return false;

        // Cannot kick if boss died in last 5 minutes
        if (party.LastBossKillTime != null &&
            (DateTime.UtcNow - party.LastBossKillTime.Value).TotalMinutes < 5) {
            return false;
        }

        return true;
    }

    public List<LootEligiblePlayer> GetLootEligiblePlayers(Boss boss)
    {
        // Lock eligibility at pull, not at kill
        return boss.PlayersAtPullTime; // Not party.CurrentMembers
    }

    public bool CanRollNeed(Player player, Item item)
    {
        // Class restriction
        if (!item.UsableByClasses.Contains(player.Class)) {
            return false; // Can only Greed or Pass
        }

        // Already have better item
        if (player.HasBetterItem(item)) {
            return false;
        }

        return true;
    }
}
```

**Warning signs:**
- Players reporting being kicked right before loot
- Same party leader appearing in multiple kick complaints
- Items going to players who can't use them (wrong class)
- Party finder showing very low completion rates for certain players

**Phase to address:**
Phase 2: Party Foundation - Loot rules must be finalized before party system launches. Changing loot rules mid-game causes massive player backlash.

---

### Pitfall 6: AFK Party Leeching (XP Exploitation)

**What goes wrong:**
Players join parties and go AFK to leech XP/loot from active players. Or high-level players "carry" low-level alts for power-leveling without doing any work.

**Why it happens:**
Party XP sharing doesn't require active participation. Distance check is too lenient. No contribution tracking.

**Real-world examples:**
- **Guild Wars 2**: Sparkfly Fen event allowed AFK leeching for XP/karma/gold. Players would park characters and farm events overnight.
- **MapleStory**: High-level characters kill difficult monsters while low-level characters (same owner) participate for sole purpose of getting XP without contributing.

**How to avoid:**
- Require damage/healing contribution for XP (minimum % of party total)
- Implement proximity requirement: must be within X meters of combat
- Add active input check: if no actions in 2 minutes, no XP share
- Show contribution metrics to party members (transparency prevents abuse)
- Diminishing returns for level gaps: if 20 levels apart, reduced XP share

```csharp
public class PartyXPDistribution
{
    public void DistributeXP(Party party, Enemy enemy, int totalXP)
    {
        var eligibleMembers = new List<PartyMember>();

        foreach (var member in party.Members)
        {
            // Proximity check
            if (Vector2.Distance(member.Position, enemy.Position) > 100) {
                continue;
            }

            // Contribution check (did at least 5% of damage OR healing)
            var contribution = GetContribution(member, enemy);
            if (contribution < 0.05f) {
                continue;
            }

            // Level gap penalty
            var levelDiff = Math.Abs(member.Level - enemy.Level);
            if (levelDiff > 20) {
                continue; // No XP if too far from enemy level
            }

            // Activity check
            if ((DateTime.UtcNow - member.LastActionTime).TotalSeconds > 120) {
                continue; // AFK for 2+ minutes
            }

            eligibleMembers.Add(member);
        }

        // Split XP among eligible members
        var xpPerMember = totalXP / eligibleMembers.Count;
        foreach (var member in eligibleMembers)
        {
            member.AddXP(xpPerMember);
        }
    }

    private float GetContribution(PartyMember member, Enemy enemy)
    {
        var damageContribution = member.DamageDealt / enemy.TotalDamageTaken;
        var healingContribution = member.HealingDone / party.TotalHealingReceived;
        return Math.Max(damageContribution, healingContribution);
    }
}
```

**Warning signs:**
- Players standing still during boss fights but receiving loot
- Party members with 0 damage dealt but full XP
- Reports of "AFK farmers" in popular grinding spots
- Unusual XP gain rates on low-level characters

**Phase to address:**
Phase 2: Party Foundation - XP sharing rules should be strict from launch. Tightening rules later causes player complaints ("you're nerfing my playstyle").

---

### Pitfall 7: Rebirth/Prestige Stat Duplication

**What goes wrong:**
When rebirth resets character level but grants permanent stat bonuses, players exploit the rebirth process to duplicate stat bonuses or reset without losing stats.

**Why it happens:**
Rebirth transaction isn't atomic. Server checks "eligible for rebirth?" → "reset level" → "grant bonus stats" as separate operations. If process fails between steps 2 and 3, player loses everything. If it fails between steps 1 and 2, they can retry and get double bonuses.

**Real-world examples:**
- **Ragnarok Online**: Rebirth system (Transcendent Classes) had various exploits over the years. Players found ways to avoid damage/status effects by manipulating screen positioning during stat recalculation.
- **Myth War 2**: Rebirth at level 120 grants 160 bonus stat points + 8 per level. Exploiting the rebirth timing could duplicate these bonuses.

**How to avoid:**
```csharp
public class RebirthSystem
{
    public async Task<RebirthResult> PerformRebirth(ServerCharacter character)
    {
        // Idempotency check
        if (character.HasRebirthInProgress) {
            return new RebirthResult {
                Success = false,
                Message = "Rebirth already in progress"
            };
        }

        using var transaction = await db.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            // Lock character row
            var charLocked = await db.Characters
                .Where(c => c.Id == character.Id)
                .ForUpdate()
                .FirstOrDefaultAsync();

            // Validation
            if (charLocked.Level < 120) {
                await transaction.RollbackAsync();
                return new RebirthResult {
                    Success = false,
                    Message = "Must be level 120"
                };
            }

            if (charLocked.RebirthCount >= 10) {
                await transaction.RollbackAsync();
                return new RebirthResult {
                    Success = false,
                    Message = "Maximum rebirths reached"
                };
            }

            // Atomic rebirth operation
            var snapshot = charLocked.CreateSnapshot(); // For rollback

            charLocked.RebirthCount++;
            charLocked.Level = 1;
            charLocked.Experience = 0;

            // Grant permanent bonuses BEFORE commit
            var bonusStats = 160 + (charLocked.RebirthCount * 8);
            charLocked.BonusStatPoints += bonusStats;

            // Log rebirth (prevent future exploitation)
            await db.RebirthHistory.AddAsync(new RebirthRecord {
                CharacterId = charLocked.Id,
                Timestamp = DateTime.UtcNow,
                RebirthNumber = charLocked.RebirthCount,
                BonusStatsGranted = bonusStats,
                Snapshot = snapshot // For audit
            });

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return new RebirthResult {
                Success = true,
                BonusStats = bonusStats,
                NewRebirthCount = charLocked.RebirthCount
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            // Log failed rebirth attempt for security monitoring
            SecurityLogger.LogSecurityEvent("RebirthFailed", character.Id,
                $"Exception: {ex.Message}");

            return new RebirthResult {
                Success = false,
                Message = "Rebirth failed, please try again"
            };
        }
    }
}
```

**Warning signs:**
- Players with abnormally high stats for their rebirth count
- Reports of "rebirth failed but I lost my level"
- Multiple rebirth attempts in quick succession (replay attack indicator)
- Database showing rebirth count mismatches with stat bonuses

**Phase to address:**
Phase 3: Rebirth System - Must be atomic from day one. Rebirth exploits are extremely hard to detect and roll back because they compound over time.

---

### Pitfall 8: Marketplace Price Manipulation & Wash Trading

**What goes wrong:**
Players create multiple accounts to fake market activity (wash trading), manipulate prices by buying/selling to themselves, or corner markets by buying all supply and relisting at inflated prices.

**Why it happens:**
No restrictions on self-trading, no detection for suspicious patterns, no limits on market share concentration.

**Real-world examples:**
- **EVE Online**: Players manipulate markets by attacking trade hubs to create shortages, driving up demand and inflating prices.
- **World of Warcraft**: Auction house "farming" - players use addons to discover market niches and monopolize them. Blizzard had to update exploitation policy to restrain auction house farming.

**How to avoid:**
- Ban trading between accounts from same IP/device
- Implement market share limits (one player can't own >30% of an item's listings)
- Add listing fees (non-refundable) to prevent wash trading spam
- Detect suspicious patterns: same buyer/seller repeatedly trading same item
- Delay price changes: new listings can't be <80% or >120% of median price for first hour
- Velocity limits: can't list more than X items of same type per day

```csharp
public class MarketplaceAntiManipulation
{
    public bool CanCreateListing(Player player, Item item, int price)
    {
        // Market share check
        var existingListings = GetListingsForItem(item.Id);
        var playerListings = existingListings.Count(l => l.SellerId == player.Id);
        var marketShare = (float)playerListings / existingListings.Count;

        if (marketShare > 0.3f) {
            return false; // Cannot control >30% of market
        }

        // Price bounds check (first hour)
        var medianPrice = GetMedianPrice(item.Id);
        if (price < medianPrice * 0.8f || price > medianPrice * 1.2f) {
            // Allow but flag for review
            SecurityLogger.LogSecurityEvent("MarketPriceOutlier", player.Id,
                $"Item:{item.Id} Price:{price} Median:{medianPrice}");
        }

        // Velocity check
        var todayListings = GetTodayListings(player.Id, item.Id);
        if (todayListings > 100) {
            return false; // Listing spam
        }

        return true;
    }

    public bool IsWashTrading(int buyerId, int sellerId, int itemId)
    {
        // Same IP/device
        if (GetIPAddress(buyerId) == GetIPAddress(sellerId)) {
            return true;
        }

        // Repeated back-and-forth trades
        var recentTrades = GetRecentTrades(buyerId, sellerId, itemId, days: 7);
        if (recentTrades.Count > 10) {
            return true; // Suspicious pattern
        }

        return false;
    }
}
```

**Warning signs:**
- Same item listed/delisted repeatedly by same player
- New accounts immediately listing high-value items
- Prices spiking without supply shortage
- Player owning majority of listings for valuable item

**Phase to address:**
Phase 1: Market Foundation - Anti-manipulation needs to be in the initial market design. Adding restrictions later causes legitimate traders to complain.

---

### Pitfall 9: Compound Interest Exploit (Listing Fee Arbitrage)

**What goes wrong:**
If marketplace allows free listing cancellations or listing fees are refundable, players exploit compound interest by listing/canceling repeatedly to generate currency or manipulate markets.

**Why it happens:**
Listing fees are refunded on cancellation, or cancellation is free. Players list high-value items, cancel, relist at different price, creating artificial activity and potentially earning interest or bonuses.

**How to avoid:**
- Make listing fees NON-REFUNDABLE (burn the currency)
- Add cooldown: can't relist same item for 1 hour after cancellation
- Limit cancellations: max 5 per day per player
- Escalating fees: each cancellation costs more

```csharp
public class ListingFeeSystem
{
    public bool CancelListing(Player player, Listing listing)
    {
        // Fee is NOT refunded
        var fee = listing.Price * 0.05f; // 5% listing fee was already paid
        // Fee is gone forever (deflationary measure)

        // Cooldown enforcement
        listing.CanceledAt = DateTime.UtcNow;
        AddCooldown(player.Id, listing.ItemId, hours: 1);

        // Count cancellations
        var todayCancellations = GetTodayCancellations(player.Id);
        if (todayCancellations >= 5) {
            return false; // Daily limit reached
        }

        return true;
    }
}
```

**Warning signs:**
- Players listing/canceling same items repeatedly
- Unusual currency generation without corresponding trading activity
- Market flooded with listings that never sell (cancel before expiry)

**Phase to address:**
Phase 1: Market Foundation - Fee structure must be set at launch. Changing fees later disrupts market equilibrium.

---

### Pitfall 10: Guild Chat Command Injection

**What goes wrong:**
Guild chat allows special characters or commands that can be exploited to execute unintended actions (kick members, promote, withdraw from bank) or inject malicious content.

**Why it happens:**
Chat parsing doesn't sanitize input. Commands like "/kick PlayerName" are processed even when sent as chat messages. Or HTML/markdown injection allows phishing links.

**Real-world examples:**
- **New World (2021)**: Players could post unsavory images via in-game chat and use scripting to crash other players or gain gold through chat injection exploits.

**How to avoid:**
```csharp
public class GuildChatSystem
{
    public void ProcessChatMessage(GuildMember sender, string message)
    {
        // Sanitize input
        message = SanitizeInput(message);

        // Disable commands in chat (only work via dedicated command UI)
        if (message.StartsWith("/")) {
            SendError(sender, "Commands cannot be sent via chat");
            return;
        }

        // Length limit
        if (message.Length > 500) {
            message = message.Substring(0, 500);
        }

        // HTML/script injection prevention
        message = HttpUtility.HtmlEncode(message);

        BroadcastToGuild(sender.GuildId, message);
    }

    private string SanitizeInput(string input)
    {
        // Remove null characters
        input = input.Replace("\0", "");

        // Remove SQL injection attempts
        input = Regex.Replace(input, @"[';\""-]", "");

        // Remove script tags
        input = Regex.Replace(input, @"<script.*?>.*?</script>", "",
            RegexOptions.IgnoreCase);

        return input;
    }
}
```

**Warning signs:**
- Reports of "guild chat crashed my game"
- Players getting kicked/promoted through chat messages
- Phishing links appearing in guild chat

**Phase to address:**
Phase 2: Guild Foundation - Input sanitization must be thorough from launch. Chat exploits spread rapidly.

---

## Technical Debt Patterns

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| File-based storage instead of PostgreSQL transactions | Faster initial development | Race conditions, no ACID guarantees, duplication exploits | NEVER for multiplayer trading/currency |
| Client-side validation only | Simpler server code | Trivial to bypass, enables cheating | Never for authoritative actions |
| Optimistic locking instead of pessimistic | Better throughput | Requires retry logic, harder to debug | Only for non-critical reads |
| In-memory transaction tracking only | Fast, no DB overhead | State lost on server crash, no audit trail | Never for real-money transactions |
| Same table for all transaction types | Simpler schema | Poor indexing, slow queries at scale | Only for prototypes/MVPs |
| No rate limiting on market operations | Simpler code | Vulnerable to spam, DoS, dupe exploits | Never in production |

## Performance Traps

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| N+1 queries for party member data | Lag spikes when opening party UI | Eager load party members in single query | >5 members per party |
| Full table scan for marketplace listings | Search gets slower daily | Index on ItemId, Price, Timestamp | >10,000 listings |
| Synchronous transaction commits | Trade confirmation takes 2+ seconds | Use async commits with replication | >100 concurrent trades |
| No pagination on guild member list | Guild UI freezes | Paginate with limit 50 per page | >200 guild members |
| Real-time XP sync to database | Database overwhelmed during raids | Batch XP updates every 10 seconds | >20 party members |
| Linear search for rebirth eligibility | Character screen lag at high level | Index on Level and RebirthCount | Level >100 |

## Security Mistakes

| Mistake | Risk | Prevention |
|---------|------|------------|
| Client sends final gold amount after trade | Client can send ANY amount | Server calculates EVERYTHING, client is display only |
| Guild permissions stored client-side | Trivial permission escalation | All permission checks server-side with DB validation |
| Party kick uses client-provided reason | Reason could be command injection | Sanitize ALL text input, use enum for kick reasons |
| Rebirth stat bonuses trust client stats | Client can claim any bonus | Server calculates bonuses from RebirthCount in DB |
| Marketplace search by user input | SQL injection vulnerability | Use parameterized queries, whitelist search fields |
| No logging for guild bank withdrawals | Theft goes undetected | Log EVERY withdrawal with timestamp, item, member ID |

## UX Pitfalls

| Pitfall | User Impact | Better Approach |
|---------|-------------|-----------------|
| Market listings expire while player offline | Lost listing fees, items disappear | Auto-relist OR send items to mailbox on expiry |
| Party kicked mid-dungeon loses ALL progress | Frustration, quit game | Kicked players keep loot eligibility, can rejoin |
| Guild bank has no withdrawal history | Leaders can't detect theft | Full audit log visible to officers+ |
| Rebirth fails with no clear error | Players lose trust in system | Show detailed error + prevention (level requirement) |
| No confirmation for high-value market listings | Accidental 1 gold listing of rare item | Require confirmation if price <50% of average |
| Party XP share invisible to members | Members don't know if leech is present | Show contribution % to all party members |

## "Looks Done But Isn't" Checklist

- [ ] **Market Transactions:** Often missing idempotency keys — verify by simulating network retry (should not double-charge)
- [ ] **Party Loot:** Often missing kick protection during combat — verify kick button disables when in combat
- [ ] **Guild Permissions:** Often missing time gates for new members — verify new member can't withdraw immediately
- [ ] **Rebirth System:** Often missing transaction rollback — verify failed rebirth doesn't lose player progress
- [ ] **Marketplace Search:** Often missing pagination — verify searching 50,000 items doesn't timeout
- [ ] **Party XP:** Often missing contribution tracking — verify AFK player gets 0 XP
- [ ] **Trade History:** Often missing audit logs — verify every gold/item transfer is logged immutably
- [ ] **Rate Limiting:** Often missing per-user limits — verify single player can't spam 1000 listings/second

## Recovery Strategies

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| Item duplication discovered | HIGH | 1. Disable trading immediately 2. Analyze logs for dupe pattern 3. Remove duped items 4. Rollback affected accounts 5. Ban exploiters |
| Guild bank cleaned out | MEDIUM | 1. Restore from backup snapshot 2. Review withdrawal logs 3. Ban thief 4. Compensate guild if rollback not possible |
| Market wash trading ring | MEDIUM | 1. Identify alt account network via IP/device 2. Ban all accounts 3. Remove manipulated listings 4. Refund legit buyers |
| Party kick abuse | LOW | 1. Restore loot eligibility to kicked player 2. Warn/ban abuser 3. Implement kick protection |
| Rebirth stat exploit | HIGH | 1. Audit all rebirth history records 2. Recalculate correct stats 3. Fix discrepancies 4. Permanent ban for intentional exploitation |
| Economy hyperinflation | VERY HIGH | 1. Emergency gold sink events 2. Adjust drop rates 3. May require server rollback or economy reset |

## Pitfall-to-Phase Mapping

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| Race condition duplication | Phase 1: Market Foundation | Load test: 100 concurrent trades, verify no dupes |
| Incomplete rollback | Phase 1: Market Foundation | Fault injection: kill DB mid-transaction, verify rollback |
| Missing idempotency | Phase 1: Market Foundation | Network simulation: replay requests, verify single charge |
| Guild permission escalation | Phase 2: Guild Foundation | Security test: new member tries to withdraw, should fail |
| Party kick abuse | Phase 2: Party Foundation | Test: kick during combat, should fail |
| AFK leeching | Phase 2: Party Foundation | Test: stand still in party, should get 0 XP |
| Rebirth stat exploit | Phase 3: Rebirth System | Test: fail rebirth mid-process, verify no stat gain |
| Market manipulation | Phase 1: Market Foundation | Monitor: flag players with >30% market share |
| Listing fee exploit | Phase 1: Market Foundation | Test: cancel listing, verify fee not refunded |
| Chat injection | Phase 2: Guild Foundation | Pen test: send command in chat, verify no execution |

## Current Project Vulnerabilities

Based on codebase analysis (`EconomyManager.cs`), the project already has:
- ✅ Transaction idempotency (operationId tracking)
- ✅ Rate limiting
- ✅ Ledger-based audit trail with hash chaining
- ✅ Atomic transaction support
- ✅ Compensating transactions (refund on failure)

**Still needs for multiplayer systems:**
- ❌ Party system XP contribution tracking
- ❌ Guild permission time gates
- ❌ Market share limits & wash trading detection
- ❌ Party kick combat protection
- ❌ Rebirth transaction atomicity
- ❌ P2P trade anti-duplication (extend EconomyManager pattern)

**Existing security gaps to address:**
- Weak movement validation (mentioned in project context) could enable position-based exploits for party XP sharing
- No anti-replay protection (mentioned) - current `operationId` system in `EconomyManager` should be extended to ALL multiplayer operations
- PostgreSQL migration should use `IsolationLevel.Serializable` for all multiplayer transactions

## Sources

### Real-World MMORPG Disasters
- [MapleStory Europe Economy Collapse (2011)](https://www.engadget.com/2011-02-01-maplestory-europes-economy-collapses-due-to-currency-exploit.html)
- [New World Duplication Exploit Forces Trading Disabled](https://www.pcgamer.com/amazon-disables-new-world-wealth-transfers-to-fight-gold-dupe-exploit/)
- [New World Turns Off Economy Over Duplication Exploit](https://comicbook.com/gaming/news/amazon-new-world-turns-off-economy-trading-duplication-exploit/)
- [MapleStory Bans Hundreds Over Exploit](https://mmofallout.com/2025/06/17/maplestory-bans-a-few-hundred-accounts-over-exploit/)
- [Lost Ark Founder's Pack Duplication](https://www.dexerto.com/lost-ark/lost-ark-devs-confirm-punishment-for-duplicate-founders-packs-exploiters-1760715/)
- [Albion Online Guild Theft](https://www.mmorpg.com/news/1-billion-worth-of-in-game-assets-stolen-from-albion-online-guild-by-player-2000119386)
- [Diablo IV Trading Disabled Due to Dupe Exploit](https://www.mmorpg.com/news/diablo-iv-disables-player-trading-thanks-to-gold-and-item-dupe-exploit-2000128708)

### Guild & Party Systems
- [Turtle WoW Guild Bank Theft Discussion](https://forum.turtle-wow.org/viewtopic.php?t=15192)
- [Neverwinter Vote-to-Kick Abuse Fix](https://www.mmorpg.com/news/new-vote-to-kick-features-to-be-added-to-combat-abuse-2000087119)
- [WoW Ninja Looting - Wowpedia](https://wowpedia.fandom.com/wiki/Loot_ninja)
- [Black Desert Online Guild Storage Permissions](https://www.naeu.playblackdesert.com/en-US/Forum/ForumTopic/Detail?_topicNo=149&_opinionNo=571)

### Technical Prevention
- [Race Condition Vulnerability Explained - Snyk](https://learn.snyk.io/lesson/race-condition/)
- [Database Race Conditions - Doyensec](https://blog.doyensec.com/2024/07/11/database-race-conditions.html)
- [Item Duplication Exploits and Prevention](https://munique.net/item-duplication-exploits/)
- [Duping in Video Games - Wikipedia](https://en.wikipedia.org/wiki/Duping_(video_games))

### Economy & Market Manipulation
- [MMO Economy Manipulation - Game Developer](https://www.gamedeveloper.com/production/mmo-economy-manipulation-)
- [Virtual Economic Theory: How MMOs Really Work](https://www.gamedeveloper.com/business/virtual-economic-theory-how-mmos-really-work)
- [MMORPG Inflation Discussion - Ask a Game Dev](https://askagamedev.tumblr.com/post/757353839637790720/mmorpg-ingame-economies-have-inflation-thats)

---
*Pitfalls research for: The Wonderland Legacy (TWL) Multiplayer Systems*
*Researched: 2026-02-14*
*Applies to: P2P Market, Party, Guild, and Rebirth system implementation*
