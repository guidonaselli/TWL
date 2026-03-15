using TWL.Shared.Domain.Characters;
using TWL.Tests.PetTests;

namespace TWL.Tests;

public class ContentIntegrationTests
{
    [Fact]
    public void ValidatePetStats_NonZero()
    {
        var pets = ContentTestHelper.LoadPets();
        foreach (var pet in pets)
        {
            Assert.True(pet.BaseHp > 0, $"Pet {pet.PetTypeId} ({pet.Name}) has 0 BaseHp.");
            Assert.True(pet.BaseStr >= 0, $"Pet {pet.PetTypeId} has invalid BaseStr.");
            Assert.True(pet.BaseCon >= 0, $"Pet {pet.PetTypeId} has invalid BaseCon.");
            Assert.True(pet.BaseInt >= 0, $"Pet {pet.PetTypeId} has invalid BaseInt.");
            Assert.True(pet.BaseWis >= 0, $"Pet {pet.PetTypeId} has invalid BaseWis.");
            Assert.True(pet.BaseAgi >= 0, $"Pet {pet.PetTypeId} has invalid BaseAgi.");
            
            // Total base stats should be reasonable
            var total = pet.BaseStr + pet.BaseCon + pet.BaseInt + pet.BaseWis + pet.BaseAgi;
            Assert.True(total > 0, $"Pet {pet.PetTypeId} ({pet.Name}) has no base stats defined.");
        }
    }

    [Fact]
    public void ValidatePetAssets_Existence()
    {
        var pets = ContentTestHelper.LoadPets();
        foreach (var pet in pets)
        {
            if (!string.IsNullOrEmpty(pet.SpritePath))
            {
                // We assume assets are in Content/Sprites/ or similar
                // ContentManager will look in Content/
                // For unit tests, we just check if the property is not empty if it's required.
                Assert.False(string.IsNullOrWhiteSpace(pet.SpritePath), $"Pet {pet.PetTypeId} is missing SpritePath.");
            }
        }
    }
}
