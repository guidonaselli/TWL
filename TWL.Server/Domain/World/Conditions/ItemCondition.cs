using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Networking;

namespace TWL.Server.Domain.World.Conditions;

public class ItemCondition : ITriggerCondition
{
    public int ItemId { get; }
    public int Quantity { get; }

    public ItemCondition(int itemId, int quantity)
    {
        ItemId = itemId;
        Quantity = quantity;
    }

    public bool IsMet(ServerCharacter character, PlayerService playerService)
    {
        return character.HasItem(ItemId, Quantity);
    }
}
