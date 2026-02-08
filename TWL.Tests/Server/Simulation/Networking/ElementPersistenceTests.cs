using System.IO;
using TWL.Server.Persistence;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;

namespace TWL.Tests.Server.Simulation.Networking;

public class ElementPersistenceTests
{
    [Fact]
    public void Character_LoadsElement_Correctly()
    {
        var data = new ServerCharacterData
        {
            Element = Element.Fire,
            Name = "Test"
        };

        var character = new ServerCharacter();
        character.LoadSaveData(data);

        Assert.Equal(Element.Fire, character.CharacterElement);
    }

    [Fact]
    public void Character_SavesElement_Correctly()
    {
        var character = new ServerCharacter();
        character.CharacterElement = Element.Wind;

        var data = character.GetSaveData();

        Assert.Equal(Element.Wind, data.Element);
    }

    [Fact]
    public void Character_Load_NoneElement_DefaultsToEarthForPlayer()
    {
        var data = new ServerCharacterData
        {
            Element = Element.None,
            Name = "LegacyPlayer"
        };

        var character = new ServerCharacter();
        // Default MonsterId is 0 (Player)

        character.LoadSaveData(data);

        Assert.Equal(Element.Earth, character.CharacterElement);
        Assert.True(character.IsDirty);
    }
}
