using System.Text.Json;
using System.Text.Json.Serialization;
using TWL.Shared.Domain.Characters;

namespace TWL.Tests.Server.Services;

public class ContentValidationTests
{
    private const string MonsterFile = "../../../../Content/Data/monsters.json";

    private List<MonsterDefinition> LoadMonsters()
    {
        var json = File.ReadAllText(MonsterFile);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter());
        return JsonSerializer.Deserialize<List<MonsterDefinition>>(json, options)!;
    }

    [Fact]
    public void Monsters_ElementNone_MustHaveQuestOnlyTag()
    {
        if (!File.Exists(MonsterFile))
        {
            // Skip if running in environment where content is not available or path differs
            // But usually we want to fail.
            Assert.Fail($"Monster file not found at {Path.GetFullPath(MonsterFile)}");
        }

        var monsters = LoadMonsters();

        foreach (var m in monsters)
        {
            if (m.Element == Element.None)
            {
                Assert.Contains("QuestOnly", m.Tags);
            }
        }
    }

    [Fact]
    public void Monsters_ElementNone_MustNotBeCapturable()
    {
        if (!File.Exists(MonsterFile)) return;

        var monsters = LoadMonsters();

        foreach (var m in monsters)
        {
            if (m.Element == Element.None)
            {
                Assert.False(m.IsCapturable, $"Monster {m.Name} ({m.MonsterId}) is Element.None but IsCapturable=true");
            }
        }
    }
}
