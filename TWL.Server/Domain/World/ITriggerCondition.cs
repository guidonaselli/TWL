using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Networking;

namespace TWL.Server.Domain.World;

public interface ITriggerCondition
{
    bool IsMet(ServerCharacter character, PlayerService playerService);
}
