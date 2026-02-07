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
