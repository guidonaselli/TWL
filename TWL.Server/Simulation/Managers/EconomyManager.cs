using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using TWL.Server.Security;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.DTO;
using TWL.Shared.Domain.Models;

namespace TWL.Server.Simulation.Managers;

public class EconomyManager : IEconomyService, IDisposable
{
    private const int CLEANUP_INTERVAL = 100;
    private const double EXPIRATION_MINUTES = 10.0;
    private const long DAILY_GIFT_LIMIT = 5000;

    private static readonly Regex _ledgerRegex = new(
        @"^([^,]+),([^,]+),([^,]+),(.+),([-\d]+),([-\d]+)(?:,([a-fA-F0-9]+),([a-fA-F0-9]+))?$",
        RegexOptions.Compiled);

    private readonly string _ledgerFile;
    private readonly string _snapshotFile;
    private readonly string _providerSecret;

    private readonly object _ledgerLock = new();
    private readonly Channel<string> _logChannel;
    private readonly Task _logTask;

    private string _lastHash = "0000000000000000000000000000000000000000000000000000000000000000";

    // Mock Data
    private readonly Dictionary<string, long> _productPrices = new()
    {
        { "gems_100", 100 },
        { "gems_500", 500 },
        { "gems_1000", 1000 }
    };

    private readonly ConcurrentDictionary<int, RateLimitTracker> _rateLimits = new();

    private readonly Dictionary<int, (long Price, int ItemId, BindPolicy Policy)> _shopItems = new()
    {
        { 1, (10, 101, BindPolicy.Unbound) }, // ShopItem 1: 10 Gems -> Item 101 (Potion)
        { 2, (50, 102, BindPolicy.BindOnEquip) }, // ShopItem 2: 50 Gems -> Item 102 (Sword)
        { 3, (100, 103, BindPolicy.BindOnPickup) } // ShopItem 3: 100 Gems -> Item 103 (Armor)
    };

    private readonly ConcurrentDictionary<string, Transaction> _transactions = new();
    private int _cleanupCounter;
    private int _isCleaningUp;

    public EconomyManager(string ledgerFile = "economy_ledger.log", string? providerSecret = null)
    {
        _providerSecret = providerSecret ?? Environment.GetEnvironmentVariable("ECONOMY_SECRET") ?? "TEST_SECRET_DEFAULT";
        if (_providerSecret == "TEST_SECRET_DEFAULT")
        {
            Console.WriteLine("Warning: EconomyManager using TEST_SECRET_DEFAULT. Do not use in Production.");
        }

        Metrics = new EconomyMetrics(this);
        _ledgerFile = ledgerFile;
        _snapshotFile = Path.ChangeExtension(_ledgerFile, ".snapshot.json");

        // Ensure ledger exists
        if (!File.Exists(_ledgerFile))
        {
            File.WriteAllText(_ledgerFile, "Timestamp,Type,UserId,Details,Delta,NewBalance\n");
        }
        else
        {
            var snapshotTime = LoadSnapshot();
            ReplayLedger(snapshotTime);
        }

        // Initialize Async Logging
        _logChannel = Channel.CreateUnbounded<string>();
        _logTask = Task.Run(ProcessLogQueue);
    }

    public EconomyMetrics Metrics { get; }

    public void Dispose()
    {
        SaveSnapshot();

        try
        {
            _logChannel.Writer.TryComplete();
        }
        catch
        {
        }

        try
        {
            // Wait gracefully for the task to finish writing remaining logs.
            // We use a timeout to prevent hanging the process indefinitely.
            _logTask.Wait(2000);
        }
        catch
        {
        }
    }

    public PurchaseGemsIntentResponseDTO InitiatePurchase(int userId, string productId, string? traceId = null)
    {
        TryCleanup();

        if (!CheckRateLimit(userId))
        {
            return null;
        }

        if (!_productPrices.ContainsKey(productId))
        {
            return null;
        }

        var orderId = Guid.NewGuid().ToString("N");
        var tx = new Transaction
        {
            OrderId = orderId,
            UserId = userId,
            ProductId = productId,
            State = TransactionState.Pending,
            Timestamp = DateTime.UtcNow
        };

        _transactions[orderId] = tx;

        LogLedger("PurchaseIntent", userId, $"Order:{orderId}, Product:{productId}", 0, 0, traceId);

        return new PurchaseGemsIntentResponseDTO
        {
            OrderId = orderId,
            ProviderUrl = $"https://mock-provider.com/pay?order={orderId}"
        };
    }

    public EconomyOperationResultDTO VerifyPurchase(int userId, string orderId, string receiptToken,
        ServerCharacter character, string? traceId = null)
    {
        if (!CheckRateLimit(userId))
        {
            SecurityLogger.LogSecurityEvent("PurchaseVerifyFailed", userId, "Reason:RateLimitExceeded", traceId);
            return new EconomyOperationResultDTO { Success = false, Message = "Rate limit exceeded" };
        }

        if (!_transactions.TryGetValue(orderId, out var tx))
        {
            SecurityLogger.LogSecurityEvent("PurchaseVerifyFailed", userId, $"Reason:OrderNotFound OrderId:{orderId}", traceId);
            return new EconomyOperationResultDTO { Success = false, Message = "Order not found" };
        }

        if (tx.UserId != userId)
        {
            SecurityLogger.LogSecurityEvent("PurchaseVerifyFailed", userId,
                $"Reason:UserMismatch OrderId:{orderId} TxUser:{tx.UserId}", traceId);
            return new EconomyOperationResultDTO { Success = false, Message = "User mismatch" };
        }

        lock (tx)
        {
            // HARDENING: Check character-local persistence for double-spend prevention.
            // If the global ledger failed to record completion but the character was saved with the order marked,
            // we must treat it as completed to avoid giving currency twice.
            if (tx.State == TransactionState.Completed || character.HasProcessedOrder(orderId))
            {
                // Idempotent success
                return new EconomyOperationResultDTO
                {
                    Success = true,
                    Message = "Already completed",
                    NewBalance = character.PremiumCurrency,
                    OrderId = orderId
                };
            }

            if (tx.State != TransactionState.Pending)
            {
                SecurityLogger.LogSecurityEvent("PurchaseVerifyFailed", userId,
                    $"Reason:InvalidState State:{tx.State}", traceId);
                return new EconomyOperationResultDTO { Success = false, Message = "Invalid state" };
            }

            // Verify receipt (Mock Signature Validation)
            if (!ValidateReceipt(receiptToken, orderId))
            {
                SecurityLogger.LogSecurityEvent("PurchaseVerifyFailed", userId, "Reason:InvalidSignature", traceId);
                return new EconomyOperationResultDTO { Success = false, Message = "Invalid receipt signature" };
            }

            // Credit Gems
            var gemsToAdd = _productPrices[tx.ProductId];
            character.AddPremiumCurrency(gemsToAdd);

            // Mark locally on character (critical for anti-double spend if ledger write fails)
            character.MarkOrderProcessed(orderId);

            tx.State = TransactionState.Completed;

            LogLedger("PurchaseVerify", userId, $"Order:{orderId}, Product:{tx.ProductId}", gemsToAdd,
                character.PremiumCurrency, traceId);

            return new EconomyOperationResultDTO
            {
                Success = true,
                Message = "Purchase successful",
                NewBalance = character.PremiumCurrency,
                OrderId = orderId
            };
        }
    }

    public EconomyOperationResultDTO BuyShopItem(ServerCharacter character, int shopItemId, int quantity,
        string? operationId, string? traceId = null)
    {
        TryCleanup();

        if (!CheckRateLimit(character.Id))
        {
            SecurityLogger.LogSecurityEvent("ShopBuyFailed", character.Id, "Reason:RateLimitExceeded", traceId);
            return new EconomyOperationResultDTO { Success = false, Message = "Rate limit exceeded" };
        }

        if (quantity <= 0 || quantity > 999)
        {
            return new EconomyOperationResultDTO { Success = false, Message = "Invalid quantity (1-999)" };
        }

        // Idempotency Check
        Transaction? tx = null;
        if (!string.IsNullOrEmpty(operationId))
        {
            if (_transactions.TryGetValue(operationId, out tx))
            {
                lock (tx!)
                {
                    if (tx.State == TransactionState.Completed)
                    {
                        return new EconomyOperationResultDTO
                        {
                            Success = true,
                            Message = "Already completed",
                            NewBalance = character.PremiumCurrency,
                            OrderId = operationId
                        };
                    }

                    if (tx.State == TransactionState.Pending)
                    {
                        return new EconomyOperationResultDTO { Success = false, Message = "Transaction in progress" };
                    }
                    // If Failed, allow retry (fall through)
                }
            }
            else
            {
                // Register Pending Transaction
                tx = new Transaction
                {
                    OrderId = operationId,
                    UserId = character.Id,
                    ProductId = $"shop_{shopItemId}",
                    State = TransactionState.Pending,
                    Timestamp = DateTime.UtcNow
                };
                if (!_transactions.TryAdd(operationId, tx))
                {
                    // If we failed to add, it means someone else added it concurrently.
                    // Recursively retry to hit the TryGetValue path.
                    return BuyShopItem(character, shopItemId, quantity, operationId);
                }
            }
        }

        if (!_shopItems.TryGetValue(shopItemId, out var itemDef))
        {
            if (tx != null)
            {
                lock (tx)
                {
                    tx.State = TransactionState.Failed;
                }
            }

            SecurityLogger.LogSecurityEvent("ShopBuyFailed", character.Id, $"Reason:ItemNotFound Item:{shopItemId}", traceId);
            return new EconomyOperationResultDTO { Success = false, Message = "Item not found" };
        }

        var totalCost = itemDef.Price * quantity;

        if (!character.TryConsumePremiumCurrency(totalCost))
        {
            if (tx != null)
            {
                lock (tx)
                {
                    tx.State = TransactionState.Failed;
                }
            }

            SecurityLogger.LogSecurityEvent("ShopBuyFailed", character.Id,
                $"Reason:InsufficientFunds Cost:{totalCost} Has:{character.PremiumCurrency}", traceId);
            return new EconomyOperationResultDTO { Success = false, Message = "Insufficient funds" };
        }

        var details = $"ShopItem:{shopItemId}, Item:{itemDef.ItemId} x{quantity}";
        if (!string.IsNullOrEmpty(operationId))
        {
            details += $", OrderId:{operationId}";
        }

        // Add Item with Policy
        var added = character.AddItem(itemDef.ItemId, quantity, itemDef.Policy);

        if (!added)
        {
            // Compensation: Refund
            character.AddPremiumCurrency(totalCost);

            if (tx != null)
            {
                lock (tx)
                {
                    tx.State = TransactionState.Failed;
                }
            }

            LogLedger("ShopBuyFailed", character.Id, details + ", Reason:InventoryFull", 0, character.PremiumCurrency, traceId);

            return new EconomyOperationResultDTO { Success = false, Message = "Inventory full" };
        }

        // Mark Transaction as Completed
        if (tx != null)
        {
            lock (tx)
            {
                tx.State = TransactionState.Completed;
            }
        }

        LogLedger("ShopBuy", character.Id, details, -totalCost, character.PremiumCurrency, traceId);

        return new EconomyOperationResultDTO
        {
            Success = true,
            Message = "Purchase successful",
            NewBalance = character.PremiumCurrency,
            OrderId = operationId
        };
    }

    public EconomyOperationResultDTO GiftShopItem(ServerCharacter giver, ServerCharacter receiver, int shopItemId,
        int quantity, string operationId, string? traceId = null)
    {
        if (giver == null || receiver == null)
        {
            return new EconomyOperationResultDTO { Success = false, Message = "Invalid participants" };
        }

        if (string.IsNullOrEmpty(operationId))
        {
            return new EconomyOperationResultDTO { Success = false, Message = "OperationId is required for gifting" };
        }

        if (quantity <= 0 || quantity > 999)
        {
            return new EconomyOperationResultDTO { Success = false, Message = "Invalid quantity (1-999)" };
        }

        if (!CheckRateLimit(giver.Id))
        {
            return new EconomyOperationResultDTO { Success = false, Message = "Rate limit exceeded" };
        }

        // Idempotency Check
        Transaction? tx = null;
        if (_transactions.TryGetValue(operationId, out tx))
        {
            lock (tx!)
            {
                if (tx.State == TransactionState.Completed)
                {
                    return new EconomyOperationResultDTO
                    {
                        Success = true,
                        Message = "Already completed",
                        NewBalance = giver.PremiumCurrency,
                        OrderId = operationId
                    };
                }

                if (tx.State == TransactionState.Pending)
                {
                    return new EconomyOperationResultDTO { Success = false, Message = "Transaction in progress" };
                }
                // If Failed, allow retry
            }
        }
        else
        {
            tx = new Transaction
            {
                OrderId = operationId,
                UserId = giver.Id,
                ProductId = $"gift_shop_{shopItemId}_to_{receiver.Id}",
                State = TransactionState.Pending,
                Timestamp = DateTime.UtcNow
            };
            if (!_transactions.TryAdd(operationId, tx))
            {
                return GiftShopItem(giver, receiver, shopItemId, quantity, operationId);
            }
        }

        if (!_shopItems.TryGetValue(shopItemId, out var itemDef))
        {
            if (tx != null)
            {
                lock (tx)
                {
                    tx.State = TransactionState.Failed;
                }
            }

            return new EconomyOperationResultDTO { Success = false, Message = "Item not found" };
        }

        var totalCost = itemDef.Price * quantity;

        // Debit Giver
        // HARDENING: Check Daily Gift Limit
        if (!giver.TryConsumeDailyGiftLimit(totalCost, DAILY_GIFT_LIMIT))
        {
            return new EconomyOperationResultDTO { Success = false, Message = "Daily gift limit exceeded" };
        }

        if (!giver.TryConsumePremiumCurrency(totalCost))
        {
            if (tx != null)
            {
                lock (tx)
                {
                    tx.State = TransactionState.Failed;
                }
            }

            return new EconomyOperationResultDTO { Success = false, Message = "Insufficient funds" };
        }

        var details = $"GiftShopItem:{shopItemId}, Item:{itemDef.ItemId} x{quantity}, To:{receiver.Id}";
        if (!string.IsNullOrEmpty(operationId))
        {
            details += $", OrderId:{operationId}";
        }

        // Add Item to Receiver (BindPolicy applies to Receiver)
        // Note: BindOnPickup becomes bound to Receiver.
        int? boundToId = null;
        if (itemDef.Policy == BindPolicy.BindOnPickup || itemDef.Policy == BindPolicy.CharacterBound)
        {
            boundToId = receiver.Id;
        }

        var added = receiver.AddItem(itemDef.ItemId, quantity, itemDef.Policy, boundToId);

        if (!added)
        {
            // Compensation: Refund Giver
            giver.AddPremiumCurrency(totalCost);

            if (tx != null)
            {
                lock (tx)
                {
                    tx.State = TransactionState.Failed;
                }
            }

            LogLedger("GiftBuyFailed", giver.Id, details + ", Reason:ReceiverInventoryFull", 0, giver.PremiumCurrency, traceId);

            return new EconomyOperationResultDTO { Success = false, Message = "Receiver inventory full" };
        }

        // Mark Transaction as Completed
        if (tx != null)
        {
            lock (tx)
            {
                tx.State = TransactionState.Completed;
            }
        }

        LogLedger("GiftBuy", giver.Id, details, -totalCost, giver.PremiumCurrency, traceId);

        return new EconomyOperationResultDTO
        {
            Success = true,
            Message = "Gift successful",
            NewBalance = giver.PremiumCurrency,
            OrderId = operationId
        };
    }

    private bool CheckRateLimit(int userId, int limit = 10, int windowSeconds = 60)
    {
        var now = DateTime.UtcNow;
        var tracker = _rateLimits.GetOrAdd(userId, _ => new RateLimitTracker { WindowStart = now, Count = 0 });

        lock (tracker)
        {
            if ((now - tracker.WindowStart).TotalSeconds > windowSeconds)
            {
                tracker.WindowStart = now;
                tracker.Count = 0;
            }

            if (tracker.Count >= limit)
            {
                return false;
            }

            tracker.Count++;
            return true;
        }
    }

    private DateTime? LoadSnapshot()
    {
        if (!File.Exists(_snapshotFile))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(_snapshotFile);
            var data = JsonSerializer.Deserialize<SnapshotData>(json);
            if (data == null)
            {
                return null;
            }

            foreach (var tx in data.Transactions)
            {
                _transactions[tx.OrderId] = tx;
            }

            Console.WriteLine($"[EconomyManager] Loaded snapshot from {data.Timestamp:O} with {data.Transactions.Count} transactions.");
            return data.Timestamp;
        }
        catch (System.Security.SecurityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EconomyManager] Failed to load snapshot: {ex.Message}");
            return null;
        }
    }

    private void SaveSnapshot()
    {
        try
        {
            var data = new SnapshotData
            {
                Timestamp = DateTime.UtcNow,
                Transactions = _transactions.Values.ToList()
            };
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            var tmp = _snapshotFile + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, _snapshotFile, true);
            Console.WriteLine($"[EconomyManager] Saved snapshot with {_transactions.Count} transactions.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EconomyManager] Failed to save snapshot: {ex.Message}");
        }
    }

    private void ReplayLedger(DateTime? startTime = null)
    {
        var sw = Stopwatch.StartNew();
        var count = 0;
        try
        {
            // Verify Integrity while replaying
            if (!VerifyLedgerIntegrity(out var lastHash))
            {
                throw new System.Security.SecurityException("Ledger integrity check failed! Tampering detected.");
            }

            // Set internal state to the last known valid hash to continue chain
            lock (_ledgerLock)
            {
                _lastHash = lastHash;
            }

            var lines = File.ReadLines(_ledgerFile);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("Timestamp"))
                {
                    continue;
                }

                var match = _ledgerRegex.Match(line);
                if (!match.Success)
                {
                    continue;
                }

                var timestampStr = match.Groups[1].Value;
                var type = match.Groups[2].Value;
                var userIdStr = match.Groups[3].Value;
                var details = match.Groups[4].Value;
                // Groups[7] is PrevHash, Groups[8] is Hash (if present)

                var timestamp = DateTime.TryParse(timestampStr, null, DateTimeStyles.RoundtripKind, out var ts)
                    ? ts
                    : DateTime.UtcNow;

                if (startTime.HasValue && timestamp <= startTime.Value)
                {
                    continue;
                }

                string? orderId = null;
                string? productId = null;

                if (type == "ShopBuy")
                {
                    // Details: ShopItem:X, Item:Y xZ, OrderId:ABC
                    var split = details.Split(", OrderId:");
                    if (split.Length > 1)
                    {
                        orderId = split[1].Trim();
                    }
                }
                else if (type == "PurchaseIntent" || type == "PurchaseVerify")
                {
                    // Details: Order:ABC, Product:XYZ
                    var orderSplit = details.Split(", Product:");
                    if (orderSplit.Length > 0)
                    {
                        var firstPart = orderSplit[0]; // Order:ABC
                        if (firstPart.StartsWith("Order:"))
                        {
                            orderId = firstPart.Substring(6).Trim();
                        }
                    }

                    if (orderSplit.Length > 1)
                    {
                        productId = orderSplit[1].Trim();
                    }
                }

                if (!string.IsNullOrEmpty(orderId))
                {
                    var userId = int.TryParse(userIdStr, out var uid) ? uid : 0;

                    if (type == "PurchaseIntent")
                    {
                        if (!_transactions.ContainsKey(orderId!))
                        {
                            var tx = new Transaction
                            {
                                OrderId = orderId!,
                                UserId = userId,
                                ProductId = productId ?? "unknown",
                                State = TransactionState.Pending,
                                Timestamp = timestamp
                            };
                            _transactions[orderId!] = tx;
                        }
                    }
                    else if (type == "PurchaseVerify" || type == "ShopBuy")
                    {
                        // Mark as Completed
                        if (_transactions.TryGetValue(orderId!, out var tx))
                        {
                            tx.State = TransactionState.Completed;
                        }
                        else
                        {
                            tx = new Transaction
                            {
                                OrderId = orderId!,
                                UserId = userId,
                                ProductId = productId ?? "unknown",
                                State = TransactionState.Completed,
                                Timestamp = timestamp
                            };
                            _transactions[orderId!] = tx;
                        }

                        count++;
                    }
                }
            }
        }
        catch (System.Security.SecurityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EconomyManager] Error replaying ledger: {ex.Message}");
        }

        sw.Stop();
        Metrics.ReplayedTransactionCount = count;
        Metrics.ReplayDurationMs = sw.ElapsedMilliseconds;
        Console.WriteLine($"[EconomyManager] Replayed {count} transactions in {sw.ElapsedMilliseconds}ms.");
    }

    public bool VerifyLedgerIntegrity() => VerifyLedgerIntegrity(out _);

    public bool VerifyLedgerIntegrity(out string lastHash)
    {
        lastHash = "0000000000000000000000000000000000000000000000000000000000000000";
        if (!File.Exists(_ledgerFile))
        {
            return true;
        }

        try
        {
            var lines = File.ReadLines(_ledgerFile);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("Timestamp"))
                {
                    continue;
                }

                var match = _ledgerRegex.Match(line);
                if (!match.Success)
                {
                    // Malformed line
                    return false;
                }

                // If line has hash info
                if (match.Groups[7].Success && match.Groups[8].Success)
                {
                    var content = $"{match.Groups[1].Value},{match.Groups[2].Value},{match.Groups[3].Value},{match.Groups[4].Value},{match.Groups[5].Value},{match.Groups[6].Value}";
                    var claimedPrev = match.Groups[7].Value;
                    var claimedHash = match.Groups[8].Value;

                    if (claimedPrev != lastHash)
                    {
                        // Broken chain
                        return false;
                    }

                    using var sha = System.Security.Cryptography.SHA256.Create();
                    var actualHash = Convert.ToHexString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content + claimedPrev)));

                    if (actualHash != claimedHash)
                    {
                        // Tampering detected
                        return false;
                    }

                    lastHash = actualHash;
                }
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task ProcessLogQueue()
    {
        try
        {
            using var stream =
                new FileStream(_ledgerFile, FileMode.Append, FileAccess.Write, FileShare.Read, 4096, true);
            using var writer = new StreamWriter(stream) { AutoFlush = false };

            while (await _logChannel.Reader.WaitToReadAsync())
            {
                while (_logChannel.Reader.TryRead(out var line))
                {
                    await writer.WriteAsync(line);
                }

                await writer.FlushAsync();
            }
        }
        catch (Exception ex)
        {
            // Log to console if the background writer dies
            Console.Error.WriteLine($"[CRITICAL] EconomyManager Log Writer Failed: {ex}");
        }
    }

    public EconomyOperationResultDTO BuyShopItem(ServerCharacter character, int shopItemId, int quantity) =>
        BuyShopItem(character, shopItemId, quantity, null, null);

    public string GenerateSignature(string orderId)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(_providerSecret));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(orderId));
        return Convert.ToHexString(hash);
    }

    private bool ValidateReceipt(string receiptToken, string orderId)
    {
        // FAIL CLOSED: Strict validation
        if (string.IsNullOrWhiteSpace(receiptToken) || string.IsNullOrWhiteSpace(orderId))
        {
            return false;
        }

        var expected = GenerateSignature(orderId);
        // Constant-time comparison to prevent timing attacks
        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(receiptToken),
            System.Text.Encoding.UTF8.GetBytes(expected));
    }

    private void LogLedger(string type, int userId, string details, long delta, long newBalance, string? traceId = null)
    {
        string line;
        lock (_ledgerLock)
        {
            var content = $"{DateTime.UtcNow:O},{type},{userId},{details},{delta},{newBalance}";
            using var sha = System.Security.Cryptography.SHA256.Create();
            var hash = Convert.ToHexString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content + _lastHash)));

            line = $"{content},{_lastHash},{hash}\n";
            _lastHash = hash;
        }

        // Async Logging
        _logChannel.Writer.TryWrite(line);

        // Audit via SecurityLogger as well
        SecurityLogger.LogSecurityEvent($"Economy_{type}", userId,
            $"{details} | Delta:{delta} NewBalance:{newBalance}", traceId);
    }

    private void TryCleanup()
    {
        if (Interlocked.Increment(ref _cleanupCounter) % CLEANUP_INTERVAL == 0)
        {
            if (Interlocked.CompareExchange(ref _isCleaningUp, 1, 0) == 0)
            {
                Task.Run(() =>
                {
                    try
                    {
                        var now = DateTime.UtcNow;
                        foreach (var kvp in _transactions)
                        {
                            if ((now - kvp.Value.Timestamp).TotalMinutes > EXPIRATION_MINUTES)
                            {
                                _transactions.TryRemove(kvp.Key, out _);
                            }
                        }
                    }
                    finally
                    {
                        Interlocked.Exchange(ref _isCleaningUp, 0);
                    }
                });
            }
        }
    }

    private enum TransactionState
    {
        Pending,
        Completed,
        Failed
    }

    private class Transaction
    {
        public string OrderId { get; set; }
        public int UserId { get; set; }
        public string ProductId { get; set; }
        public TransactionState State { get; set; }
        public DateTime Timestamp { get; set; }
    }

    private class RateLimitTracker
    {
        public int Count { get; set; }
        public DateTime WindowStart { get; set; }
    }

    private class SnapshotData
    {
        public DateTime Timestamp { get; set; }
        public List<Transaction> Transactions { get; set; } = new();
    }

    public class EconomyMetrics
    {
        private readonly EconomyManager _manager;

        public EconomyMetrics(EconomyManager manager)
        {
            _manager = manager;
        }

        public int ReplayedTransactionCount { get; set; }
        public long ReplayDurationMs { get; set; }
        public int ActiveTransactions => _manager._transactions.Count;
        public int PendingTransactions => _manager._transactions.Values.Count(t => t.State == TransactionState.Pending);
    }
}