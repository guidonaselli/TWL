using TWL.Server.Domain.World;
using TWL.Server.Simulation.Networking;

namespace TWL.Server.Services.World;

public interface IWorldTriggerService
{
    void RegisterHandler(ITriggerHandler handler);
    void Start();
    void OnEnterTrigger(ServerCharacter character, int mapId, string triggerId);
    void OnInteractTrigger(ServerCharacter character, int mapId, string triggerId);
    void CheckTriggers(ServerCharacter character);
    ServerSpawn? GetSpawn(int mapId, string spawnId);
    IEnumerable<ServerCharacter> GetPlayersInTrigger(ServerTrigger trigger, int mapId);
}