using System.Collections.Concurrent;
using System.Text.Json;
using TWL.Shared.Domain.DTO;

namespace TWL.Client.Presentation.Services;

public class JsonPlayerColorsService : IPlayerColorsService
{
    // Static cache to prevent repeated File I/O and deserialization
    // Use Lazy<Task<...>> to ensure exactly one load per filepath
    private static readonly ConcurrentDictionary<string, Lazy<Task<Dictionary<Guid, PlayerColorsDto>>>> _cache = new();

    private readonly Task<Dictionary<Guid, PlayerColorsDto>> _loadingTask;

    public JsonPlayerColorsService(string filePath)
    {
        // Get from cache or load if not present
        var lazy = _cache.GetOrAdd(filePath,
            path => new Lazy<Task<Dictionary<Guid, PlayerColorsDto>>>(() => LoadColorsAsync(path)));
        _loadingTask = lazy.Value;
    }

    public PlayerColorsDto? Get(Guid playerId)
    {
        // Must block if not ready. The constructor is non-blocking, so IO happens in background
        // until this method is called.
        var map = _loadingTask.GetAwaiter().GetResult();
        return map.TryGetValue(playerId, out var dto) ? dto : null;
    }

    private static async Task<Dictionary<Guid, PlayerColorsDto>> LoadColorsAsync(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var raw = await JsonSerializer.DeserializeAsync<Dictionary<string, PlayerColorsDto>>(stream)
            .ConfigureAwait(false) ?? new Dictionary<string, PlayerColorsDto>();
        return raw.ToDictionary(
            kv => Guid.Parse(kv.Key),
            kv => kv.Value);
    }
}