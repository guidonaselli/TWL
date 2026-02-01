using TWL.Server.Domain.World;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.World;

namespace TWL.Server.Services.World.Handlers;

public class MapTransitionHandler : ITriggerHandler
{
    public bool CanHandle(string triggerType) => triggerType == WorldConstants.TriggerTypes.MapTransition;

    public void ExecuteEnter(ServerCharacter character, ServerTrigger trigger, IWorldTriggerService context)
    {
        if (trigger.Properties.TryGetValue("TargetMapId", out var targetMapIdStr) &&
            int.TryParse(targetMapIdStr, out var targetMapId) &&
            trigger.Properties.TryGetValue("TargetSpawnId", out var targetSpawnId))
        {
            var spawn = context.GetSpawn(targetMapId, targetSpawnId);
            if (spawn != null)
            {
                // Update Character Position
                character.MapId = targetMapId;
                character.X = spawn.X;
                character.Y = spawn.Y;
            }
        }
    }

    public void ExecuteInteract(ServerCharacter character, ServerTrigger trigger, IWorldTriggerService context) =>
        ExecuteEnter(character, trigger, context);
}