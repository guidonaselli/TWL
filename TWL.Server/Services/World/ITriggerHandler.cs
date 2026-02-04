using TWL.Server.Domain.World;
using TWL.Server.Simulation.Networking;

namespace TWL.Server.Services.World;

public interface ITriggerHandler
{
    bool CanHandle(string triggerType);
    void ExecuteEnter(ServerCharacter character, ServerTrigger trigger, IWorldTriggerService context);
    void ExecuteInteract(ServerCharacter character, ServerTrigger trigger, IWorldTriggerService context);
    void ExecuteTick(ServerTrigger trigger, int mapId, IWorldTriggerService context);
}