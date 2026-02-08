namespace TWL.Server.Domain.World;

public class TriggerAction
{
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, string> Parameters { get; set; } = new();

    public TriggerAction() { }

    public TriggerAction(string type, Dictionary<string, string> parameters)
    {
        Type = type;
        Parameters = parameters;
    }
}
