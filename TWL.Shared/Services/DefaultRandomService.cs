using System;

namespace TWL.Shared.Services;

/// <summary>
///     A default implementation of IRandomService using System.Random.
///     This is suitable for shared domain logic where dependency injection is not available
///     or for default behavior. Note: When using the default constructor, it uses Random.Shared (Thread-Safe).
///     When using a specific seed, it uses new Random(seed) (Not Thread-Safe).
/// </summary>
public class DefaultRandomService : IRandomService
{
    private readonly Random _random;

    public DefaultRandomService()
    {
        _random = Random.Shared;
    }

    public DefaultRandomService(int seed)
    {
        _random = new Random(seed);
    }

    public float NextFloat(string? context = null)
    {
        return _random.NextSingle();
    }

    public float NextFloat(float min, float max, string? context = null)
    {
        return min + (max - min) * _random.NextSingle();
    }

    public double NextDouble(string? context = null)
    {
        return _random.NextDouble();
    }

    public int Next(string? context = null)
    {
        return _random.Next();
    }

    public int Next(int min, int max, string? context = null)
    {
        return _random.Next(min, max);
    }
}
