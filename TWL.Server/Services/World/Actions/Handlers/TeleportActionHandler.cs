using TWL.Server.Domain.World;
using TWL.Server.Simulation.Networking;

namespace TWL.Server.Services.World.Actions.Handlers;

public class TeleportActionHandler : ITriggerActionHandler
{
    public string ActionType => "Teleport";

    public void Execute(ServerCharacter character, TriggerAction action)
    {
        var mapId = character.MapId;
        if (action.Parameters.TryGetValue("MapId", out var mapIdStr) && int.TryParse(mapIdStr, out var mId))
        {
            mapId = mId;
        }

        var x = character.X;
        if (action.Parameters.TryGetValue("X", out var xStr) && float.TryParse(xStr, out var xVal))
        {
            x = xVal;
        }

        var y = character.Y;
        if (action.Parameters.TryGetValue("Y", out var yStr) && float.TryParse(yStr, out var yVal))
        {
            y = yVal;
        }

        character.Teleport(mapId, x, y);
    }
}
