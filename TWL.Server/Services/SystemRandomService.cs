using System;
using TWL.Shared.Services;

namespace TWL.Server.Services;

public class SystemRandomService : IRandomService
{
    private readonly Random _random = Random.Shared;

    public float NextFloat()
    {
        return _random.NextSingle();
    }

    public float NextFloat(float min, float max)
    {
        return min + (max - min) * NextFloat();
    }

    public int Next(int min, int max)
    {
        return _random.Next(min, max);
    }
}
