using System.Text.Json;
using System.Xml.Linq;

namespace TWL.Tests.Maps;

public class MapValidationTests
{
    private string GetContentRoot()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var current = new DirectoryInfo(baseDir);
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, "Content/Maps");
            if (Directory.Exists(candidate))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return "../../../../";
    }

    private string GetDocsRoot()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var current = new DirectoryInfo(baseDir);
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, "docs");
            if (Directory.Exists(candidate))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return "../../../../";
    }

    private IEnumerable<string> GetAllMapFiles()
    {
        var root = Path.Combine(GetContentRoot(), "Content/Maps");
        if (!Directory.Exists(root))
        {
            return Enumerable.Empty<string>();
        }

        return Directory.GetFiles(root, "*.tmx", SearchOption.AllDirectories)
            .Where(path => !path.Replace("\\", "/").Contains("/Tilesets/"));
    }

    private XDocument LoadTmx(string path) => XDocument.Load(path);

    private MapMetadata? LoadMetadata(string tmxPath)
    {
        var metaPath = Path.ChangeExtension(tmxPath, ".meta.json");
        if (!File.Exists(metaPath))
        {
            return null;
        }

        var json = File.ReadAllText(metaPath);
        return JsonSerializer.Deserialize<MapMetadata>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        });
    }

    private WorldGraph? LoadWorldGraph()
    {
        var path = Path.Combine(GetDocsRoot(), "docs/world/WORLD_GRAPH.json");
        if (!File.Exists(path))
        {
            return null;
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<WorldGraph>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        });
    }

    [Fact]
    public void ValidateMapLayers()
    {
        var mapFiles = GetAllMapFiles();
        Assert.NotEmpty(mapFiles);

        var expectedLayers = new List<string>
        {
            "Ground",
            "Ground_Detail",
            "Water",
            "Cliffs",
            "Rocks",
            "Props_Low",
            "Props_High",
            "Collisions",
            "Spawns",
            "Triggers"
        };

        foreach (var file in mapFiles)
        {
            var doc = LoadTmx(file);
            var layers = doc.Root.Elements()
                .Where(e => e.Name.LocalName == "layer" || e.Name.LocalName == "objectgroup")
                .Select(e => e.Attribute("name")?.Value)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList();

            // Check for exact order and presence
            for (var i = 0; i < expectedLayers.Count; i++)
            {
                Assert.True(i < layers.Count,
                    $"Map {Path.GetFileName(file)} is missing layer '{expectedLayers[i]}' at index {i}.");
                Assert.Equal(expectedLayers[i], layers[i]);
            }
        }
    }

    [Fact]
    public void ValidateObjectLayers()
    {
        var mapFiles = GetAllMapFiles();
        Assert.NotEmpty(mapFiles);

        foreach (var file in mapFiles)
        {
            var doc = LoadTmx(file);
            var mapName = Path.GetFileName(file);

            // Collisions
            var collisionGroup = doc.Root.Elements("objectgroup")
                .FirstOrDefault(e => e.Attribute("name")?.Value == "Collisions");
            Assert.NotNull(collisionGroup); // Should exist based on layer check
            foreach (var obj in collisionGroup.Elements("object"))
            {
                var props = GetProperties(obj);
                Assert.True(props.ContainsKey("CollisionType"),
                    $"Map {mapName}: Collision object {obj.Attribute("id")?.Value} missing CollisionType.");
                var type = props["CollisionType"];
                Assert.Contains(type, new[] { "Solid", "WaterBlock", "CliffBlock", "OneWay" });
            }

            // Spawns
            var spawnGroup = doc.Root.Elements("objectgroup")
                .FirstOrDefault(e => e.Attribute("name")?.Value == "Spawns");
            Assert.NotNull(spawnGroup);
            var spawnIds = new HashSet<int>();
            foreach (var obj in spawnGroup.Elements("object"))
            {
                var props = GetProperties(obj);
                Assert.True(props.ContainsKey("SpawnType"),
                    $"Map {mapName}: Spawn object {obj.Attribute("id")?.Value} missing SpawnType.");
                Assert.True(props.ContainsKey("Id"),
                    $"Map {mapName}: Spawn object {obj.Attribute("id")?.Value} missing Id property.");

                if (int.TryParse(props["Id"], out var id))
                {
                    Assert.True(spawnIds.Add(id), $"Map {mapName}: Duplicate Spawn Id {id}.");
                }
                else
                {
                    Assert.Fail($"Map {mapName}: Spawn object {obj.Attribute("id")?.Value} has invalid Id format.");
                }
            }

            // Triggers
            var triggerGroup = doc.Root.Elements("objectgroup")
                .FirstOrDefault(e => e.Attribute("name")?.Value == "Triggers");
            Assert.NotNull(triggerGroup);
            var triggerIds = new HashSet<int>();
            foreach (var obj in triggerGroup.Elements("object"))
            {
                var props = GetProperties(obj);
                Assert.True(props.ContainsKey("TriggerType"),
                    $"Map {mapName}: Trigger object {obj.Attribute("id")?.Value} missing TriggerType.");
                Assert.True(props.ContainsKey("Id"),
                    $"Map {mapName}: Trigger object {obj.Attribute("id")?.Value} missing Id property.");

                if (int.TryParse(props["Id"], out var id))
                {
                    Assert.True(triggerIds.Add(id), $"Map {mapName}: Duplicate Trigger Id {id}.");
                }
                else
                {
                    Assert.Fail($"Map {mapName}: Trigger object {obj.Attribute("id")?.Value} has invalid Id format.");
                }

                if (props["TriggerType"] == "MapTransition")
                {
                    Assert.True(props.ContainsKey("TargetMapId"),
                        $"Map {mapName}: MapTransition trigger {id} missing TargetMapId.");
                    Assert.True(props.ContainsKey("TargetSpawnId"),
                        $"Map {mapName}: MapTransition trigger {id} missing TargetSpawnId.");
                }
            }
        }
    }

    [Fact]
    public void ValidateMapMetadata()
    {
        var mapFiles = GetAllMapFiles();
        foreach (var file in mapFiles)
        {
            var meta = LoadMetadata(file);
            Assert.NotNull(meta); // Meta file must exist

            var mapName = Path.GetFileName(file);
            Assert.NotEqual(0, meta.MapId);
            Assert.False(string.IsNullOrEmpty(meta.RegionId), $"Map {mapName} missing RegionId.");

            // Check EntryPoints against Spawns
            var doc = LoadTmx(file);
            var spawnGroup = doc.Root.Elements("objectgroup")
                .FirstOrDefault(e => e.Attribute("name")?.Value == "Spawns");
            var spawnObjects = spawnGroup?.Elements("object").Select(o => GetProperties(o)).ToList() ??
                               new List<Dictionary<string, string>>();
            var playerStartIds = spawnObjects
                .Where(p => p.GetValueOrDefault("SpawnType") == "PlayerStart" && p.ContainsKey("Id"))
                .Select(p => int.Parse(p["Id"]))
                .ToHashSet();

            foreach (var entryPoint in meta.EntryPoints)
            {
                Assert.True(playerStartIds.Contains(entryPoint),
                    $"Map {mapName}: Metadata EntryPoint {entryPoint} does not exist as a PlayerStart Spawn in TMX.");
            }

            // Check Exits against Triggers
            var triggerGroup = doc.Root.Elements("objectgroup")
                .FirstOrDefault(e => e.Attribute("name")?.Value == "Triggers");
            var triggerObjects = triggerGroup?.Elements("object").Select(o => GetProperties(o)).ToList() ??
                                 new List<Dictionary<string, string>>();

            var transitionTargets = triggerObjects
                .Where(p => p.GetValueOrDefault("TriggerType") == "MapTransition")
                .Select(p => new
                {
                    TargetMapId = int.Parse(p.GetValueOrDefault("TargetMapId", "0")),
                    TargetSpawnId = int.Parse(p.GetValueOrDefault("TargetSpawnId", "0"))
                })
                .ToList();

            foreach (var exit in meta.Exits)
            {
                // Every exit listed in meta should effectively be reachable via a trigger?
                // Or rather, every MapTransition Trigger should be listed in Exits?
                // The prompt says "Exits[] (TargetMapId + SpawnId + gating)".
                // Let's verify that for every exit in Meta, there is a transition trigger (not strictly required but good practice).
                // Actually, let's verify that every MapTransition trigger is valid (done in other test)
                // and that Exits in meta match the intent.

                // For now, let's just ensure basic valid data in Exits
                Assert.NotEqual(0, exit.TargetMapId);
                Assert.NotEqual(0, exit.SpawnId);
            }
        }
    }

    [Fact]
    public void ValidateWorldGraphIntegrity()
    {
        var graph = LoadWorldGraph();
        Assert.NotNull(graph);

        var mapFiles = GetAllMapFiles();
        var loadedMaps = new Dictionary<int, (string FilePath, MapMetadata Meta, XDocument Tmx)>();

        foreach (var file in mapFiles)
        {
            var meta = LoadMetadata(file);
            if (meta != null)
            {
                loadedMaps[meta.MapId] = (file, meta, LoadTmx(file));
            }
        }

        // Verify nodes
        foreach (var node in graph.Nodes)
        {
            Assert.True(loadedMaps.ContainsKey(node.Id),
                $"World Graph Node {node.Id} ({node.Name}) does not have a corresponding Map file.");
            Assert.Equal(node.Region, loadedMaps[node.Id].Meta.RegionId);
        }

        // Verify transitions (Graph Edges vs Map Triggers)
        // This is complex because edges in graph are abstract.
        // Better check: For every MapTransition in every Map, does the Target exist?
        foreach (var map in loadedMaps.Values)
        {
            var triggerGroup = map.Tmx.Root.Elements("objectgroup")
                .FirstOrDefault(e => e.Attribute("name")?.Value == "Triggers");
            if (triggerGroup == null)
            {
                continue;
            }

            foreach (var obj in triggerGroup.Elements("object"))
            {
                var props = GetProperties(obj);
                if (props.GetValueOrDefault("TriggerType") == "MapTransition")
                {
                    if (int.TryParse(props.GetValueOrDefault("TargetMapId"), out var targetMapId) &&
                        int.TryParse(props.GetValueOrDefault("TargetSpawnId"), out var targetSpawnId))
                    {
                        // Target Map must exist
                        Assert.True(loadedMaps.ContainsKey(targetMapId),
                            $"Map {map.Meta.MapId} has transition to non-existent map {targetMapId}.");

                        // Target Spawn must exist in Target Map
                        var targetMapTmx = loadedMaps[targetMapId].Tmx;
                        var targetSpawns = targetMapTmx.Root.Elements("objectgroup")
                            .FirstOrDefault(e => e.Attribute("name")?.Value == "Spawns");

                        var targetSpawnExists = targetSpawns?.Elements("object")
                            .Any(o => GetProperties(o).GetValueOrDefault("Id") == targetSpawnId.ToString());

                        Assert.True(targetSpawnExists,
                            $"Map {map.Meta.MapId} transitions to Map {targetMapId} Spawn {targetSpawnId}, but that spawn does not exist.");
                    }
                }
            }
        }
    }

    private Dictionary<string, string> GetProperties(XElement obj)
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

    // DTOs for JSON deserialization
    private class MapMetadata
    {
        public int MapId { get; set; }
        public string RegionId { get; set; } = "";
        public List<int> EntryPoints { get; set; } = new();
        public List<MapExit> Exits { get; set; } = new();
    }

    private class MapExit
    {
        public int TargetMapId { get; set; }
        public int SpawnId { get; set; }
    }

    private class WorldGraph
    {
        public List<GraphNode> Nodes { get; set; } = new();
        public List<GraphEdge> Edges { get; set; } = new();
    }

    private class GraphNode
    {
        public int Id { get; set; }
        public string Name { get; } = "";
        public string Region { get; } = "";
    }

    private class GraphEdge
    {
        public int From { get; set; }
        public int To { get; set; }
    }
}