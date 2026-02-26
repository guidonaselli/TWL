using Microsoft.Extensions.Options;
using TWL.Server.Security;
using TWL.Shared.Net.Network;
using Xunit;

namespace TWL.Tests.Security;

public class RateLimiterTests
{
    [Fact]
    public void Check_ShouldUseDefaultPolicy_WhenNoConfig()
    {
        var limiter = new RateLimiter();

        // Default bucket: 2 burst, 1 refill/sec
        Assert.True(limiter.Check(Opcode.MoveRequest));
        Assert.True(limiter.Check(Opcode.MoveRequest));
        Assert.False(limiter.Check(Opcode.MoveRequest));
    }

    [Fact]
    public void Check_ShouldUseConfiguredPolicy()
    {
        var options = new RateLimiterOptions();
        options.Policies["MoveRequest"] = new RateLimitPolicy { Capacity = 5, RefillRate = 1 };

        var limiter = new RateLimiter(options);

        // Should allow 5
        for (int i = 0; i < 5; i++)
        {
            Assert.True(limiter.Check(Opcode.MoveRequest), $"Iteration {i} failed");
        }

        // Should reject 6th
        Assert.False(limiter.Check(Opcode.MoveRequest));
    }

    [Fact]
    public void Check_ShouldHandleCaseInsensitivePolicyKeys()
    {
        // Enum.TryParse is case insensitive if second arg is true.
        // Code: Enum.TryParse<Opcode>(policy.Key, true, out var op)

        var options = new RateLimiterOptions();
        options.Policies["moverequest"] = new RateLimitPolicy { Capacity = 10, RefillRate = 1 };

        var limiter = new RateLimiter(options);

        // Consume more than default (2) to prove it picked up the config
        for (int i = 0; i < 3; i++)
        {
             Assert.True(limiter.Check(Opcode.MoveRequest));
        }
    }
}
