using TWL.Server.Domain.World;

namespace TWL.Server.Services.World;

public interface IMapRegistry
{
    ServerMap? GetMap(int id);
    IEnumerable<ServerMap> GetAllMaps();
    void Load(string contentPath);

    /// <summary>
    /// Look up an entity (trigger or spawn) position by a given target name in a specific map.
    /// </summary>
    (float X, float Y)? GetEntityPosition(int mapId, string targetName);
}
