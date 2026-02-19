using TWL.Server.Security;

namespace TWL.Tests.Security;

public class ReplayGuardTests
{
    private static ReplayGuard CreateGuard(
        int clockSkewSeconds = 30,
        int nonceTtlSeconds = 60,
        int maxNonces = 1000,
        bool strictMode = false,
        Func<DateTime>? clock = null)
    {
        var options = new ReplayGuardOptions
        {
            AllowedClockSkewSeconds = clockSkewSeconds,
            NonceTtlSeconds = nonceTtlSeconds,
            MaxNoncesPerSession = maxNonces,
            StrictMode = strictMode
        };
        return new ReplayGuard(options, clock ?? (() => DateTime.UtcNow));
    }

    // --- Fresh unique nonce ---

    [Fact]
    public void Validate_FreshUniqueNonce_Accepted()
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var guard = CreateGuard(clock: () => now);

        var result = guard.Validate(1, Guid.NewGuid().ToString(), now, out var reason);

        Assert.True(result);
        Assert.Equal(string.Empty, reason);
    }

    // --- Duplicate nonce ---

    [Fact]
    public void Validate_DuplicateNonce_SameSession_Rejected()
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var guard = CreateGuard(clock: () => now);
        var nonce = Guid.NewGuid().ToString();

        guard.Validate(1, nonce, now, out _);
        var result = guard.Validate(1, nonce, now, out var reason);

        Assert.False(result);
        Assert.Contains("DuplicateNonce", reason);
    }

    [Fact]
    public void Validate_SameNonce_DifferentSessions_Accepted()
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var guard = CreateGuard(clock: () => now);
        var nonce = Guid.NewGuid().ToString();

        guard.Validate(1, nonce, now, out _);
        var result = guard.Validate(2, nonce, now, out var reason);

        Assert.True(result);
        Assert.Equal(string.Empty, reason);
    }

    // --- Stale timestamp ---

    [Fact]
    public void Validate_StaleTimestamp_Rejected()
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var guard = CreateGuard(clockSkewSeconds: 30, clock: () => now);

        // Message sent 60 seconds ago
        var staleTime = now.AddSeconds(-60);
        var result = guard.Validate(1, Guid.NewGuid().ToString(), staleTime, out var reason);

        Assert.False(result);
        Assert.Contains("StaleTimestamp", reason);
    }

    [Fact]
    public void Validate_FutureTimestamp_Rejected()
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var guard = CreateGuard(clockSkewSeconds: 30, clock: () => now);

        // Message from 60 seconds in the future
        var futureTime = now.AddSeconds(60);
        var result = guard.Validate(1, Guid.NewGuid().ToString(), futureTime, out var reason);

        Assert.False(result);
        Assert.Contains("FutureTimestamp", reason);
    }

    [Fact]
    public void Validate_WithinClockSkew_Accepted()
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var guard = CreateGuard(clockSkewSeconds: 30, clock: () => now);

        // Message sent 20 seconds ago (within 30s window)
        var recentTime = now.AddSeconds(-20);
        var result = guard.Validate(1, Guid.NewGuid().ToString(), recentTime, out var reason);

        Assert.True(result);
        Assert.Equal(string.Empty, reason);
    }

    // --- Missing metadata ---

    [Fact]
    public void Validate_MissingNonce_NonStrictMode_Accepted()
    {
        var guard = CreateGuard(strictMode: false);

        var result = guard.Validate(1, null, DateTime.UtcNow, out var reason);

        Assert.True(result);
        Assert.Equal(string.Empty, reason);
    }

    [Fact]
    public void Validate_MissingNonce_StrictMode_Rejected()
    {
        var guard = CreateGuard(strictMode: true);

        var result = guard.Validate(1, null, DateTime.UtcNow, out var reason);

        Assert.False(result);
        Assert.Contains("MissingReplayMetadata", reason);
    }

    [Fact]
    public void Validate_MissingTimestamp_StrictMode_Rejected()
    {
        var guard = CreateGuard(strictMode: true);

        var result = guard.Validate(1, Guid.NewGuid().ToString(), null, out var reason);

        Assert.False(result);
        Assert.Contains("MissingReplayMetadata", reason);
    }

    // --- Max nonces eviction ---

    [Fact]
    public void Validate_MaxNoncesReached_EvictsOldest()
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var guard = CreateGuard(maxNonces: 3, clock: () => now);

        // Fill cache
        guard.Validate(1, "nonce-1", now, out _);
        guard.Validate(1, "nonce-2", now, out _);
        guard.Validate(1, "nonce-3", now, out _);

        // Adding 4th should evict "nonce-1"
        var result = guard.Validate(1, "nonce-4", now, out _);
        Assert.True(result);

        // "nonce-1" should now be accepted again (evicted)
        result = guard.Validate(1, "nonce-1", now, out _);
        Assert.True(result);

        // "nonce-2" should still be rejected (not yet evicted)
        result = guard.Validate(1, "nonce-2", now, out var reason);
        Assert.False(result);
        Assert.Contains("DuplicateNonce", reason);
    }

    // --- Session cleanup ---

    [Fact]
    public void RemoveSession_ClearsCache()
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var guard = CreateGuard(clock: () => now);
        var nonce = Guid.NewGuid().ToString();

        guard.Validate(1, nonce, now, out _);
        guard.RemoveSession(1);

        // Same nonce should now be accepted (cache cleared)
        var result = guard.Validate(1, nonce, now, out _);
        Assert.True(result);
    }

    // --- TTL expiry ---

    [Fact]
    public void Validate_NonceTtlExpired_AllowsReuse()
    {
        var currentTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var guard = CreateGuard(nonceTtlSeconds: 5, clockSkewSeconds: 100, clock: () => currentTime);

        var nonce = Guid.NewGuid().ToString();
        guard.Validate(1, nonce, currentTime, out _);

        // Advance clock past TTL
        currentTime = currentTime.AddSeconds(10);
        guard = CreateGuard(nonceTtlSeconds: 5, clockSkewSeconds: 100, clock: () => currentTime);

        // New guard instance, so nonce is accepted — but we want to test the same instance
        // Let's use a mutable clock
        var time = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var guard2 = CreateGuard(nonceTtlSeconds: 5, clockSkewSeconds: 100, clock: () => time);

        guard2.Validate(1, nonce, time, out _);

        // Advance clock and add a new nonce to trigger eviction
        time = time.AddSeconds(10);
        guard2.Validate(1, "trigger-eviction", time, out _);

        // Original nonce should now be accepted (expired and evicted)
        var result = guard2.Validate(1, nonce, time, out _);
        Assert.True(result);
    }
}
