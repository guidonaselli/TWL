using TWL.Shared.Services;

namespace TWL.Tests.Mocks;

public class MockRandomService : IRandomService
{
    public MockRandomService()
    {
    }

    public MockRandomService(float fixedFloat)
    {
        FixedFloat = fixedFloat;
    }

    public float FixedFloat { get; set; } = 0.5f;
    public int FixedInt { get; set; } = 0;

    public float NextFloat() => FixedFloat;

    public float NextFloat(float min, float max) => min + (max - min) * FixedFloat;

    public double NextDouble() => FixedFloat;

    public int Next() => FixedInt;

    public int Next(int min, int max)
    {
        if (FixedInt >= min && FixedInt < max)
        {
            return FixedInt;
        }

        return min;
    }
}
