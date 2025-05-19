using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TWL.Shared.Domain.DTO;

namespace TWL.Client.Presentation.Services;

public class JsonPlayerColorsService : IPlayerColorsService
{
    private readonly Dictionary<Guid,PlayerColorsDto> _map;

    public JsonPlayerColorsService(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var raw = JsonSerializer.Deserialize<Dictionary<string,PlayerColorsDto>>(json)
                  ?? new();
        _map = raw.ToDictionary(
            kv => Guid.Parse(kv.Key),
            kv => kv.Value);
    }

    public PlayerColorsDto? Get(Guid playerId)
        => _map.TryGetValue(playerId, out var dto) ? dto : null;
}