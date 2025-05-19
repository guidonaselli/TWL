namespace TWL.Shared.Domain.Characters;

/// <summary>
///     Represents a slot in the inventory holding an item and a quantity.
/// </summary>
public class ItemSlot
{
    public int ItemId;
    public int Quantity;

    public ItemSlot(int itemId, int quantity)
    {
        ItemId = itemId;
        Quantity = quantity;
    }
}