using TWL.Server.Security;
using TWL.Shared.Net.Network;

namespace TWL.Tests.Security;

public class RateLimiterTests
{
    [Fact]
    public void Check_AllowsRequestsUnderLimit()
    {
        var limiter = new RateLimiter();
        limiter.SetPolicy(Opcode.MoveRequest, 10, 10);

        // Should allow 10 requests immediately
        for (var i = 0; i < 10; i++)
        {
            Assert.True(limiter.Check(Opcode.MoveRequest), $"Request {i} failed");
        }
    }

    [Fact]
    public void Check_BlocksRequestsOverLimit()
    {
        var limiter = new RateLimiter();
        // Capacity 2, Refill very slow
        limiter.SetPolicy(Opcode.AttackRequest, 2, 0.001);

        Assert.True(limiter.Check(Opcode.AttackRequest)); // 1
        Assert.True(limiter.Check(Opcode.AttackRequest)); // 2
        Assert.False(limiter.Check(Opcode.AttackRequest)); // 3 - blocked
    }

    [Fact]
    public void Check_RefillsOverTime()
    {
        var limiter = new RateLimiter();
        // Capacity 1, Refill 10 per second
        limiter.SetPolicy(Opcode.InteractRequest, 1, 10);

        Assert.True(limiter.Check(Opcode.InteractRequest)); // Consumes 1
        Assert.False(limiter.Check(Opcode.InteractRequest)); // Empty

        // Wait a bit
        Thread.Sleep(200); // 0.2s should refill 2 tokens (but cap is 1)

        Assert.True(limiter.Check(Opcode.InteractRequest)); // Should work again
    }
}