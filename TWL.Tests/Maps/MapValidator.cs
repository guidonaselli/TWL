using System.Text.Json;
using System.Xml.Linq;
using TWL.Shared.Domain.World;

namespace TWL.Tests.Maps;

public static class MapValidator
{
    public static void ValidateMap(string mapFolderPath, WorldGraph? worldGraph = null)
    {
        if (!Directory.Exists(mapFolderPath))
        {
            throw new DirectoryNotFoundException($"Map folder not found: {mapFolderPath}");
        }

        var mapFiles = Directory.GetFiles(mapFolderPath, "*.tmx");
        if (mapFiles.Length == 0)
        {
            throw new FileNotFoundException($"No .tmx file found in {mapFolderPath}");
        }

        if (mapFiles.Length > 1)
        {
            throw new Exception($"Multiple .tmx files found in {mapFolderPath}");
        }

        var tmxPath = mapFiles[0];
        var metaPath = Path.Combine(mapFolderPath, Path.GetFileNameWithoutExtension(tmxPath) + ".meta.json");

        var mapDoc = LoadTmx(tmxPath);
        var meta = ValidateMeta(metaPath);

        ValidateTmxStructure(mapDoc, tmxPath);
        ValidateObjectProperties(mapDoc, meta, worldGraph);
    }

    public static void ValidateWorldGraph(WorldGraph graph, string mapsRoot)
    {
        // 1. Verify Nodes exist as maps
        var allMapFiles = Directory.GetFiles(mapsRoot, "*.tmx", SearchOption.AllDirectories)
            .Where(path => !path.Replace("\\", "/").Contains("/Tilesets/"))
            .ToList();

        var mapIdToFile = new Dictionary<int, string>();

        foreach (var file in allMapFiles)
        {
            var metaPath = Path.ChangeExtension(file, ".meta.json");
            if (File.Exists(metaPath))
            {
                var meta = LoadMeta(metaPath);
                mapIdToFile[meta.MapId] = file;
            }
        }

        foreach (var node in graph.Nodes)
        {
            if (!mapIdToFile.ContainsKey(node.Id))
            {
                throw new Exception($"World Graph Node {node.Id} ({node.Name}) does not have a corresponding Map file.");
            }
        }

        // 2. Verify Edges have transitions
        foreach (var edge in graph.Edges)
        {
            if (!mapIdToFile.ContainsKey(edge.From)) continue; // Handled by node check

            var fromMapFile = mapIdToFile[edge.From];
            var doc = LoadTmx(fromMapFile);
            var triggers = GetObjects(doc, "Triggers");

            bool transitionFound = false;
            foreach (var obj in triggers)
            {
                var props = GetProperties(obj);
                if (props.GetValueOrDefault("TriggerType") == "MapTransition" &&
                    props.GetValueOrDefault("TargetMapId") == edge.To.ToString())
                {
                    transitionFound = true;
                    break;
                }
            }

            if (!transitionFound)
            {
                throw new Exception($"World Graph defines edge from {edge.From} to {edge.To}, but Map {edge.From} has no MapTransition trigger to Map {edge.To}.");
            }
        }
    }

    private static XDocument LoadTmx(string path)
    {
        try
        {
            return XDocument.Load(path);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to load TMX XML: {path}. Error: {ex.Message}");
        }
    }

    private static void ValidateTmxStructure(XDocument doc, string path)
    {
        var root = doc.Element("map");
        if (root == null)
        {
            throw new Exception("Invalid TMX: missing <map> root element.");
        }

        // Get all layers (tile layers and object groups) in order
        var layers = root.Elements().Where(e => e.Name == "layer" || e.Name == "objectgroup").ToList();

        // Check for missing required layers
        foreach (var requiredLayer in WorldConstants.RequiredLayers)
        {
            if (!layers.Any(l => l.Attribute("name")?.Value == requiredLayer))
            {
                throw new Exception($"Missing required layer: {requiredLayer}");
            }
        }

        var presentRequiredLayers = layers
            .Select(l => l.Attribute("name")?.Value)
            .Where(n => n != null && WorldConstants.RequiredLayers.Contains(n))
            .ToList();

        for (var i = 0; i < WorldConstants.RequiredLayers.Count; i++)
        {
            if (i >= presentRequiredLayers.Count)
            {
                throw new Exception($"Missing required layer (checking order): {WorldConstants.RequiredLayers[i]}");
            }

            if (presentRequiredLayers[i] != WorldConstants.RequiredLayers[i])
            {
                throw new Exception(
                    $"Layer order mismatch. Expected {WorldConstants.RequiredLayers[i]}, found {presentRequiredLayers[i]}.");
            }
        }

        // Verify types (ObjectGroup vs TileLayer)
        foreach (var layerName in WorldConstants.ObjectGroupLayers)
        {
            var layer = layers.First(l => l.Attribute("name")?.Value == layerName);
            if (layer.Name != "objectgroup")
            {
                throw new Exception($"Layer '{layerName}' must be an objectgroup, found {layer.Name}.");
            }
        }

        // Verify Tile Layers
        var tileLayers = WorldConstants.RequiredLayers.Except(WorldConstants.ObjectGroupLayers);
        foreach (var layerName in tileLayers)
        {
            var layer = layers.First(l => l.Attribute("name")?.Value == layerName);
            if (layer.Name != "layer")
            {
                throw new Exception($"Layer '{layerName}' must be a tile layer (layer), found {layer.Name}.");
            }
        }
    }

    private static void ValidateObjectProperties(XDocument doc, MapMetadata meta, WorldGraph? worldGraph)
    {
        ValidateCollisions(GetObjects(doc, "Collisions"));
        ValidateSpawns(GetObjects(doc, "Spawns"), meta);
        ValidateTriggers(GetObjects(doc, "Triggers"), meta, worldGraph);
    }

    private static IEnumerable<XElement> GetObjects(XDocument doc, string layerName)
    {
        var layer = doc.Root.Elements("objectgroup").FirstOrDefault(l => l.Attribute("name")?.Value == layerName);
        return layer?.Elements("object") ?? Enumerable.Empty<XElement>();
    }

    private static void ValidateCollisions(IEnumerable<XElement> objects)
    {
        foreach (var obj in objects)
        {
            var props = GetProperties(obj);
            if (!props.TryGetValue("CollisionType", out var collisionType))
            {
                throw new Exception($"Collision object (ID {obj.Attribute("id")?.Value}) missing 'CollisionType' property.");
            }

            if (!WorldConstants.ValidCollisionTypes.Contains(collisionType))
            {
                throw new Exception($"Collision object (ID {obj.Attribute("id")?.Value}) has invalid 'CollisionType': {collisionType}.");
            }
        }
    }

    private static void ValidateSpawns(IEnumerable<XElement> objects, MapMetadata meta)
    {
        var playerStartIds = new HashSet<int>();
        var spawnIds = new HashSet<int>();

        foreach (var obj in objects)
        {
            var props = GetProperties(obj);
            if (!props.TryGetValue("SpawnType", out var spawnType))
            {
                throw new Exception($"Spawn object (ID {obj.Attribute("id")?.Value}) missing 'SpawnType' property.");
            }

            if (!WorldConstants.ValidSpawnTypes.Contains(spawnType))
            {
                throw new Exception($"Spawn object (ID {obj.Attribute("id")?.Value}) has invalid 'SpawnType': {spawnType}.");
            }

            if (!props.TryGetValue("Id", out var idStr) || !int.TryParse(idStr, out var id))
            {
                throw new Exception($"Spawn object (ID {obj.Attribute("id")?.Value}) missing or invalid 'Id' property.");
            }

            if (!spawnIds.Add(id))
            {
                throw new Exception($"Duplicate Spawn ID found: {id}");
            }

            if (spawnType == "PlayerStart")
            {
                playerStartIds.Add(id);
            }

            if (spawnType == "Monster")
            {
                if (!props.ContainsKey("MonsterId"))
                {
                    throw new Exception($"Monster Spawn (ID {obj.Attribute("id")?.Value}) missing 'MonsterId' property.");
                }
            }
        }

        // Validate EntryPoints
        foreach (var entryPoint in meta.EntryPoints)
        {
            if (!playerStartIds.Contains(entryPoint))
            {
                throw new Exception($"Metadata EntryPoint {entryPoint} does not exist as a PlayerStart Spawn in TMX.");
            }
        }
    }

    private static void ValidateTriggers(IEnumerable<XElement> objects, MapMetadata meta, WorldGraph? worldGraph)
    {
        var triggerIds = new HashSet<int>();

        foreach (var obj in objects)
        {
            var props = GetProperties(obj);
            if (!props.TryGetValue("TriggerType", out var triggerType))
            {
                throw new Exception($"Trigger object (ID {obj.Attribute("id")?.Value}) missing 'TriggerType' property.");
            }

            if (!WorldConstants.ValidTriggerTypes.Contains(triggerType))
            {
                throw new Exception($"Trigger object (ID {obj.Attribute("id")?.Value}) has invalid 'TriggerType': {triggerType}.");
            }

            if (!props.TryGetValue("Id", out var idStr) || !int.TryParse(idStr, out var id))
            {
                throw new Exception($"Trigger object (ID {obj.Attribute("id")?.Value}) missing or invalid 'Id' property.");
            }

            if (!triggerIds.Add(id))
            {
                throw new Exception($"Duplicate Trigger ID found: {id}");
            }

            if (triggerType == "MapTransition")
            {
                if (!props.TryGetValue("TargetMapId", out var targetMapIdStr) || !int.TryParse(targetMapIdStr, out var targetMapId))
                {
                     throw new Exception($"MapTransition Trigger (ID {id}) missing or invalid 'TargetMapId'.");
                }

                if (!props.ContainsKey("TargetSpawnId"))
                {
                    throw new Exception($"MapTransition Trigger (ID {id}) missing 'TargetSpawnId'.");
                }

                if (worldGraph != null)
                {
                    // Check if TargetMapId exists in the graph
                    if (!worldGraph.Nodes.Any(n => n.Id == targetMapId))
                    {
                         throw new Exception($"MapTransition Trigger (ID {id}) points to non-existent Map ID {targetMapId} in World Graph.");
                    }
                }
            }
        }
    }

    private static Dictionary<string, string> GetProperties(XElement obj)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var propsElement = obj.Element("properties");
        if (propsElement != null)
        {
            foreach (var prop in propsElement.Elements("property"))
            {
                var name = prop.Attribute("name")?.Value;
                var value = prop.Attribute("value")?.Value;
                if (!string.IsNullOrEmpty(name))
                {
                    dict[name] = value ?? "";
                }
            }
        }
        return dict;
    }

    private static MapMetadata LoadMeta(string path)
    {
         if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Metadata file missing: {path}");
        }

        var json = File.ReadAllText(path);
        try
        {
            var meta = JsonSerializer.Deserialize<MapMetadata>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });
            return meta ?? throw new Exception("Deserialized meta is null");
        }
        catch (JsonException ex)
        {
            throw new Exception($"Invalid JSON in {path}: {ex.Message}");
        }
    }

    private static MapMetadata ValidateMeta(string path)
    {
        var meta = LoadMeta(path);

        if (meta.MapId == 0) throw new Exception("Meta missing or invalid 'MapId'");
        if (string.IsNullOrEmpty(meta.RegionId)) throw new Exception("Meta missing 'RegionId'");
        if (meta.EntryPoints == null) throw new Exception("Meta missing 'EntryPoints' array");
        if (meta.Exits == null) throw new Exception("Meta missing 'Exits' array");

        return meta;
    }
}

public class MapMetadata
{
    public int MapId { get; set; }
    public string RegionId { get; set; } = "";
    public List<int> EntryPoints { get; set; } = new();
    public List<MapExit> Exits { get; set; } = new();
}

public class MapExit
{
    public int TargetMapId { get; set; }
    public int SpawnId { get; set; }
}

public class WorldGraph
{
    public List<GraphNode> Nodes { get; set; } = new();
    public List<GraphEdge> Edges { get; set; } = new();
}

public class GraphNode
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Region { get; set; } = "";
}

public class GraphEdge
{
    public int From { get; set; }
    public int To { get; set; }
}
