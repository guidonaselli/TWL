using System.Text.Json;
using System.Text.Json.Serialization;
using TWL.Shared.Domain.Characters;

namespace TWL.Server.Simulation.Managers;

public class MonsterManager
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly Dictionary<int, MonsterDefinition> _definitions = new();

    public void Load(string path)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"Monster definitions file not found at {path}");
            return;
        }

        var json = File.ReadAllText(path);
        var list = JsonSerializer.Deserialize<List<MonsterDefinition>>(json, _jsonOptions);

        if (list != null)
        {
            foreach (var def in list)
            {
                if (def.Element == Element.None && !def.Tags.Contains("QuestOnly"))
                {
                    throw new InvalidDataException($"Monster {def.MonsterId} ({def.Name}) has Element.None but is missing 'QuestOnly' tag.");
                }

                if (_definitions.ContainsKey(def.MonsterId))
                {
                    Console.WriteLine($"Warning: Duplicate monster ID {def.MonsterId}");
                }

                _definitions[def.MonsterId] = def;
            }

            Console.WriteLine($"Loaded {_definitions.Count} monster definitions.");
        }
    }

    public virtual MonsterDefinition? GetDefinition(int monsterId) => _definitions.GetValueOrDefault(monsterId);

    public virtual IEnumerable<MonsterDefinition> GetAllDefinitions() => _definitions.Values;
}