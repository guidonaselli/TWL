using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using TWL.Shared.Domain.Characters;

namespace TWL.Server.Simulation.Managers;

public class MonsterManager
{
    private readonly Dictionary<int, MonsterDefinition> _definitions = new();
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public void Load(string path)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"Monster definitions file not found at {path}");
            return;
        }

        try
        {
            var json = File.ReadAllText(path);
            var list = JsonSerializer.Deserialize<List<MonsterDefinition>>(json, _jsonOptions);

            if (list != null)
            {
                foreach (var def in list)
                {
                    if (_definitions.ContainsKey(def.MonsterId))
                    {
                        Console.WriteLine($"Warning: Duplicate monster ID {def.MonsterId}");
                    }
                    _definitions[def.MonsterId] = def;
                }
                Console.WriteLine($"Loaded {_definitions.Count} monster definitions.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading monsters: {ex.Message}");
        }
    }

    public virtual MonsterDefinition? GetDefinition(int monsterId)
    {
        return _definitions.GetValueOrDefault(monsterId);
    }
}
