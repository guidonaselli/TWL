namespace TWL.Server.Services.World.Actions;

public class TriggerActionRegistry
{
    private readonly Dictionary<string, ITriggerActionHandler> _handlers = new(StringComparer.OrdinalIgnoreCase);

    public void Register(ITriggerActionHandler handler)
    {
        _handlers[handler.ActionType] = handler;
    }

    public ITriggerActionHandler? GetHandler(string actionType)
    {
        _handlers.TryGetValue(actionType, out var handler);
        return handler;
    }
}
