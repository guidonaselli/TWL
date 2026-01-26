using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using TWL.Shared.Domain.Characters;

namespace TWL.Server.Simulation.Managers;

public class PetManager
{
    private readonly Dictionary<int, PetDefinition> _definitions = new();
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public void Load(string path)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"Pet definitions file not found at {path}");
            return;
        }

        try
        {
            var json = File.ReadAllText(path);
            var list = JsonSerializer.Deserialize<List<PetDefinition>>(json, _jsonOptions);

            if (list != null)
            {
                foreach (var def in list)
                {
                    if (_definitions.ContainsKey(def.PetTypeId))
                    {
                        Console.WriteLine($"Warning: Duplicate pet ID {def.PetTypeId}");
                    }
                    _definitions[def.PetTypeId] = def;
                }
                Console.WriteLine($"Loaded {_definitions.Count} pet definitions.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading pets: {ex.Message}");
        }
    }

    public PetDefinition? GetDefinition(int petTypeId)
    {
        return _definitions.GetValueOrDefault(petTypeId);
    }
}
