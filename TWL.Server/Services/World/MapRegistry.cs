using Microsoft.Extensions.Logging;
using TWL.Server.Domain.World;

namespace TWL.Server.Services.World;

public class MapRegistry : IMapRegistry
{
    private readonly ILogger<MapRegistry> _logger;
    private readonly MapLoader _mapLoader;
    private readonly Dictionary<int, ServerMap> _maps = new();

    public MapRegistry(ILogger<MapRegistry> logger, MapLoader mapLoader)
    {
        _logger = logger;
        _mapLoader = mapLoader;
    }

    public ServerMap? GetMap(int id)
    {
        _maps.TryGetValue(id, out var map);
        return map;
    }

    public IEnumerable<ServerMap> GetAllMaps()
    {
        return _maps.Values;
    }

    public (float X, float Y)? GetEntityPosition(int mapId, string targetName)
    {
        if (!_maps.TryGetValue(mapId, out var map))
        {
            return null;
        }

        // Search triggers
        var trigger = map.Triggers.FirstOrDefault(t =>
            string.Equals(t.Id, targetName, StringComparison.OrdinalIgnoreCase) ||
            (t.Properties.TryGetValue("TargetName", out var tn) && string.Equals(tn, targetName, StringComparison.OrdinalIgnoreCase)) ||
            (t.Properties.TryGetValue("Name", out var n) && string.Equals(n, targetName, StringComparison.OrdinalIgnoreCase))
        );

        if (trigger != null)
        {
            return (trigger.X + trigger.Width / 2f, trigger.Y + trigger.Height / 2f); // Return center of trigger
        }

        // Search spawns
        var spawn = map.Spawns.FirstOrDefault(s =>
            string.Equals(s.Id, targetName, StringComparison.OrdinalIgnoreCase) ||
            (s.Properties.TryGetValue("TargetName", out var tn) && string.Equals(tn, targetName, StringComparison.OrdinalIgnoreCase)) ||
            (s.Properties.TryGetValue("Name", out var n) && string.Equals(n, targetName, StringComparison.OrdinalIgnoreCase))
        );

        if (spawn != null)
        {
            return (spawn.X, spawn.Y);
        }

        return null;
    }

    public void Load(string contentPath)
    {
        if (!Directory.Exists(contentPath))
        {
            _logger.LogWarning("Maps directory not found: {Path}", contentPath);
            return;
        }

        var mapFiles = Directory.GetFiles(contentPath, "*.tmx", SearchOption.AllDirectories);
        int count = 0;

        foreach (var file in mapFiles)
        {
            try
            {
                var map = _mapLoader.LoadMap(file);
                if (_maps.ContainsKey(map.Id))
                {
                    _logger.LogWarning("Duplicate Map ID {Id} in file {File}. Overwriting.", map.Id, file);
                }
                _maps[map.Id] = map;
                count++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load map {File}", file);
            }
        }

        _logger.LogInformation("Loaded {Count} maps from {Path}", count, contentPath);
    }
}
