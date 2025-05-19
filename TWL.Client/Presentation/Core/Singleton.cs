using System;

namespace TWL.Client.Presentation.Core;

public class Singleton<T> where T : class, new()
{
    private static readonly Lazy<T> _lazy = new(() => new());
    public static T Instance => _lazy.Value;
}