using System.Collections.Concurrent;

namespace TWL.Server.Security;

/// <summary>
/// Validates message replay metadata (nonce + timestamp) to prevent
/// duplicate and stale packet processing. Thread-safe per-session.
/// </summary>
public class ReplayGuard
{
    private readonly ReplayGuardOptions _options;
    private readonly ConcurrentDictionary<int, SessionNonceCache> _sessionCaches = new();
    private readonly Func<DateTime> _clock;

    public ReplayGuard(ReplayGuardOptions options)
        : this(options, () => DateTime.UtcNow)
    {
    }

    /// <summary>
    /// Constructor with injectable clock for deterministic testing.
    /// </summary>
    public ReplayGuard(ReplayGuardOptions options, Func<DateTime> clock)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>
    /// Validates a message's replay metadata.
    /// </summary>
    /// <param name="sessionId">User/session identifier.</param>
    /// <param name="nonce">Message nonce (unique per message).</param>
    /// <param name="timestampUtc">Message creation timestamp in UTC.</param>
    /// <param name="reason">Rejection reason if validation fails.</param>
    /// <returns>True if message should be processed; false if rejected.</returns>
    public bool Validate(int sessionId, string? nonce, DateTime? timestampUtc, out string reason)
    {
        reason = string.Empty;

        // Legacy client without replay metadata
        if (string.IsNullOrEmpty(nonce) || !timestampUtc.HasValue)
        {
            if (_options.StrictMode)
            {
                reason = "MissingReplayMetadata";
                return false;
            }
            // Transitional mode: allow legacy clients through
            return true;
        }

        var now = _clock();

        // Freshness check: reject stale messages
        var age = (now - timestampUtc.Value).TotalSeconds;
        if (age > _options.AllowedClockSkewSeconds)
        {
            reason = $"StaleTimestamp:Age={age:F1}s,Max={_options.AllowedClockSkewSeconds}s";
            return false;
        }

        // Future check: reject messages from the future (with small tolerance)
        if (age < -_options.AllowedClockSkewSeconds)
        {
            reason = $"FutureTimestamp:Age={age:F1}s";
            return false;
        }

        // Duplicate nonce check
        var cache = _sessionCaches.GetOrAdd(sessionId, _ => new SessionNonceCache(_options));
        if (!cache.TryAdd(nonce, now))
        {
            reason = $"DuplicateNonce:{nonce}";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Removes session cache on disconnect to free memory.
    /// </summary>
    public void RemoveSession(int sessionId)
    {
        _sessionCaches.TryRemove(sessionId, out _);
    }

    /// <summary>
    /// Per-session bounded nonce cache with TTL-based eviction.
    /// </summary>
    private class SessionNonceCache
    {
        private readonly ReplayGuardOptions _options;
        private readonly Dictionary<string, DateTime> _nonces = new();
        private readonly LinkedList<string> _insertionOrder = new();
        private readonly object _lock = new();

        public SessionNonceCache(ReplayGuardOptions options)
        {
            _options = options;
        }

        /// <summary>
        /// Attempts to add a nonce. Returns false if duplicate.
        /// Evicts expired entries and enforces max capacity.
        /// </summary>
        public bool TryAdd(string nonce, DateTime now)
        {
            lock (_lock)
            {
                // Evict expired entries
                EvictExpired(now);

                // Check duplicate
                if (_nonces.ContainsKey(nonce))
                {
                    return false;
                }

                // Evict oldest if at capacity
                while (_nonces.Count >= _options.MaxNoncesPerSession && _insertionOrder.First != null)
                {
                    var oldest = _insertionOrder.First.Value;
                    _insertionOrder.RemoveFirst();
                    _nonces.Remove(oldest);
                }

                // Add new nonce
                _nonces[nonce] = now;
                _insertionOrder.AddLast(nonce);
                return true;
            }
        }

        private void EvictExpired(DateTime now)
        {
            while (_insertionOrder.First != null)
            {
                var oldest = _insertionOrder.First.Value;
                if (_nonces.TryGetValue(oldest, out var added) &&
                    (now - added).TotalSeconds > _options.NonceTtlSeconds)
                {
                    _insertionOrder.RemoveFirst();
                    _nonces.Remove(oldest);
                }
                else
                {
                    break;
                }
            }
        }
    }
}
