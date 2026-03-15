using TWL.Shared.Domain;
using TWL.Shared.Domain.Characters;

namespace TWL.Tests.PetTests;

public class PetRosterCoverageTests
{
    [Fact]
    public void StarterRegion_PetRoster_HasMinimumCount()
    {
        var pets = ContentTestHelper.LoadPets();
        
        // We require at least 20 unique pet definitions for the starter regions
        // IDs 1000-1999 are typically common capture pets
        var starterPets = pets.Where(p => p.PetTypeId >= 1000 && p.PetTypeId < 2000).ToList();
        
        Assert.True(starterPets.Count >= 20, 
            $"Starter region pet roster only has {starterPets.Count} pets. Expected 20+.");
    }

    [Fact]
    public void StarterRegion_CapturableMonsters_AreLinkedToPets()
    {
        var monsters = ContentTestHelper.LoadMonsters();
        var pets = ContentTestHelper.LoadPets();
        var petIds = pets.Select(p => p.PetTypeId).ToHashSet();

        // Focus on common monsters (2000-2999 range often used for standard mobs)
        var starterMonsters = monsters.Where(m => m.MonsterId >= 2000 && m.MonsterId < 3000).ToList();
        var capturableStarterMonsters = starterMonsters.Where(m => m.IsCapturable).ToList();

        Assert.NotEmpty(capturableStarterMonsters);

        foreach (var monster in capturableStarterMonsters)
        {
            Assert.True(monster.PetTypeId.HasValue, 
                $"Capturable monster {monster.MonsterId} ({monster.Name}) has no PetTypeId.");
            Assert.True(petIds.Contains(monster.PetTypeId.Value), 
                $"Capturable monster {monster.MonsterId} ({monster.Name}) links to non-existent PetTypeId {monster.PetTypeId.Value}.");
        }
    }
}
