namespace TWL.Client.Presentation.Managers;

public static class RandomManager
{
    private static readonly Random _rnd = new();

    public static float NextFloat() => (float)_rnd.NextDouble();

    public static int NextInt(int min, int max) => _rnd.Next(min, max);

    public static int NextInt(int max) => _rnd.Next(max);

    public static int NextInt() => _rnd.Next();

    public static float NextFloat(float min, float max) => (float)(_rnd.NextDouble() * (max - min) + min);

    public static float NextFloat(float max) => (float)(_rnd.NextDouble() * max);
}