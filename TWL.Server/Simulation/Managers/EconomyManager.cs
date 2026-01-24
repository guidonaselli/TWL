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

            // Verify receipt (Mock Signature Validation)
            if (!ValidateReceipt(receiptToken, orderId))
            {
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
        if (quantity <= 0 || quantity > 999)
            return new EconomyOperationResultDTO { Success = false, Message = "Invalid quantity (1-999)" };

        if (!_shopItems.TryGetValue(shopItemId, out var itemDef))
        {
            return new EconomyOperationResultDTO { Success = false, Message = "Item not found" };
        }

        long totalCost = itemDef.Price * quantity;

        if (!character.TryConsumePremiumCurrency(totalCost))
        {
            return new EconomyOperationResultDTO { Success = false, Message = "Insufficient funds" };
        }

        // Add Item
        character.AddItem(itemDef.ItemId, quantity);

        LogLedger("ShopBuy", character.Id, $"ShopItem:{shopItemId}, Item:{itemDef.ItemId} x{quantity}", -totalCost, character.PremiumCurrency);

        return new EconomyOperationResultDTO
        {
            Success = true,
            Message = "Purchase successful",
            NewBalance = character.PremiumCurrency
        };
    }

    private bool ValidateReceipt(string receiptToken, string orderId)
    {
        // In a real app, this would verify a cryptographic signature.
        // For this hardening task, we enforce a strict format linked to the orderId.
        return !string.IsNullOrEmpty(receiptToken) && receiptToken == $"mock_sig_{orderId}";
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
            catch (Exception ex)
            {
                // Fallback logging to console so we don't lose the audit trail completely
                Console.Error.WriteLine($"[CRITICAL] LEDGER WRITE FAILED: {ex.Message} | Data: {line}");
            }
        }
    }
}
