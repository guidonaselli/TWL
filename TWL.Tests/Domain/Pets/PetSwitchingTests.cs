using Xunit;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;

namespace TWL.Tests.Domain.Pets;

public class PetSwitchingTests
{
    [Fact]
    public void SetActivePet_ShouldWork_WhenPetOwned()
    {
        var chara = new ServerCharacter();
        var def = new PetDefinition { PetTypeId = 1, Name = "Test" };
        var pet = new ServerPet(def);
        chara.AddPet(pet);

        // Initial state
        Assert.Null(chara.ActivePetInstanceId);

        // Set Active
        bool result = chara.SetActivePet(pet.InstanceId);
        Assert.True(result);
        Assert.Equal(pet.InstanceId, chara.ActivePetInstanceId);
        Assert.Equal(pet, chara.GetActivePet());
    }

    [Fact]
    public void SetActivePet_ShouldFail_WhenPetNotOwned()
    {
        var chara = new ServerCharacter();

        bool result = chara.SetActivePet("some-guid");
        Assert.False(result);
        Assert.Null(chara.ActivePetInstanceId);
    }

    [Fact]
    public void SetActivePet_ShouldClear_WhenNullPassed()
    {
        var chara = new ServerCharacter();
        var def = new PetDefinition { PetTypeId = 1, Name = "Test" };
        var pet = new ServerPet(def);
        chara.AddPet(pet);
        chara.SetActivePet(pet.InstanceId);

        bool result = chara.SetActivePet(null);
        Assert.True(result);
        Assert.Null(chara.ActivePetInstanceId);
    }
}
