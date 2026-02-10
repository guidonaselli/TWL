namespace TWL.Server.Domain.World;

public class ServerTrigger
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public TriggerScope Scope { get; set; } = TriggerScope.Character;
    public TriggerActivationType ActivationType { get; set; } = TriggerActivationType.Enter;
    public int IntervalMs { get; set; }
    public int CooldownMs { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
    public List<ITriggerCondition> Conditions { get; set; } = new();
    public List<TriggerAction> Actions { get; set; } = new();
}
