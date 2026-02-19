namespace TWL.Server.Security;

/// <summary>
/// Configuration options for the <see cref="ReplayGuard"/> service.
/// </summary>
public class ReplayGuardOptions
{
    /// <summary>
    /// Maximum allowed clock skew (freshness window) in seconds.
    /// Messages older than this are rejected.
    /// Default: 30 seconds.
    /// </summary>
    public int AllowedClockSkewSeconds { get; set; } = 30;

    /// <summary>
    /// How long seen nonces are retained per session for duplicate detection in seconds.
    /// Should be >= AllowedClockSkewSeconds to cover the full validity window.
    /// Default: 60 seconds.
    /// </summary>
    public int NonceTtlSeconds { get; set; } = 60;

    /// <summary>
    /// Maximum distinct nonces tracked per session before oldest entries are evicted.
    /// Prevents unbounded memory growth from misbehaving clients.
    /// Default: 1000.
    /// </summary>
    public int MaxNoncesPerSession { get; set; } = 1000;

    /// <summary>
    /// When true, messages without nonce/timestamp from legacy clients
    /// are rejected. When false, they are allowed through (transitional mode).
    /// Default: false (allow legacy clients).
    /// </summary>
    public bool StrictMode { get; set; } = false;
}
