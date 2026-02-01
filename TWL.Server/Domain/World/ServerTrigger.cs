namespace TWL.Server.Domain.World;

public class ServerTrigger
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
}