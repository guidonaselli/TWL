using TWL.Server.Domain.World;
using TWL.Server.Services.World.Actions;
using TWL.Server.Simulation.Networking;

namespace TWL.Server.Services.World.Handlers;

public class GenericTriggerHandler : ITriggerHandler
{
    private readonly TriggerActionRegistry _registry;

    public GenericTriggerHandler(TriggerActionRegistry registry)
    {
        _registry = registry;
    }

    public bool CanHandle(string triggerType)
    {
        return triggerType.Equals("Generic", StringComparison.OrdinalIgnoreCase) ||
               triggerType.Equals("Script", StringComparison.OrdinalIgnoreCase) ||
               triggerType.Equals("Event", StringComparison.OrdinalIgnoreCase);
    }

    public void ExecuteEnter(ServerCharacter character, ServerTrigger trigger, IWorldTriggerService context)
    {
        ExecuteActions(character, trigger);
    }

    public void ExecuteInteract(ServerCharacter character, ServerTrigger trigger, IWorldTriggerService context)
    {
        ExecuteActions(character, trigger);
    }

    public void ExecuteTick(ServerTrigger trigger, int mapId, IWorldTriggerService context)
    {
        foreach (var character in context.GetPlayersInTrigger(trigger, mapId))
        {
            ExecuteActions(character, trigger);
        }
    }

    private void ExecuteActions(ServerCharacter character, ServerTrigger trigger)
    {
        foreach (var action in trigger.Actions)
        {
            try
            {
                var handler = _registry.GetHandler(action.Type);
                if (handler != null)
                {
                    handler.Execute(character, action);
                }
                else
                {
                    Console.WriteLine($"[Trigger] No handler for action type {action.Type} in trigger {trigger.Id}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing action {action.Type} for trigger {trigger.Id}: {ex.Message}");
            }
        }
    }
}
