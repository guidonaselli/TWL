using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using TWL.Shared.Net.Network;

namespace TWL.Server.Security;

public class RateLimiter
{
    private readonly ConcurrentDictionary<Opcode, Bucket> _buckets;

    public RateLimiter(IOptions<RateLimiterOptions> options) : this(options.Value)
    {
    }

    public RateLimiter(RateLimiterOptions options)
    {
        _buckets = new ConcurrentDictionary<Opcode, Bucket>();
        InitializePolicies(options);
    }

    // For testing or manual initialization without options
    public RateLimiter() : this(new RateLimiterOptions())
    {
    }

    private void InitializePolicies(RateLimiterOptions options)
    {
        if (options?.Policies == null) return;

        foreach (var policy in options.Policies)
        {
            if (Enum.TryParse<Opcode>(policy.Key, true, out var op))
            {
                SetPolicy(op, policy.Value.Capacity, policy.Value.RefillRate);
            }
        }
    }

    public void SetPolicy(Opcode op, double capacity, double refillRate) =>
        _buckets[op] = new Bucket(capacity, refillRate);

    public bool Check(Opcode op)
    {
        // Default policy for undefined opcodes: 2 burst, 1 refill/sec
        var bucket = _buckets.GetOrAdd(op, _ => new Bucket(2, 1));
        return bucket.Consume();
    }

    private class Bucket
    {
        public Bucket(double capacity, double refillRate)
        {
            Capacity = capacity;
            RefillRate = refillRate;
            Tokens = capacity;
            LastRefill = DateTime.UtcNow;
        }

        public double Capacity { get; }
        public double RefillRate { get; } // Tokens per second
        public double Tokens { get; set; }
        public DateTime LastRefill { get; set; }

        public bool Consume(double amount = 1.0)
        {
            lock (this)
            {
                Refill();
                if (Tokens >= amount)
                {
                    Tokens -= amount;
                    return true;
                }

                return false;
            }
        }

        private void Refill()
        {
            var now = DateTime.UtcNow;
            var delta = (now - LastRefill).TotalSeconds;
            if (delta > 0)
            {
                Tokens = Math.Min(Capacity, Tokens + delta * RefillRate);
                LastRefill = now;
            }
        }
    }
}
