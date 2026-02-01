using System.Text.Json;
using TWL.Shared.Domain.Characters;

namespace TWL.Client.Presentation.Managers;

public class PetDefinitionManager
{
    private readonly Dictionary<int, PetDefinition> _pets;

    public PetDefinitionManager()
    {
        _pets = new Dictionary<int, PetDefinition>();
    }

    public int Count => _pets.Count;

    public void Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Pet definitions file not found at path: {path}");
        }

        var jsonContent = File.ReadAllText(path);
        var petList = JsonSerializer.Deserialize<List<PetDefinition>>(jsonContent);

        _pets.Clear();
        foreach (var pet in petList)
        {
            _pets[pet.PetTypeId] = pet;
        }
    }

    public PetDefinition Get(int petTypeId)
    {
        if (_pets.TryGetValue(petTypeId, out var petDefinition))
        {
            return petDefinition;
        }

        return null;
    }

    public bool Exists(int petTypeId) => _pets.ContainsKey(petTypeId);

    public IEnumerable<PetDefinition> GetAll() => _pets.Values;
}