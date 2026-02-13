using System.Text.Json;
using System.Text.Json.Serialization;
using TWL.Shared.Domain.Characters;

namespace TWL.Server.Simulation.Managers;

public class PetManager
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly Dictionary<int, PetDefinition> _definitions = new();
    private readonly Dictionary<int, int> _amityItems = new();

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
                    if (def.Element == Element.None)
                    {
                        throw new InvalidDataException($"Pet {def.PetTypeId} ({def.Name}) has invalid Element.None. Pets cannot be None.");
                    }

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

    public void LoadAmityItems(string path)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"Amity items file not found at {path}");
            return;
        }

        try
        {
            var json = File.ReadAllText(path);
            var list = JsonSerializer.Deserialize<List<AmityItemDefinition>>(json, _jsonOptions);

            if (list != null)
            {
                foreach (var item in list)
                {
                    _amityItems[item.ItemId] = item.AmityValue;
                }
                Console.WriteLine($"Loaded {_amityItems.Count} amity item definitions.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading amity items: {ex.Message}");
        }
    }

    public virtual PetDefinition? GetDefinition(int petTypeId) => _definitions.GetValueOrDefault(petTypeId);

    public virtual int GetAmityValue(int itemId) => _amityItems.GetValueOrDefault(itemId, 0);
}