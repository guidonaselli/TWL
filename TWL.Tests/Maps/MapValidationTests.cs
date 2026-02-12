using System.Text.Json;

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
    public void ValidateAllMaps()
    {
        var mapsRoot = Path.Combine(GetContentRoot(), "Content/Maps");
        if (!Directory.Exists(mapsRoot))
        {
            // If Content/Maps doesn't exist, this might be a CI environment without content,
            // or just a test run without mapped drives. But for this agent task, it should exist.
            Assert.Fail($"Content/Maps folder not found at {mapsRoot}");
        }

        var mapFiles = Directory.GetFiles(mapsRoot, "*.tmx", SearchOption.AllDirectories)
            .Where(path => !path.Replace("\\", "/").Contains("/Tilesets/"))
            .ToList();

        Assert.NotEmpty(mapFiles);

        var graph = LoadWorldGraph();
        Assert.NotNull(graph);

        foreach (var file in mapFiles)
        {
            var folder = Path.GetDirectoryName(file);
            if (folder == null) continue;

            try
            {
                MapValidator.ValidateMap(folder, graph);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Validation failed for map {Path.GetFileName(file)}: {ex.Message}");
            }
        }
    }

    [Fact]
    public void ValidateWorldGraph()
    {
        var mapsRoot = Path.Combine(GetContentRoot(), "Content/Maps");
        var graph = LoadWorldGraph();
        Assert.NotNull(graph);

        try
        {
            MapValidator.ValidateWorldGraph(graph, mapsRoot);
        }
        catch (Exception ex)
        {
            Assert.Fail($"World Graph validation failed: {ex.Message}");
        }
    }
}
