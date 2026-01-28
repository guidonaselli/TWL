using System.Collections.Generic;

namespace TWL.Server.Domain.World;

public class ServerMap
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public int TileWidth { get; set; }
    public int TileHeight { get; set; }
    public List<ServerTrigger> Triggers { get; set; } = new();
    public List<ServerSpawn> Spawns { get; set; } = new();
}
