using TWL.Server.Domain.World;
using TWL.Server.Simulation.Networking;

namespace TWL.Server.Services.World.Actions;

public interface ITriggerActionHandler
{
    string ActionType { get; }
    void Execute(ServerCharacter character, TriggerAction action);
}
