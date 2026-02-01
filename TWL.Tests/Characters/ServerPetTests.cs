using Xunit;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;

namespace TWL.Tests.Characters;

public class ServerPetTests
{
    [Fact]
    public void Constructor_ShouldInitializeFromDefinition()
    {
        var def = new PetDefinition
        {
            PetTypeId = 101,
            Name = "Test Parrot",
            BaseHp = 30,
            IsQuestUnique = true,
            SkillIds = new System.Collections.Generic.List<int>(),
            RebirthSprite = ""
        };

        var pet = new ServerPet(def);

        Assert.Equal(101, pet.DefinitionId);
        Assert.Equal("Test Parrot", pet.Name);
        Assert.Equal(30, pet.MaxHp);
        Assert.Equal(30, pet.Hp);
        Assert.Equal(1, pet.Level);
        Assert.Equal(0, pet.Exp);
        Assert.Equal(50, pet.Amity);
        Assert.False(pet.IsDead);
    }

    [Fact]
    public void AddExp_ShouldLevelUp_WhenExpExceedsThreshold()
    {
        var def = new PetDefinition
        {
            PetTypeId = 1,
            BaseHp = 10,
            Name = "Test",
            SkillIds = new System.Collections.Generic.List<int>(),
            RebirthSprite = ""
        };
        var pet = new ServerPet(def);

        // Initial ExpToNextLevel is 100
        pet.AddExp(150);

        Assert.Equal(2, pet.Level);
        Assert.Equal(50, pet.Exp); // 150 - 100
        // Check dynamic stat growth logic. Default HpGrowthPerLevel is 10.
        // Base 10 + 10 * 1 = 20.
        Assert.Equal(20, pet.MaxHp);
        Assert.Equal(20, pet.Hp);
    }

    [Fact]
    public void ChangeAmity_ShouldClampValues()
    {
        var pet = new ServerPet();
        pet.Amity = 50;

        pet.ChangeAmity(60); // 110
        Assert.Equal(100, pet.Amity);

        pet.ChangeAmity(-150); // -50
        Assert.Equal(0, pet.Amity);
    }
}
