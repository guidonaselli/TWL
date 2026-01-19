using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TWL.Shared.Domain.DTO;

namespace TWL.Client.Presentation.Services;

public class JsonPlayerColorsService : IPlayerColorsService
{
    // Static cache to prevent repeated File I/O and deserialization
    private static readonly ConcurrentDictionary<string, Dictionary<Guid, PlayerColorsDto>> _cache = new();

    private readonly Dictionary<Guid, PlayerColorsDto> _map;

    public JsonPlayerColorsService(string filePath)
    {
        // Get from cache or load if not present
        _map = _cache.GetOrAdd(filePath, LoadColors);
    }

    private static Dictionary<Guid, PlayerColorsDto> LoadColors(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var raw = JsonSerializer.Deserialize<Dictionary<string, PlayerColorsDto>>(json)
                  ?? new();
        return raw.ToDictionary(
            kv => Guid.Parse(kv.Key),
            kv => kv.Value);
    }

    public PlayerColorsDto? Get(Guid playerId)
        => _map.TryGetValue(playerId, out var dto) ? dto : null;
}
