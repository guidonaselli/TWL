namespace TWL.Shared.Services;

/// <summary>
///     Provides an abstraction for Random Number Generation to allow deterministic testing and consistent usage.
/// </summary>
public interface IRandomService
{
    /// <summary>
    ///     Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.
    /// </summary>
    /// <param name="context">Optional context string for auditing purposes (e.g. "HitChance", "DropRoll").</param>
    float NextFloat(string? context = null);

    /// <summary>
    ///     Returns a random floating-point number that is greater than or equal to min, and less than max.
    /// </summary>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <param name="context">Optional context string for auditing purposes.</param>
    float NextFloat(float min, float max, string? context = null);

    /// <summary>
    ///     Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.
    /// </summary>
    /// <param name="context">Optional context string for auditing purposes.</param>
    double NextDouble(string? context = null);

    /// <summary>
    ///     Returns a non-negative random integer.
    /// </summary>
    /// <param name="context">Optional context string for auditing purposes.</param>
    int Next(string? context = null);

    /// <summary>
    ///     Returns a random integer that is within a specified range.
    /// </summary>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="max">The exclusive upper bound.</param>
    /// <param name="context">Optional context string for auditing purposes.</param>
    int Next(int min, int max, string? context = null);
}
