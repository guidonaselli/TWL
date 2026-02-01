using Microsoft.Extensions.Logging;
using TWL.Shared.Services;

namespace TWL.Server.Services;

/// <summary>
///     A seedable, thread-safe implementation of IRandomService.
///     Allows for deterministic behavior when a seed is provided.
/// </summary>
public class SeedableRandomService : IRandomService
{
    private readonly object _lock = new();
    private readonly ILogger<SeedableRandomService> _logger;
    private readonly Random _random;

    public SeedableRandomService(ILogger<SeedableRandomService> logger, int? seed = null)
    {
        _logger = logger;
        if (seed.HasValue)
        {
            _random = new Random(seed.Value);
            _logger.LogInformation("RNG Initialized with fixed Seed: {Seed}", seed.Value);
        }
        else
        {
            _random = new Random();
            _logger.LogInformation("RNG Initialized with default (time-based) seed.");
        }
    }

    public float NextFloat()
    {
        lock (_lock)
        {
            var val = _random.NextSingle();
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("RNG NextFloat: {Value}", val);
            }

            return val;
        }
    }

    public float NextFloat(float min, float max)
    {
        lock (_lock)
        {
            var val = min + (max - min) * _random.NextSingle();
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("RNG NextFloat({Min}, {Max}): {Value}", min, max, val);
            }

            return val;
        }
    }

    public int Next(int min, int max)
    {
        lock (_lock)
        {
            var val = _random.Next(min, max);
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("RNG Next({Min}, {Max}): {Value}", min, max, val);
            }

            return val;
        }
    }

    public double NextDouble()
    {
        lock (_lock)
        {
            var val = _random.NextDouble();
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("RNG NextDouble: {Value}", val);
            }

            return val;
        }
    }

    public int Next()
    {
        lock (_lock)
        {
            var val = _random.Next();
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("RNG Next: {Value}", val);
            }

            return val;
        }
    }
}