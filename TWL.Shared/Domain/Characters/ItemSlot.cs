using TWL.Shared.Domain.Models;

namespace TWL.Shared.Domain.Characters;

/// <summary>
///     Represents a slot in the inventory holding an item and a quantity.
/// </summary>
public class ItemSlot
{
    public int? BoundToId;
    public int ItemId;
    public BindPolicy Policy;
    public int Quantity;

    public ItemSlot(int itemId, int quantity, BindPolicy policy = BindPolicy.Unbound, int? boundToId = null)
    {
        ItemId = itemId;
        Quantity = quantity;
        Policy = policy;
        BoundToId = boundToId;
    }
}