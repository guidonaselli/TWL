using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;

namespace TWL.Tests.Persistence;

public class DirtyPropagationTests
{
    [Fact]
    public void IsDirty_ShouldPropagate_FromPet()
    {
        var ch = new ServerCharacter { Id = 1, Name = "Owner" };
        var def = new PetDefinition { PetTypeId = 100, Name = "FluffyDef" };
        var pet = new ServerPet(def);
        ch.AddPet(pet);

        // Initially clean (assuming AddPet sets dirty, which we clear)
        ch.IsDirty = false;
        pet.IsDirty = false;
        Assert.False(ch.IsDirty);
        Assert.False(pet.IsDirty);

        // Modify pet
        pet.AddExp(10);
        Assert.True(pet.IsDirty, "Pet should be dirty after AddExp");
        Assert.True(ch.IsDirty, "Character should be dirty when pet is dirty");

        // Clear dirty on character
        ch.IsDirty = false;
        Assert.False(ch.IsDirty, "Character should be clean");
        Assert.False(pet.IsDirty, "Pet should be clean after clearing character dirty flag");
    }

    [Fact]
    public void IsDirty_ShouldSet_OnSkillUsage()
    {
        var ch = new ServerCharacter { Id = 1, Name = "Mage" };
        ch.IsDirty = false; // Reset initial dirty state

        // Use skill
        ch.IncrementSkillUsage(123);

        Assert.True(ch.IsDirty, "Character should be dirty after skill usage");
    }

    [Fact]
    public void IsDirty_ShouldPropagate_FromMultiplePets()
    {
        var ch = new ServerCharacter();
        var p1 = new ServerPet();
        var p2 = new ServerPet();
        ch.AddPet(p1);
        ch.AddPet(p2);

        ch.IsDirty = false;
        p1.IsDirty = false;
        p2.IsDirty = false;

        p2.ChangeAmity(5);
        Assert.True(p2.IsDirty);
        Assert.True(ch.IsDirty);

        ch.IsDirty = false;
        Assert.False(p1.IsDirty);
        Assert.False(p2.IsDirty);
        Assert.False(ch.IsDirty);
    }
}