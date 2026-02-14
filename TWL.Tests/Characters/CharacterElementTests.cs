using System.Collections.Generic;
using TWL.Server.Persistence;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Models;
using Xunit;

namespace TWL.Tests.Characters;

public class CharacterElementTests
{
    [Fact]
    public void ServerCharacter_DefaultsToEarth_NotNone()
    {
        var character = new ServerCharacter();
        Assert.NotEqual(Element.None, character.CharacterElement);
        Assert.Equal(Element.Earth, character.CharacterElement);
    }

    [Fact]
    public void ServerCharacter_LoadSaveData_FixesNoneElement_ForPlayer()
    {
        var character = new ServerCharacter();
        var data = new ServerCharacterData
        {
            Id = 1,
            Name = "TestPlayer",
            Element = Element.None, // Legacy/Invalid
            // Other fields default
        };

        character.LoadSaveData(data);

        Assert.NotEqual(Element.None, character.CharacterElement);
        Assert.Equal(Element.Earth, character.CharacterElement);
        Assert.True(character.IsDirty);
    }

    [Fact]
    public void ServerCharacter_LoadSaveData_KeepsValidElement()
    {
        var character = new ServerCharacter();
        var data = new ServerCharacterData
        {
            Id = 1,
            Name = "TestPlayer",
            Element = Element.Fire
        };

        character.LoadSaveData(data);

        Assert.Equal(Element.Fire, character.CharacterElement);
    }

    [Fact]
    public void ServerPet_Hydrate_ThrowsOnNoneElement()
    {
        var pet = new ServerPet();
        var def = new PetDefinition { PetTypeId = 1, Name = "BadPet", Element = Element.None };

        Assert.Throws<System.InvalidOperationException>(() => pet.Hydrate(def));
    }
}
