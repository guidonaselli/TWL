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

    public float NextFloat(string? context = null) => FixedFloat;

    public float NextFloat(float min, float max, string? context = null) => min + (max - min) * FixedFloat;

    public double NextDouble(string? context = null) => FixedFloat;

    public int Next(string? context = null) => FixedInt;

    public int Next(int min, int max, string? context = null)
    {
        if (FixedInt >= min && FixedInt < max)
        {
            return FixedInt;
        }

        return min;
    }
}
