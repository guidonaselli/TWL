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

            // Verify order
            int currentRequiredIndex = 0;
            foreach (var layer in layers)
            {
                string name = layer.Attribute("name")?.Value ?? "";
                if (string.IsNullOrEmpty(name)) continue;

                // If this is one of our required layers, check if it's in the right sequence relative to other required layers
                if (RequiredLayers.Contains(name))
                {
                    int expectedIndex = Array.IndexOf(RequiredLayers, name);
                    // We allow non-required layers in between, but the required ones must satisfy their relative order
                    // Wait, the style guide says "Must include exactly these layers in this exact order".
                    // Does it imply NO other layers allowed? Or just relative order?
                    // "Must include exactly these layers in this exact order" suggests strictness.
                    // But usually TMX might have meta layers. Let's enforce strict order for the required ones,
                    // and maybe warn or allow others if they are not reserved names.

                    // Let's implement strict order check for the required set.
                    // If we find a required layer that appears BEFORE a previous required layer, that's an error.
                    // Actually, simpler: Filter the layers in the file to only those in RequiredLayers list.
                    // Then compare that list to RequiredLayers.
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
