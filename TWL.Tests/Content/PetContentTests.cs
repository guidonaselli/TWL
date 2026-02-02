using System.Text.Json;
using System.Text.Json.Serialization;
using TWL.Shared.Domain.Characters;

namespace TWL.Tests.Content;

public class PetContentTests
{
    private const string PetsPath = "Content/Data/pets.json";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public void ValidatePetDefinitions()
    {
        Assert.True(File.Exists(PetsPath), $"Pets file not found at {PetsPath}");

        var json = File.ReadAllText(PetsPath);
        var pets = JsonSerializer.Deserialize<List<PetDefinition>>(json, _jsonOptions);

        Assert.NotNull(pets);
        Assert.NotEmpty(pets);

        var ids = new HashSet<int>();

        foreach (var pet in pets)
        {
            // 1. Unique IDs
            Assert.True(ids.Add(pet.PetTypeId), $"Duplicate Pet ID: {pet.PetTypeId}");

            // 2. Element Validity
            Assert.NotEqual(Element.None, pet.Element); // Element.None is forbidden for pets

            // 3. Name
            Assert.False(string.IsNullOrWhiteSpace(pet.Name), $"Pet {pet.PetTypeId} has no name");

            // 4. Capture Rules
            if (pet.Type == PetType.Capture)
            {
                Assert.True(pet.CaptureRules.IsCapturable, $"Capture Pet {pet.PetTypeId} must be capturable");
                Assert.True(pet.CaptureRules.BaseChance > 0, $"Capture Pet {pet.PetTypeId} must have base chance > 0");
            }
            else if (pet.IsQuestUnique)
            {
                Assert.False(pet.CaptureRules.IsCapturable, $"Quest Unique Pet {pet.PetTypeId} cannot be capturable");
            }

            // 5. Growth Model
            Assert.NotNull(pet.GrowthModel);
            Assert.True(pet.BaseHp > 0, $"Pet {pet.PetTypeId} has invalid BaseHp");
        }
    }
}
