using TWL.Shared.Domain.Models;

namespace TWL.Shared.Domain.Requests;

public class InventoryUpdate
{
    public InventoryUpdate()
    {
        Items = new List<Item>();
    }

    public int PlayerId { get; set; }
    public List<Item> Items { get; set; }
}