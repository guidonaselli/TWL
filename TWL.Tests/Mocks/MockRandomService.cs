using System;
using TWL.Shared.Services;

namespace TWL.Tests.Mocks;

public class MockRandomService : IRandomService
{
    public float FixedFloat { get; set; } = 0.5f;
    public int FixedInt { get; set; } = 0;

    public MockRandomService() { }

    public MockRandomService(float fixedFloat)
    {
        FixedFloat = fixedFloat;
    }

    public float NextFloat()
    {
        return FixedFloat;
    }

    public float NextFloat(float min, float max)
    {
        return min + (max - min) * FixedFloat;
    }

    public int Next(int min, int max)
    {
        if (FixedInt >= min && FixedInt < max) return FixedInt;
        return min;
    }
}
