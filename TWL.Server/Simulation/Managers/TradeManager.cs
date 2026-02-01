using TWL.Server.Security;
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

            // Attempt to remove specifically this policy type
            if (source.RemoveItem(itemId, toTake, item.Policy))
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
        foreach (var m in movedItems)
        {
            target.AddItem(m.ItemId, m.Qty, m.Policy, m.BoundToId);
        }

        SecurityLogger.LogSecurityEvent("TradeSuccess", source.Id, $"To:{target.Id} Item:{itemId} Qty:{quantity}");
        source.NotifyTradeCommitted(target, itemId, quantity);
        return true;
    }
}