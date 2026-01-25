using Xunit;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;

namespace TWL.Tests.Domain.Pets;

public class PetAmityTests
{
    [Fact]
    public void TestAmityBounds()
    {
        var def = new PetDefinition { PetTypeId = 1, Name = "Test" };
        var pet = new ServerPet(def);
        pet.Amity = 50;

        pet.ChangeAmity(100);
        Assert.Equal(100, pet.Amity);

        pet.ChangeAmity(-200);
        Assert.Equal(0, pet.Amity);
    }

    [Fact]
    public void TestRebellious()
    {
        var def = new PetDefinition { PetTypeId = 1, Name = "Test" };
        var pet = new ServerPet(def);
        pet.Amity = 10;
        Assert.True(pet.IsRebellious);

        pet.ChangeAmity(20);
        Assert.False(pet.IsRebellious);
    }
}
