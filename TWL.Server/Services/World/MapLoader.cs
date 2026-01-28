using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using TWL.Server.Domain.World;
using TWL.Shared.Domain.World;

namespace TWL.Server.Services.World;

public class MapLoader
{
    private readonly ILogger<MapLoader> _logger;

    public MapLoader(ILogger<MapLoader> logger)
    {
        _logger = logger;
    }

    public ServerMap LoadMap(string tmxPath)
    {
        if (!File.Exists(tmxPath))
        {
            throw new FileNotFoundException($"TMX file not found: {tmxPath}");
        }

        XDocument doc;
        try
        {
            doc = XDocument.Load(tmxPath);
        }
        catch (Exception ex)
        {
             throw new Exception($"Failed to load TMX XML: {tmxPath}. Error: {ex.Message}", ex);
        }

        var root = doc.Element("map");
        if (root == null)
        {
            throw new Exception("Invalid TMX: missing <map> root element.");
        }

        var map = new ServerMap();

        // TMX attributes
        if (int.TryParse(root.Attribute("width")?.Value, out var w)) map.Width = w;
        if (int.TryParse(root.Attribute("height")?.Value, out var h)) map.Height = h;
        if (int.TryParse(root.Attribute("tilewidth")?.Value, out var tw)) map.TileWidth = tw;
        if (int.TryParse(root.Attribute("tileheight")?.Value, out var th)) map.TileHeight = th;

        // Parse ID from meta.json if exists
        string metaPath = Path.ChangeExtension(tmxPath, ".meta.json");
        if (File.Exists(metaPath))
        {
             try
             {
                 var json = File.ReadAllText(metaPath);
                 using var jsonDoc = JsonDocument.Parse(json);
                 if (jsonDoc.RootElement.TryGetProperty("MapId", out var mapIdProp))
                 {
                     map.Id = mapIdProp.GetInt32();
                 }
                 if (jsonDoc.RootElement.TryGetProperty("Name", out var nameProp))
                 {
                     map.Name = nameProp.GetString() ?? string.Empty;
                 }
             }
             catch(Exception ex)
             {
                 _logger.LogWarning(ex, "Failed to parse meta.json for map {Path}", tmxPath);
             }
        }
        else
        {
             _logger.LogWarning("No meta.json found for map {Path}, MapId will be 0", tmxPath);
        }

        var objectGroups = root.Elements("objectgroup").ToList();

        // Triggers
        var triggerLayer = objectGroups.FirstOrDefault(l => l.Attribute("name")?.Value == "Triggers");
        if (triggerLayer != null)
        {
            foreach (var obj in triggerLayer.Elements("object"))
            {
                var trigger = ParseTrigger(obj);
                map.Triggers.Add(trigger);
            }
        }

        // Spawns
        var spawnLayer = objectGroups.FirstOrDefault(l => l.Attribute("name")?.Value == "Spawns");
        if (spawnLayer != null)
        {
            foreach (var obj in spawnLayer.Elements("object"))
            {
                var spawn = ParseSpawn(obj);
                map.Spawns.Add(spawn);
            }
        }

        return map;
    }

    private ServerTrigger ParseTrigger(XElement obj)
    {
        var trigger = new ServerTrigger
        {
            // TMX object ID is distinct from our logical "Id" property, but useful as fallback
            Id = obj.Attribute("id")?.Value ?? string.Empty,
            Properties = ParseProperties(obj)
        };

        if (float.TryParse(obj.Attribute("x")?.Value, out var x)) trigger.X = x;
        if (float.TryParse(obj.Attribute("y")?.Value, out var y)) trigger.Y = y;
        if (float.TryParse(obj.Attribute("width")?.Value, out var w)) trigger.Width = w;
        if (float.TryParse(obj.Attribute("height")?.Value, out var h)) trigger.Height = h;

        if (trigger.Properties.TryGetValue("TriggerType", out var type))
        {
            trigger.Type = type;
        }

        // Logical Id overrides TMX Id
        if (trigger.Properties.TryGetValue("Id", out var customId))
        {
             trigger.Id = customId;
        }

        return trigger;
    }

    private ServerSpawn ParseSpawn(XElement obj)
    {
        var spawn = new ServerSpawn
        {
            Id = obj.Attribute("id")?.Value ?? string.Empty,
            Properties = ParseProperties(obj)
        };

        if (float.TryParse(obj.Attribute("x")?.Value, out var x)) spawn.X = x;
        if (float.TryParse(obj.Attribute("y")?.Value, out var y)) spawn.Y = y;

        if (spawn.Properties.TryGetValue("SpawnType", out var type))
        {
            spawn.Type = type;
        }

        if (spawn.Properties.TryGetValue("Id", out var customId))
        {
             spawn.Id = customId;
        }

        return spawn;
    }

    private Dictionary<string, string> ParseProperties(XElement obj)
    {
        var dict = new Dictionary<string, string>();
        var props = obj.Element("properties");
        if (props != null)
        {
            foreach (var prop in props.Elements("property"))
            {
                var name = prop.Attribute("name")?.Value;
                var value = prop.Attribute("value")?.Value;
                if (!string.IsNullOrEmpty(name))
                {
                    dict[name] = value ?? string.Empty;
                }
            }
        }
        return dict;
    }
}
