using TWL.Server.Domain.World;
using TWL.Server.Simulation.Networking;

namespace TWL.Server.Services.World.Actions.Handlers;

public class HealActionHandler : ITriggerActionHandler
{
    public string ActionType => "Heal";

    public void Execute(ServerCharacter character, TriggerAction action)
    {
        if (action.Parameters.TryGetValue("Amount", out var amtStr) && int.TryParse(amtStr, out var amt))
        {
            character.Heal(amt);
        }
    }
}
