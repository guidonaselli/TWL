using TWL.Server.Persistence.Database;
using TWL.Server.Security;
using TWL.Server.Security.Idempotency;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Models;

namespace TWL.Server.Simulation.Managers;

public class TradeManager
{
    public bool ValidateTransfer(Item item, int targetUserId, int sourceUserId)
    {
        // HARDENING: If item is already bound to a specific ID, strict rules apply.
        if (item.BoundToId.HasValue && item.BoundToId.Value != 0)
        {
            // AccountBound allows transfer within same account (here checked via ID match or similar logic)
            if (item.Policy == BindPolicy.AccountBound)
            {
                return targetUserId == sourceUserId;
            }

            // All other bound items (BoP, CharacterBound, or equipped BoE) are strictly non-transferable
            return false;
        }

        switch (item.Policy)
        {
            case BindPolicy.Unbound:
                return true;
            case BindPolicy.BindOnEquip:
                // Treat as unbound for transfer if in inventory AND NOT BOUND (checked above)
                return true;
            case BindPolicy.AccountBound:
                // Only allow if target is the same account (e.g. storage transfer)
                // Since this method handles "Trade" between entities, rejection is correct if users differ.
                return targetUserId == sourceUserId;
            case BindPolicy.BindOnPickup:
            case BindPolicy.CharacterBound:
            default:
                return false;
        }
    }

    public bool TransferItem(ServerCharacter source, ServerCharacter target, int itemId, int quantity,
        BindPolicy? policyFilter = null)
    {
        if (source == null || target == null)
        {
            return false;
        }

        if (quantity <= 0)
        {
            return false;
        }

        // Prevent self-transfer (unless we want to support stacking, but typically trade is 2 parties)
        if (source.Id == target.Id)
        {
            SecurityLogger.LogSecurityEvent("TradeSelfTransferAttempt", source.Id, $"ItemId:{itemId}");
            return false;
        }

        // 1. Locate Item(s)
        var items = source.GetItems(itemId, policyFilter);
        long available = 0;
        foreach (var i in items)
        {
            available += i.Quantity;
        }

        if (available < quantity)
        {
            SecurityLogger.LogSecurityEvent("TradeInsufficientFunds", source.Id,
                $"ItemId:{itemId} Want:{quantity} Have:{available}");
            return false;
        }

        // 2. Validate Bind Policy for candidates
        var tradableItems = new List<Item>();
        long tradableQty = 0;

        foreach (var item in items)
        {
            if (ValidateTransfer(item, target.Id, source.Id))
            {
                tradableItems.Add(item);
                tradableQty += item.Quantity;
            }
        }

        if (tradableQty < quantity)
        {
            SecurityLogger.LogSecurityEvent("TradeBoundItemAttempt", source.Id,
                $"ItemId:{itemId} Tradable:{tradableQty} Want:{quantity}");
            return false;
        }

        // 3. Pre-Validation: Can Target Accept? (Optimistic)
        // Hardening: Check if target has space for AT LEAST the first batch to avoid unnecessary rollbacks
        // This is heuristic because fragmentation might vary, but for single item type transfer it is reasonably accurate.
        // We assume we transfer 'quantity' of 'itemId'.
        // Since we might be transferring multiple stacks (some bound, some unbound), this is complex.
        // We do a simple check: Can target accept 1 unit?
        if (!target.CanAddItem(itemId, 1, tradableItems[0].Policy, tradableItems[0].BoundToId))
        {
            SecurityLogger.LogSecurityEvent("TradeTargetFull", source.Id, "Target inventory full (pre-check).");
            return false;
        }

        // 3. Execute Transfer (Atomic-ish)
        var remaining = quantity;
        var movedItems = new List<(int ItemId, int Qty, BindPolicy Policy, int? BoundToId)>();

        foreach (var item in tradableItems)
        {
            if (remaining <= 0)
            {
                break;
            }

            var toTake = Math.Min(item.Quantity, remaining);

            // HARDENING: Use RemoveItemExact to prevent laundering bound items
            // We use the properties from the validated 'item' object (Policy and BoundToId)
            if (source.RemoveItemExact(itemId, toTake, item.Policy, item.BoundToId))
            {
                movedItems.Add((itemId, toTake, item.Policy, item.BoundToId));
                remaining -= toTake;
            }
        }

        if (remaining > 0)
        {
            // ROLLBACK if we couldn't get enough (e.g. concurrency race)
            foreach (var m in movedItems)
            {
                source.AddItem(m.ItemId, m.Qty, m.Policy, m.BoundToId);
            }

            SecurityLogger.LogSecurityEvent("TradeConcurrencyFailure", source.Id, "Failed to lock items.");
            return false;
        }

        // 4. Add to Target
        var addedToTarget = new List<(int ItemId, int Qty, BindPolicy Policy, int? BoundToId)>();
        var success = true;

        foreach (var m in movedItems)
        {
            if (!target.AddItem(m.ItemId, m.Qty, m.Policy, m.BoundToId))
            {
                success = false;
                break;
            }
            addedToTarget.Add(m);
        }

        if (!success)
        {
            // ROLLBACK Target (Remove what was just added)
            // Use RemoveItemExact for rollback too to be safe
            foreach (var m in addedToTarget)
            {
                target.RemoveItemExact(m.ItemId, m.Qty, m.Policy, m.BoundToId);
            }

            // ROLLBACK Source (Add back what was taken)
            foreach (var m in movedItems)
            {
                source.AddItem(m.ItemId, m.Qty, m.Policy, m.BoundToId);
            }

            SecurityLogger.LogSecurityEvent("TradeTargetFull", source.Id, "Target inventory full or rejected item.");
            return false;
        }

        SecurityLogger.LogSecurityEvent("TradeSuccess", source.Id, $"To:{target.Id} Item:{itemId} Qty:{quantity}");
        source.NotifyTradeCommitted(target, itemId, quantity);
        return true;
    }

    /// <summary>
    /// Executes a trade within a Serializable transaction boundary, enforcing idempotency via a shared validator.
    /// This provides a reusable pattern for market and guild-bank integrations.
    /// </summary>
    public async Task<bool> TransferItemAsync(
        DbService db, 
        IdempotencyValidator idempotencyValidator, 
        string operationKey, 
        ServerCharacter source, 
        ServerCharacter target, 
        int itemId, 
        int quantity, 
        BindPolicy? policyFilter = null)
    {
        if (!idempotencyValidator.TryRegisterOperation(operationKey, source.Id, out var existingRecord))
        {
            // If already completed, return true idempotently. If pending/failed, return false (or retry strategy).
            return existingRecord.State == OperationState.Completed;
        }

        try
        {
            var result = await db.ExecuteSerializableAsync(async (con, tx) =>
            {
                var success = TransferItem(source, target, itemId, quantity, policyFilter);
                if (success)
                {
                    // Emit audit details containing operation identity and correlation metadata
                    SecurityLogger.LogSecurityEvent("TradeValuableCommitted", source.Id, 
                        $"OperationKey:{operationKey} To:{target.Id} Item:{itemId} Qty:{quantity}");
                }
                
                // We return the memory manipulation success state
                return await Task.FromResult(success);
            });

            if (result)
            {
                idempotencyValidator.MarkCompleted(operationKey);
                return true;
            }
            else
            {
                idempotencyValidator.MarkFailed(operationKey);
                return false;
            }
        }
        catch (Exception ex)
        {
            idempotencyValidator.MarkFailed(operationKey);
            SecurityLogger.LogSecurityEvent("TradeValuableError", source.Id, $"OperationKey:{operationKey} Error:{ex.Message}");
            return false;
        }
    }
}