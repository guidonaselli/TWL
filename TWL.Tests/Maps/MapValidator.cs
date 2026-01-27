using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Text.Json;

namespace TWL.Tests.Maps
{
    public static class MapValidator
    {
        private static readonly string[] RequiredLayers = new[]
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

        private static readonly string[] ObjectGroupLayers = new[]
        {
            "Collisions",
            "Spawns",
            "Triggers"
        };

        private static readonly HashSet<string> ValidCollisionTypes = new HashSet<string>
        {
            "Solid", "WaterBlock", "CliffBlock", "OneWay"
        };

        private static readonly HashSet<string> ValidSpawnTypes = new HashSet<string>
        {
            "PlayerStart", "Monster", "NPC", "ResourceNode"
        };

        private static readonly HashSet<string> ValidTriggerTypes = new HashSet<string>
        {
            "MapTransition", "QuestHook", "InstanceGate", "CutsceneHook", "Interaction"
        };

        public static void ValidateMap(string mapFolderPath)
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

            string tmxPath = mapFiles[0];
            string metaPath = Path.Combine(mapFolderPath, Path.GetFileNameWithoutExtension(tmxPath) + ".meta.json");

            ValidateTmx(tmxPath);
            ValidateMeta(metaPath);
        }

        private static void ValidateTmx(string path)
        {
            XDocument doc;
            try
            {
                doc = XDocument.Load(path);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load TMX XML: {path}. Error: {ex.Message}");
            }

            var root = doc.Element("map");
            if (root == null)
            {
                throw new Exception("Invalid TMX: missing <map> root element.");
            }

            // Get all layers (tile layers and object groups) in order
            var layers = root.Elements().Where(e => e.Name == "layer" || e.Name == "objectgroup").ToList();

            // Check for missing required layers
            foreach (var requiredLayer in RequiredLayers)
            {
                if (!layers.Any(l => l.Attribute("name")?.Value == requiredLayer))
                {
                    throw new Exception($"Missing required layer: {requiredLayer}");
                }
            }

            var presentRequiredLayers = layers
                .Select(l => l.Attribute("name")?.Value)
                .Where(n => n != null && RequiredLayers.Contains(n))
                .ToList();

            for (int i = 0; i < RequiredLayers.Length; i++)
            {
                if (i >= presentRequiredLayers.Count)
                {
                     throw new Exception($"Missing required layer (checking order): {RequiredLayers[i]}");
                }

                if (presentRequiredLayers[i] != RequiredLayers[i])
                {
                    throw new Exception($"Layer order mismatch. Expected {RequiredLayers[i]}, found {presentRequiredLayers[i]}.");
                }
            }

            // Verify types (ObjectGroup vs TileLayer)
            foreach (var layerName in ObjectGroupLayers)
            {
                var layer = layers.First(l => l.Attribute("name")?.Value == layerName);
                if (layer.Name != "objectgroup")
                {
                     throw new Exception($"Layer '{layerName}' must be an objectgroup, found {layer.Name}.");
                }
            }

            // Verify Tile Layers
            var tileLayers = RequiredLayers.Except(ObjectGroupLayers);
            foreach (var layerName in tileLayers)
            {
                var layer = layers.First(l => l.Attribute("name")?.Value == layerName);
                if (layer.Name != "layer")
                {
                    throw new Exception($"Layer '{layerName}' must be a tile layer (layer), found {layer.Name}.");
                }
            }

            // Validate Object Properties
            ValidateCollisions(layers.First(l => l.Attribute("name")?.Value == "Collisions"));
            ValidateSpawns(layers.First(l => l.Attribute("name")?.Value == "Spawns"));
            ValidateTriggers(layers.First(l => l.Attribute("name")?.Value == "Triggers"));
        }

        private static void ValidateCollisions(XElement layer)
        {
            foreach (var obj in layer.Elements("object"))
            {
                string collisionType = GetProperty(obj, "CollisionType");
                if (string.IsNullOrEmpty(collisionType))
                {
                    throw new Exception($"Collision object (ID {obj.Attribute("id")?.Value}) missing 'CollisionType' property.");
                }
                if (!ValidCollisionTypes.Contains(collisionType))
                {
                    throw new Exception($"Collision object (ID {obj.Attribute("id")?.Value}) has invalid 'CollisionType': {collisionType}.");
                }
            }
        }

        private static void ValidateSpawns(XElement layer)
        {
            foreach (var obj in layer.Elements("object"))
            {
                string spawnType = GetProperty(obj, "SpawnType");
                if (string.IsNullOrEmpty(spawnType))
                {
                    throw new Exception($"Spawn object (ID {obj.Attribute("id")?.Value}) missing 'SpawnType' property.");
                }
                if (!ValidSpawnTypes.Contains(spawnType))
                {
                    throw new Exception($"Spawn object (ID {obj.Attribute("id")?.Value}) has invalid 'SpawnType': {spawnType}.");
                }

                // Check other required properties based on SpawnType
                ValidatePropertyExists(obj, "Id"); // Note: This is a custom property, not the TMX object ID attribute

                if (spawnType == "Monster" || spawnType == "NPC")
                {
                    // Faction, LevelRange, RespawnSeconds, Radius
                    // ValidatePropertyExists(obj, "Faction"); // Not always mandatory? Style guide implies yes.
                }
            }
        }

        private static void ValidateTriggers(XElement layer)
        {
            var triggerIds = new HashSet<string>();

            foreach (var obj in layer.Elements("object"))
            {
                string triggerType = GetProperty(obj, "TriggerType");
                if (string.IsNullOrEmpty(triggerType))
                {
                    throw new Exception($"Trigger object (ID {obj.Attribute("id")?.Value}) missing 'TriggerType' property.");
                }
                if (!ValidTriggerTypes.Contains(triggerType))
                {
                    throw new Exception($"Trigger object (ID {obj.Attribute("id")?.Value}) has invalid 'TriggerType': {triggerType}.");
                }

                string id = GetProperty(obj, "Id");
                if (string.IsNullOrEmpty(id))
                {
                     throw new Exception($"Trigger object (ID {obj.Attribute("id")?.Value}) missing 'Id' property.");
                }

                if (!triggerIds.Add(id))
                {
                    throw new Exception($"Duplicate Trigger ID found: {id}");
                }

                if (triggerType == "MapTransition")
                {
                    ValidatePropertyExists(obj, "TargetMapId");
                    ValidatePropertyExists(obj, "TargetSpawnId");
                }
            }
        }

        private static string GetProperty(XElement obj, string propertyName)
        {
            var props = obj.Element("properties");
            if (props == null) return null;

            var prop = props.Elements("property").FirstOrDefault(p => p.Attribute("name")?.Value == propertyName);
            return prop?.Attribute("value")?.Value;
        }

        private static void ValidatePropertyExists(XElement obj, string propertyName)
        {
             if (string.IsNullOrEmpty(GetProperty(obj, propertyName)))
             {
                 throw new Exception($"Object (ID {obj.Attribute("id")?.Value}) missing required property '{propertyName}'.");
             }
        }

        private static void ValidateMeta(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Metadata file missing: {path}");
            }

            string json = File.ReadAllText(path);
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    if (!root.TryGetProperty("MapId", out _)) throw new Exception("Meta missing 'MapId'");
                    if (!root.TryGetProperty("RegionId", out _)) throw new Exception("Meta missing 'RegionId'");
                    if (!root.TryGetProperty("EntryPoints", out var entries) || entries.ValueKind != JsonValueKind.Array)
                        throw new Exception("Meta missing 'EntryPoints' array");
                    if (!root.TryGetProperty("Exits", out var exits) || exits.ValueKind != JsonValueKind.Array)
                        throw new Exception("Meta missing 'Exits' array");
                }
            }
            catch (JsonException ex)
            {
                throw new Exception($"Invalid JSON in {path}: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Invalid Metadata {path}: {ex.Message}");
            }
        }
    }
}
