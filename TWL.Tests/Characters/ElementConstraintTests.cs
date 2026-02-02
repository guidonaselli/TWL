using TWL.Shared.Domain.Characters;

namespace TWL.Tests.Characters;

public class ElementConstraintTests
{
    [Fact]
    public void PlayerCharacter_CannotBeElementNone()
    {
        Assert.Throws<ArgumentException>(() => new PlayerCharacter(Guid.NewGuid(), "TestPlayer", Element.None));
    }

    [Fact]
    public void PetCharacter_CannotBeElementNone()
    {
        Assert.Throws<ArgumentException>(() => new PetCharacter("TestPet", Element.None));
    }
}
