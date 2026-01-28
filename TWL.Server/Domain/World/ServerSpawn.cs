using System.Collections.Generic;

namespace TWL.Server.Domain.World;

public class ServerSpawn
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public float X { get; set; }
    public float Y { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
}
