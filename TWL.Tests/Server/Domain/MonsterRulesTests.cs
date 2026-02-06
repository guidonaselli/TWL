using System.Text.Json;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;

namespace TWL.Tests.Server.Domain;

public class MonsterRulesTests
{
    [Fact]
    public void MonsterManager_Load_Throws_If_ElementNone_Without_QuestOnly_Tag()
    {
        // Arrange
        var badMonster = new MonsterDefinition
        {
            MonsterId = 9999,
            Name = "Bad Monster",
            Element = Element.None,
            Tags = new List<string>() // Missing "QuestOnly"
        };

        var json = JsonSerializer.Serialize(new List<MonsterDefinition> { badMonster });
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, json);

        try
        {
            var manager = new MonsterManager();

            // Act & Assert
            var ex = Assert.Throws<InvalidDataException>(() => manager.Load(tempFile));
            Assert.Contains("missing 'QuestOnly' tag", ex.Message);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ServerPet_Hydrate_Throws_If_ElementNone()
    {
        // Arrange
        var badPetDef = new PetDefinition
        {
            PetTypeId = 8888,
            Name = "Bad Pet",
            Element = Element.None
        };

        var pet = new ServerPet();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => pet.Hydrate(badPetDef));
        Assert.Contains("forbidden", ex.Message);
    }

    [Fact]
    public void ServerCharacter_CanHaveElementNone_ForMobs()
    {
        // This confirms that the class itself supports it (needed for QuestOnly mobs)
        var mob = new ServerCharacter
        {
            CharacterElement = Element.None
        };

        Assert.Equal(Element.None, mob.CharacterElement);
    }
}
