using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.World;

namespace TWL.Tests.Content;

public class ContentValidationTests
{
    private const string MonstersPath = "Content/Data/monsters.json";
    private const string PetsPath = "Content/Data/pets.json";
    private const string SpawnsPath = "Content/Data/spawns";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public void ValidateMonsters_ElementNone_RequiresQuestOnly()
    {
        Assert.True(File.Exists(MonstersPath), $"Monsters file not found at {MonstersPath}");
        var json = File.ReadAllText(MonstersPath);
        var monsters = JsonSerializer.Deserialize<List<MonsterDefinition>>(json, _jsonOptions);

        Assert.NotNull(monsters);

        foreach (var m in monsters)
        {
            if (m.Element == Element.None)
            {
                Assert.Contains("QuestOnly", m.Tags);
            }
        }
    }

    [Fact]
    public void ValidatePets_NoElementNone()
    {
        Assert.True(File.Exists(PetsPath), $"Pets file not found at {PetsPath}");
        var json = File.ReadAllText(PetsPath);
        var pets = JsonSerializer.Deserialize<List<PetDefinition>>(json, _jsonOptions);

        Assert.NotNull(pets);

        foreach (var p in pets)
        {
            Assert.NotEqual(Element.None, p.Element); // Pets cannot be None
        }
    }

    [Fact]
    public void ValidateSpawns_Integrity()
    {
        if (!Directory.Exists(SpawnsPath))
        {
             return;
        }

        var files = Directory.GetFiles(SpawnsPath, "*.spawns.json", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            var json = File.ReadAllText(file);
            var config = JsonSerializer.Deserialize<ZoneSpawnConfig>(json, _jsonOptions);

            Assert.NotNull(config);
            Assert.True(config.MapId > 0, $"Invalid MapId in {file}");

            // Check Regions
            foreach(var region in config.SpawnRegions)
            {
                Assert.True(region.AllowedMonsterIds.Count > 0, $"Region in {file} has no monsters");
            }
        }
    }
}
