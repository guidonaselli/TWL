using System.Collections.Concurrent;
using TWL.Shared.Net.Network;

namespace TWL.Server.Security;

public class RateLimiter
{
    private readonly ConcurrentDictionary<Opcode, Bucket> _buckets;

    public RateLimiter()
    {
        _buckets = new ConcurrentDictionary<Opcode, Bucket>();
        InitializeDefaultPolicies();
    }

    private void InitializeDefaultPolicies()
    {
        // Login: 5 per minute ~ 0.08/sec. Burst 2.
        SetPolicy(Opcode.LoginRequest, 3, 0.1);

        // Move: Frequent.
        SetPolicy(Opcode.MoveRequest, 20, 10);

        // Combat/Interaction: Moderate.
        SetPolicy(Opcode.AttackRequest, 10, 5);
        SetPolicy(Opcode.InteractRequest, 10, 5);
        SetPolicy(Opcode.UseItemRequest, 10, 5);

        // Quests: Low frequency.
        SetPolicy(Opcode.StartQuestRequest, 3, 1);
        SetPolicy(Opcode.ClaimRewardRequest, 3, 1);

        // Economy: Strict.
        SetPolicy(Opcode.PurchaseGemsIntent, 2, 0.2);
        SetPolicy(Opcode.PurchaseGemsVerify, 2, 0.2);
        SetPolicy(Opcode.BuyShopItemRequest, 5, 1);
    }

    public void SetPolicy(Opcode op, double capacity, double refillRate) =>
        _buckets[op] = new Bucket(capacity, refillRate);

    public bool Check(Opcode op)
    {
        // Default policy for undefined opcodes: 1 per second
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