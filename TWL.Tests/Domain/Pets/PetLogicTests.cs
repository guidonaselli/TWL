using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;

namespace TWL.Tests.Domain.Pets;

public class PetLogicTests
{
    [Fact]
    public void TestSkillUnlocks()
    {
        var def = new PetDefinition
        {
            PetTypeId = 1,
            Name = "Test",
            Element = Element.Earth,
            SkillSet = new List<PetSkillSet>
            {
                new() { SkillId = 101, UnlockLevel = 5, UnlockAmity = 0 },
                new() { SkillId = 102, UnlockLevel = 10, UnlockAmity = 60 },
                new() { SkillId = 103, UnlockLevel = 1, UnlockAmity = 0, RequiresRebirth = true }
            }
        };
        var pet = new ServerPet(def); // Level 1, Amity 50

        // Initial state
        Assert.DoesNotContain(101, pet.UnlockedSkillIds);

        // Level up to 5
        pet.Level = 5; // Hacky set for test
        pet.CheckSkillUnlocks();
        Assert.Contains(101, pet.UnlockedSkillIds);
        Assert.DoesNotContain(102, pet.UnlockedSkillIds);

        // Amity up
        pet.ChangeAmity(10); // 60
        // Level 10
        pet.Level = 10;
        pet.CheckSkillUnlocks();
        Assert.Contains(102, pet.UnlockedSkillIds);

        // Rebirth skill
        Assert.DoesNotContain(103, pet.UnlockedSkillIds);
    }

    [Fact]
    public void TestRebirth()
    {
        var def = new PetDefinition
        {
            PetTypeId = 1,
            Name = "Test",
            Element = Element.Earth,
            RebirthEligible = true,
            RebirthSkillId = 999
        };
        var pet = new ServerPet(def);
        pet.Level = 100;

        var result = pet.TryRebirth();
        Assert.True(result);
        Assert.Equal(1, pet.Level);
        Assert.True(pet.HasRebirthed);
        Assert.Contains(999, pet.UnlockedSkillIds);

        // Try again
        pet.Level = 100;
        result = pet.TryRebirth();
        Assert.False(result); // Already rebirthed
    }
}