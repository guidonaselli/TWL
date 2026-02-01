using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Channels;
using System.Threading.Tasks;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.DTO;
using TWL.Shared.Domain.Models;
using TWL.Server.Security;

namespace TWL.Server.Simulation.Managers;

public class EconomyManager : IEconomyService, IDisposable
{
    private readonly string _ledgerFile;
    private readonly Channel<string> _logChannel;
    private readonly Task _logTask;

    // Mock Data
    private readonly Dictionary<string, long> _productPrices = new()
    {
        { "gems_100", 100 },
        { "gems_500", 500 },
        { "gems_1000", 1000 }
    };

    private readonly Dictionary<int, (long Price, int ItemId, BindPolicy Policy)> _shopItems = new()
    {
        { 1, (10, 101, BindPolicy.Unbound) }, // ShopItem 1: 10 Gems -> Item 101 (Potion)
        { 2, (50, 102, BindPolicy.BindOnEquip) }, // ShopItem 2: 50 Gems -> Item 102 (Sword)
        { 3, (100, 103, BindPolicy.BindOnPickup) } // ShopItem 3: 100 Gems -> Item 103 (Armor)
    };

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

    private readonly ConcurrentDictionary<string, Transaction> _transactions = new();
    private readonly ConcurrentDictionary<int, RateLimitTracker> _rateLimits = new();

    private class RateLimitTracker
    {
        public int Count { get; set; }
        public DateTime WindowStart { get; set; }
    }

    private readonly object _ledgerLock = new();
    private int _cleanupCounter = 0;
    private const int CLEANUP_INTERVAL = 100;
    private const double EXPIRATION_MINUTES = 10.0;

    private static readonly System.Text.RegularExpressions.Regex _ledgerRegex = new(
        @"^([^,]+),([^,]+),([^,]+),(.+),([^,]+),([^,]+)$",
        System.Text.RegularExpressions.RegexOptions.Compiled);

    public class EconomyMetrics
    {
        public int ReplayedTransactionCount { get; set; }
        public long ReplayDurationMs { get; set; }
    }

    public EconomyMetrics Metrics { get; } = new();

    public EconomyManager(string ledgerFile = "economy_ledger.log")
    {
        _ledgerFile = ledgerFile;
        // Ensure ledger exists
        if (!File.Exists(_ledgerFile))
        {
            File.WriteAllText(_ledgerFile, "Timestamp,Type,UserId,Details,Delta,NewBalance\n");
        }
        else
        {
            ReplayLedger();
        }

        // Initialize Async Logging
        _logChannel = Channel.CreateUnbounded<string>();
        _logTask = Task.Run(ProcessLogQueue);
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

    private void ReplayLedger()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        int count = 0;
        try
        {
            var lines = File.ReadLines(_ledgerFile);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("Timestamp")) continue;

                var match = _ledgerRegex.Match(line);
                if (!match.Success) continue;

                var timestampStr = match.Groups[1].Value;
                var type = match.Groups[2].Value;
                var userIdStr = match.Groups[3].Value;
                var details = match.Groups[4].Value;

                DateTime timestamp = DateTime.TryParse(timestampStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var ts) ? ts : DateTime.UtcNow;

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
                    int userId = int.TryParse(userIdStr, out var uid) ? uid : 0;

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
        catch (Exception ex)
        {
            Console.WriteLine($"[EconomyManager] Error replaying ledger: {ex.Message}");
        }

        sw.Stop();
        Metrics.ReplayedTransactionCount = count;
        Metrics.ReplayDurationMs = sw.ElapsedMilliseconds;
        Console.WriteLine($"[EconomyManager] Replayed {count} transactions in {sw.ElapsedMilliseconds}ms.");
    }

    private async Task ProcessLogQueue()
    {
        try
        {
            using var stream = new FileStream(_ledgerFile, FileMode.Append, FileAccess.Write, FileShare.Read, 4096, true);
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

    public void Dispose()
    {
        try
        {
            _logChannel.Writer.TryComplete();
        }
        catch { }

        try
        {
            // Wait gracefully for the task to finish writing remaining logs.
            // We use a timeout to prevent hanging the process indefinitely.
            _logTask.Wait(2000);
        }
        catch { }
    }

    public PurchaseGemsIntentResponseDTO InitiatePurchase(int userId, string productId)
    {
        if (!CheckRateLimit(userId)) return null;

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

        LogLedger("PurchaseIntent", userId, $"Order:{orderId}, Product:{productId}", 0, 0);

        return new PurchaseGemsIntentResponseDTO
        {
            OrderId = orderId,
            ProviderUrl = $"https://mock-provider.com/pay?order={orderId}"
        };
    }

    public EconomyOperationResultDTO VerifyPurchase(int userId, string orderId, string receiptToken, ServerCharacter character)
    {
        if (!CheckRateLimit(userId))
        {
            SecurityLogger.LogSecurityEvent("PurchaseVerifyFailed", userId, "Reason:RateLimitExceeded");
            return new EconomyOperationResultDTO { Success = false, Message = "Rate limit exceeded" };
        }

        if (!_transactions.TryGetValue(orderId, out var tx))
        {
            SecurityLogger.LogSecurityEvent("PurchaseVerifyFailed", userId, $"Reason:OrderNotFound OrderId:{orderId}");
            return new EconomyOperationResultDTO { Success = false, Message = "Order not found" };
        }

        if (tx.UserId != userId)
        {
            SecurityLogger.LogSecurityEvent("PurchaseVerifyFailed", userId, $"Reason:UserMismatch OrderId:{orderId} TxUser:{tx.UserId}");
            return new EconomyOperationResultDTO { Success = false, Message = "User mismatch" };
        }

        lock (tx)
        {
            if (tx.State == TransactionState.Completed)
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
                SecurityLogger.LogSecurityEvent("PurchaseVerifyFailed", userId, $"Reason:InvalidState State:{tx.State}");
                return new EconomyOperationResultDTO { Success = false, Message = "Invalid state" };
            }

            // Verify receipt (Mock Signature Validation)
            if (!ValidateReceipt(receiptToken, orderId))
            {
                SecurityLogger.LogSecurityEvent("PurchaseVerifyFailed", userId, "Reason:InvalidSignature");
                return new EconomyOperationResultDTO { Success = false, Message = "Invalid receipt signature" };
            }

            // Credit Gems
            long gemsToAdd = _productPrices[tx.ProductId];
            character.AddPremiumCurrency(gemsToAdd);

            tx.State = TransactionState.Completed;

            LogLedger("PurchaseVerify", userId, $"Order:{orderId}, Product:{tx.ProductId}", gemsToAdd, character.PremiumCurrency);

            return new EconomyOperationResultDTO
            {
                Success = true,
                Message = "Purchase successful",
                NewBalance = character.PremiumCurrency,
                OrderId = orderId
            };
        }
    }

    public EconomyOperationResultDTO BuyShopItem(ServerCharacter character, int shopItemId, int quantity)
    {
        return BuyShopItem(character, shopItemId, quantity, null);
    }

    public EconomyOperationResultDTO BuyShopItem(ServerCharacter character, int shopItemId, int quantity, string? operationId)
    {
        if (!CheckRateLimit(character.Id))
        {
            SecurityLogger.LogSecurityEvent("ShopBuyFailed", character.Id, "Reason:RateLimitExceeded");
            return new EconomyOperationResultDTO { Success = false, Message = "Rate limit exceeded" };
        }

        if (quantity <= 0 || quantity > 999)
            return new EconomyOperationResultDTO { Success = false, Message = "Invalid quantity (1-999)" };

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
                    else if (tx.State == TransactionState.Pending)
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
            if (tx != null) { lock (tx) tx.State = TransactionState.Failed; }
            SecurityLogger.LogSecurityEvent("ShopBuyFailed", character.Id, $"Reason:ItemNotFound Item:{shopItemId}");
            return new EconomyOperationResultDTO { Success = false, Message = "Item not found" };
        }

        long totalCost = itemDef.Price * quantity;

        if (!character.TryConsumePremiumCurrency(totalCost))
        {
            if (tx != null) { lock (tx) tx.State = TransactionState.Failed; }
            SecurityLogger.LogSecurityEvent("ShopBuyFailed", character.Id, $"Reason:InsufficientFunds Cost:{totalCost} Has:{character.PremiumCurrency}");
            return new EconomyOperationResultDTO { Success = false, Message = "Insufficient funds" };
        }

        var details = $"ShopItem:{shopItemId}, Item:{itemDef.ItemId} x{quantity}";
        if (!string.IsNullOrEmpty(operationId)) details += $", OrderId:{operationId}";

        // Add Item with Policy
        bool added = character.AddItem(itemDef.ItemId, quantity, itemDef.Policy);

        if (!added)
        {
            // Compensation: Refund
            character.AddPremiumCurrency(totalCost);

            if (tx != null)
            {
                lock (tx) tx.State = TransactionState.Failed;
            }

            LogLedger("ShopBuyFailed", character.Id, details + ", Reason:InventoryFull", 0, character.PremiumCurrency);

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

        LogLedger("ShopBuy", character.Id, details, -totalCost, character.PremiumCurrency);

        return new EconomyOperationResultDTO
        {
            Success = true,
            Message = "Purchase successful",
            NewBalance = character.PremiumCurrency,
            OrderId = operationId
        };
    }

    public EconomyOperationResultDTO GiftShopItem(ServerCharacter giver, ServerCharacter receiver, int shopItemId, int quantity, string operationId)
    {
        if (giver == null || receiver == null)
            return new EconomyOperationResultDTO { Success = false, Message = "Invalid participants" };

        if (string.IsNullOrEmpty(operationId))
            return new EconomyOperationResultDTO { Success = false, Message = "OperationId is required for gifting" };

        if (quantity <= 0 || quantity > 999)
            return new EconomyOperationResultDTO { Success = false, Message = "Invalid quantity (1-999)" };

        if (!CheckRateLimit(giver.Id))
            return new EconomyOperationResultDTO { Success = false, Message = "Rate limit exceeded" };

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
                else if (tx.State == TransactionState.Pending)
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
            if (tx != null) { lock (tx) tx.State = TransactionState.Failed; }
            return new EconomyOperationResultDTO { Success = false, Message = "Item not found" };
        }

        long totalCost = itemDef.Price * quantity;

        // Debit Giver
        if (!giver.TryConsumePremiumCurrency(totalCost))
        {
            if (tx != null) { lock (tx) tx.State = TransactionState.Failed; }
            return new EconomyOperationResultDTO { Success = false, Message = "Insufficient funds" };
        }

        var details = $"GiftShopItem:{shopItemId}, Item:{itemDef.ItemId} x{quantity}, To:{receiver.Id}";
        if (!string.IsNullOrEmpty(operationId)) details += $", OrderId:{operationId}";

        // Add Item to Receiver (BindPolicy applies to Receiver)
        // Note: BindOnPickup becomes bound to Receiver.
        int? boundToId = null;
        if (itemDef.Policy == BindPolicy.BindOnPickup || itemDef.Policy == BindPolicy.CharacterBound)
        {
            boundToId = receiver.Id;
        }

        bool added = receiver.AddItem(itemDef.ItemId, quantity, itemDef.Policy, boundToId);

        if (!added)
        {
            // Compensation: Refund Giver
            giver.AddPremiumCurrency(totalCost);

            if (tx != null)
            {
                lock (tx) tx.State = TransactionState.Failed;
            }

            LogLedger("GiftBuyFailed", giver.Id, details + ", Reason:ReceiverInventoryFull", 0, giver.PremiumCurrency);

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

        LogLedger("GiftBuy", giver.Id, details, -totalCost, giver.PremiumCurrency);

        return new EconomyOperationResultDTO
        {
            Success = true,
            Message = "Gift successful",
            NewBalance = giver.PremiumCurrency,
            OrderId = operationId
        };
    }

    private bool ValidateReceipt(string receiptToken, string orderId)
    {
        // In a real app, this would verify a cryptographic signature.
        // For this hardening task, we enforce a strict format linked to the orderId.
        // FAIL CLOSED: Strict validation
        if (string.IsNullOrWhiteSpace(receiptToken) || string.IsNullOrWhiteSpace(orderId)) return false;

        return receiptToken == $"mock_sig_{orderId}";
    }

    private void LogLedger(string type, int userId, string details, long delta, long newBalance)
    {
        var line = $"{DateTime.UtcNow:O},{type},{userId},{details},{delta},{newBalance}\n";

        // Async Logging
        _logChannel.Writer.TryWrite(line);

        // Audit via SecurityLogger as well
        SecurityLogger.LogSecurityEvent($"Economy_{type}", userId, $"{details} | Delta:{delta} NewBalance:{newBalance}");
    }

    private void TryCleanup()
    {
        if (System.Threading.Interlocked.Increment(ref _cleanupCounter) % CLEANUP_INTERVAL == 0)
        {
            // Simple cleanup: fire and forget or run inline? Inline is safer for now.
            // Using Task.Run might be better for latency but let's keep it simple.
            var now = DateTime.UtcNow;
            foreach (var kvp in _transactions)
            {
                if ((now - kvp.Value.Timestamp).TotalMinutes > EXPIRATION_MINUTES)
                {
                    _transactions.TryRemove(kvp.Key, out _);
                }
            }
        }
    }
}
