using TWL.Server.Domain.World;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Models;

namespace TWL.Server.Services.World.Actions.Handlers;

public class GiveItemActionHandler : ITriggerActionHandler
{
    public string ActionType => "GiveItem";

    public void Execute(ServerCharacter character, TriggerAction action)
    {
        if (action.Parameters.TryGetValue("ItemId", out var iidStr) && int.TryParse(iidStr, out var iid))
        {
            var count = 1;
            if (action.Parameters.TryGetValue("Count", out var countStr) && int.TryParse(countStr, out var c))
            {
                count = c;
            }

            var policy = BindPolicy.Unbound;
            if (action.Parameters.TryGetValue("Policy", out var polStr) && Enum.TryParse<BindPolicy>(polStr, out var pol))
            {
                policy = pol;
            }

            int? boundTo = null;
            if (action.Parameters.TryGetValue("BoundTo", out var boundStr) && int.TryParse(boundStr, out var boundId))
            {
                boundTo = boundId;
            }

            if (character.CanAddItem(iid, count, policy, boundTo))
            {
                character.AddItem(iid, count, policy, boundTo);
            }
            else
            {
                Console.WriteLine($"[Trigger] Failed to give item {iid} x{count} to {character.Name} (Inventory Full)");
            }
        }
    }
}
