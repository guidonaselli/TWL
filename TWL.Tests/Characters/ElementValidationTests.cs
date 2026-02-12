using TWL.Shared.Domain.Characters;

namespace TWL.Tests.Characters;

public class ElementValidationTests
{
    [Fact]
    public void PlayerCharacter_ShouldThrow_WhenElementIsNone()
    {
        var ex = Assert.Throws<ArgumentException>(() => new PlayerCharacter(Guid.NewGuid(), "TestPlayer", Element.None));
        Assert.Contains("PlayerCharacter cannot be Element.None", ex.Message);
    }

    [Fact]
    public void PetCharacter_ShouldThrow_WhenElementIsNone()
    {
        var ex = Assert.Throws<ArgumentException>(() => new PetCharacter("TestPet", Element.None));
        Assert.Contains("PetCharacter cannot be Element.None", ex.Message);
    }

    [Fact]
    public void PlayerCharacter_ShouldSucceed_WhenElementIsFire()
    {
        var player = new PlayerCharacter(Guid.NewGuid(), "TestPlayer", Element.Fire);
        Assert.Equal(Element.Fire, player.CharacterElement);
    }

    [Fact]
    public void PetCharacter_ShouldSucceed_WhenElementIsWater()
    {
        var pet = new PetCharacter("TestPet", Element.Water);
        Assert.Equal(Element.Water, pet.CharacterElement);
    }
}
