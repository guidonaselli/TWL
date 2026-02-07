using TWL.Server.Domain.World;

namespace TWL.Server.Services.World;

public interface IMapRegistry
{
    ServerMap? GetMap(int id);
    IEnumerable<ServerMap> GetAllMaps();
    void Load(string contentPath);
}
