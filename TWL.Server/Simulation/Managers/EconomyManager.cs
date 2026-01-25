using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.DTO;

namespace TWL.Server.Simulation.Managers;

public class EconomyManager
{
    private const string LEDGER_FILE = "economy_ledger.log";

    // Mock Data
    private readonly Dictionary<string, long> _productPrices = new()
    {
        { "gems_100", 100 },
        { "gems_500", 500 },
        { "gems_1000", 1000 }
    };

    private readonly Dictionary<int, (long Price, int ItemId)> _shopItems = new()
    {
        { 1, (10, 101) }, // ShopItem 1: 10 Gems -> Item 101 (Potion)
        { 2, (50, 102) }, // ShopItem 2: 50 Gems -> Item 102 (Sword)
        { 3, (100, 103) } // ShopItem 3: 100 Gems -> Item 103 (Armor)
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
    private readonly object _ledgerLock = new();
    private int _cleanupCounter = 0;
    private const int CLEANUP_INTERVAL = 100;
    private const double EXPIRATION_MINUTES = 10.0;

    public EconomyManager()
    {
        // Ensure ledger exists
        if (!File.Exists(LEDGER_FILE))
        {
            File.WriteAllText(LEDGER_FILE, "Timestamp,Type,UserId,Details,Delta,NewBalance\n");
        }
    }

    public PurchaseGemsIntentResponseDTO InitiatePurchase(int userId, string productId)
    {
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
        if (!_transactions.TryGetValue(orderId, out var tx))
        {
             // Idempotency check: if it was completed and removed?
             // Ideally we keep completed ones for a while.
             // For now, assume if not found, it's invalid or expired.
             return new EconomyOperationResultDTO { Success = false, Message = "Order not found" };
        }

        if (tx.UserId != userId)
        {
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
                return new EconomyOperationResultDTO { Success = false, Message = "Invalid state" };
            }

            // Verify receipt (Mock)
            if (string.IsNullOrEmpty(receiptToken))
            {
                return new EconomyOperationResultDTO { Success = false, Message = "Invalid receipt" };
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

    public EconomyOperationResultDTO BuyShopItem(ServerCharacter character, int shopItemId, int quantity, string operationId = null)
    {
        TryCleanup();

        if (quantity <= 0) return new EconomyOperationResultDTO { Success = false, Message = "Invalid quantity" };

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
            return new EconomyOperationResultDTO { Success = false, Message = "Item not found" };
        }

        long totalCost = itemDef.Price * quantity;

        if (!character.TryConsumePremiumCurrency(totalCost))
        {
            if (tx != null) { lock (tx) tx.State = TransactionState.Failed; }
            return new EconomyOperationResultDTO { Success = false, Message = "Insufficient funds" };
        }

        // Add Item
        character.AddItem(itemDef.ItemId, quantity);

        // Mark Transaction as Completed
        if (tx != null)
        {
            lock (tx)
            {
                tx.State = TransactionState.Completed;
            }
        }

        var details = $"ShopItem:{shopItemId}, Item:{itemDef.ItemId} x{quantity}";
        if (!string.IsNullOrEmpty(operationId)) details += $", OrderId:{operationId}";

        LogLedger("ShopBuy", character.Id, details, -totalCost, character.PremiumCurrency);

        return new EconomyOperationResultDTO
        {
            Success = true,
            Message = "Purchase successful",
            NewBalance = character.PremiumCurrency,
            OrderId = operationId
        };
    }

    private void LogLedger(string type, int userId, string details, long delta, long newBalance)
    {
        var line = $"{DateTime.UtcNow:O},{type},{userId},{details},{delta},{newBalance}\n";
        lock (_ledgerLock)
        {
            try
            {
                File.AppendAllText(LEDGER_FILE, line);
            }
            catch
            {
                // Ignore logging errors to prevent crash, but this is bad for audit.
            }
        }
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
