namespace TWL.Shared.Services;

/// <summary>
///     Provides an abstraction for Random Number Generation to allow deterministic testing and consistent usage.
/// </summary>
public interface IRandomService
{
    /// <summary>
    ///     Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.
    /// </summary>
    float NextFloat();

    /// <summary>
    ///     Returns a random floating-point number that is greater than or equal to min, and less than max.
    /// </summary>
    float NextFloat(float min, float max);

    /// <summary>
    ///     Returns a random integer that is within a specified range.
    /// </summary>
    int Next(int min, int max);
}