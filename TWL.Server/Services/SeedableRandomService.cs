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

    public float NextFloat(string? context = null)
    {
        lock (_lock)
        {
            var val = _random.NextSingle();
            AuditIfContext(context, val, "NextFloat");
            return val;
        }
    }

    public float NextFloat(float min, float max, string? context = null)
    {
        lock (_lock)
        {
            var val = min + (max - min) * _random.NextSingle();
            AuditIfContext(context, val, $"NextFloat({min}, {max})");
            return val;
        }
    }

    public int Next(string? context = null)
    {
        lock (_lock)
        {
            var val = _random.Next();
            AuditIfContext(context, val, "Next");
            return val;
        }
    }

    public int Next(int min, int max, string? context = null)
    {
        lock (_lock)
        {
            var val = _random.Next(min, max);
            AuditIfContext(context, val, $"Next({min}, {max})");
            return val;
        }
    }

    public double NextDouble(string? context = null)
    {
        lock (_lock)
        {
            var val = _random.NextDouble();
            AuditIfContext(context, val, "NextDouble");
            return val;
        }
    }

    private void AuditIfContext(string? context, object value, string operation)
    {
        if (context != null)
        {
            _logger.LogInformation("AUDIT: RNG [{Context}] {Operation}: {Value}", context, operation, value);
        }
        else if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("RNG [Global] {Operation}: {Value}", operation, value);
        }
    }
}
