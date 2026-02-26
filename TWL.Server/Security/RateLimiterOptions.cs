using System.Collections.Generic;

namespace TWL.Server.Security;

public class RateLimiterOptions
{
    public Dictionary<string, RateLimitPolicy> Policies { get; set; } = new();
}

public class RateLimitPolicy
{
    public double Capacity { get; set; }
    public double RefillRate { get; set; }
}
