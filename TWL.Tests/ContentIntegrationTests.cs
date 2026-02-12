using System.Text.Json;
using System.Text.Json.Serialization;
using TWL.Shared.Domain.Characters;

namespace TWL.Tests;

public class ContentIntegrationTests
{
    private string GetContentRoot()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var current = new DirectoryInfo(baseDir);
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, "Content/Data");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return "../../../../Content/Data";
    }

    private JsonSerializerOptions GetJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            IncludeFields = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private List<T> LoadContent<T>(string filename)
    {
        var root = GetContentRoot();
        var path = Path.Combine(root, filename);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Could not find {filename} at {Path.GetFullPath(path)}");
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<T>>(json, GetJsonOptions()) ?? new List<T>();
    }

    [Fact]
    public void ValidatePetsContent()
    {
        var pets = LoadContent<PetDefinition>("pets.json");

        // Verify new pets
        var otter = pets.Find(p => p.PetTypeId == 1011);
        Assert.NotNull(otter);
        Assert.Equal("Tiny Sea Otter", otter.Name);
        Assert.NotNull(otter.SpritePath);
        Assert.NotNull(otter.PortraitPath);

        var gecko = pets.Find(p => p.PetTypeId == 1012);
        Assert.NotNull(gecko);
        Assert.NotNull(gecko.SpritePath);

        var turtle = pets.Find(p => p.PetTypeId == 1013);
        Assert.NotNull(turtle);
        Assert.NotNull(turtle.SpritePath);

        // Verify Asset Paths exist
        var repoRoot = GetRepoRoot();

        foreach (var pet in new[] { otter, gecko, turtle })
        {
            var spritePath = Path.Combine(repoRoot, "TWL.Client/Content/Sprites", pet.SpritePath);
            var portraitPath = Path.Combine(repoRoot, "TWL.Client/Content/Sprites", pet.PortraitPath);

            Assert.True(File.Exists(spritePath), $"Missing sprite for {pet.Name}: {spritePath}");
            Assert.True(File.Exists(portraitPath), $"Missing portrait for {pet.Name}: {portraitPath}");
        }
    }

    [Fact]
    public void ValidateMonstersContent()
    {
        var monsters = LoadContent<MonsterDefinition>("monsters.json");
        Assert.Equal(14, monsters.Count);

        // Verify a few sample mobs
        Assert.Contains(monsters, m => m.MonsterId == 2001 && m.Element == Element.Earth);
        Assert.Contains(monsters, m => m.MonsterId == 2004 && m.Element == Element.Wind);
        Assert.Contains(monsters, m => m.MonsterId == 2012 && m.Name.Contains("Vine"));

        // Verify new fields
        foreach (var mob in monsters)
        {
            Assert.False(string.IsNullOrEmpty(mob.Code), $"Monster {mob.MonsterId} missing Code");
            Assert.True(mob.FamilyId > 0, $"Monster {mob.MonsterId} invalid FamilyId");
            Assert.NotNull(mob.Tags);
            Assert.NotNull(mob.Behavior);

            // Validate Element.None rule
            if (mob.Element == Element.None)
            {
                Assert.Contains("QuestOnly", mob.Tags);
            }

            // EncounterWeight > 0 required unless QuestOnly (not spawning randomly)
            if (!mob.Tags.Contains("QuestOnly"))
            {
                Assert.True(mob.EncounterWeight > 0, $"Monster {mob.MonsterId} invalid EncounterWeight");
            }
        }

        // Verify Asset Paths exist
        var repoRoot = GetRepoRoot();

        foreach (var mob in monsters)
        {
            var spritePath = Path.Combine(repoRoot, "TWL.Client/Content/Sprites", mob.SpritePath);
            var portraitPath = Path.Combine(repoRoot, "TWL.Client/Content/Sprites", mob.PortraitPath);

            Assert.True(File.Exists(spritePath), $"Missing sprite for {mob.Name}: {spritePath}");
            Assert.True(File.Exists(portraitPath), $"Missing portrait for {mob.Name}: {portraitPath}");
        }
    }

    [Fact]
    public void ValidateNpcsContent()
    {
        var npcs = LoadContent<NpcDefinition>("npcs.json");
        Assert.Equal(8, npcs.Count);

        Assert.Contains(npcs, n => n.NpcId == 3001 && n.Name == "Harbor Quartermaster");
        Assert.Contains(npcs, n => n.NpcId == 3002 && n.Name == "Herbalist Apprentice");
        Assert.Contains(npcs, n => n.NpcId == 3003 && n.Name == "Wandering Tinkerer");

        var repoRoot = GetRepoRoot();

        foreach (var npc in npcs)
        {
            var spritePath = Path.Combine(repoRoot, "TWL.Client/Content/Sprites", npc.SpritePath);
            var portraitPath = Path.Combine(repoRoot, "TWL.Client/Content/Sprites", npc.PortraitPath);

            Assert.True(File.Exists(spritePath), $"Missing sprite for {npc.Name}: {spritePath}");
            Assert.True(File.Exists(portraitPath), $"Missing portrait for {npc.Name}: {portraitPath}");
        }
    }

    private string GetRepoRoot()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var current = new DirectoryInfo(baseDir);
        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "TWL.Client")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return "../../../../"; // Fallback
    }
}