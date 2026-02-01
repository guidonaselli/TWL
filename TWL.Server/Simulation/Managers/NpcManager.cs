using System.Text.Json;
using System.Text.Json.Serialization;
using TWL.Shared.Domain.Characters;

namespace TWL.Server.Simulation.Managers;

public class NpcManager
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly Dictionary<int, NpcDefinition> _definitions = new();

    public void Load(string path)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"NPC definitions file not found at {path}");
            return;
        }

        try
        {
            var json = File.ReadAllText(path);
            var list = JsonSerializer.Deserialize<List<NpcDefinition>>(json, _jsonOptions);

            if (list != null)
            {
                foreach (var def in list)
                {
                    if (_definitions.ContainsKey(def.NpcId))
                    {
                        Console.WriteLine($"Warning: Duplicate NPC ID {def.NpcId}");
                    }

                    _definitions[def.NpcId] = def;
                }

                Console.WriteLine($"Loaded {_definitions.Count} NPC definitions.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading NPCs: {ex.Message}");
        }
    }

    public virtual NpcDefinition? GetDefinition(int npcId) => _definitions.GetValueOrDefault(npcId);
}