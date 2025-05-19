namespace TWL.Shared.Net;

public static class EventBus
{
    private static readonly Dictionary<Type, List<Delegate>> _subs = new();

    public static void Subscribe<T>(Action<T> h)
    {
        var t = typeof(T);
        if (!_subs.ContainsKey(t)) _subs[t] = new List<Delegate>();
        _subs[t].Add(h);
    }

    public static void Publish<T>(T evt)
    {
        if (_subs.TryGetValue(typeof(T), out var list))
            foreach (var d in list.Cast<Action<T>>())
                d(evt);
    }

    public static void Clear()
    {
        _subs.Clear();
        // útil en tests
    }
}