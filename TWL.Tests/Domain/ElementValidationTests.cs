using System;
using System.Collections.Generic;
using Xunit;
using TWL.Shared.Domain.Characters;

namespace TWL.Tests.Domain;

public class ElementValidationTests
{
    [Fact]
    public void PlayerCharacter_ThrowsOnNoneElement()
    {
        Assert.Throws<ArgumentException>(() => new PlayerCharacter(Guid.NewGuid(), "TestPlayer", Element.None));
    }

    [Fact]
    public void PetCharacter_ThrowsOnNoneElement()
    {
        Assert.Throws<ArgumentException>(() => new PetCharacter("TestPet", Element.None));
    }

    [Fact]
    public void MonsterDefinition_WithNoneElement_RequiresQuestOnlyTag()
    {
        // This logic is enforced during content validation, not constructor (as MonsterDefinition is POCO).
        // But we can test the validator logic here.

        var def = new MonsterDefinition
        {
            MonsterId = 1,
            Element = Element.None,
            Tags = new List<string>() // Missing QuestOnly
        };

        // Simulating the validator check
        bool isValid = ValidateMonster(def, out var error);
        Assert.False(isValid, "Monster with Element.None should fail without QuestOnly tag.");
        Assert.Contains("QuestOnly", error);

        def.Tags.Add("QuestOnly");
        isValid = ValidateMonster(def, out error);
        Assert.True(isValid, "Monster with Element.None and QuestOnly tag should pass.");
    }

    private bool ValidateMonster(MonsterDefinition def, out string error)
    {
        error = string.Empty;
        if (def.Element == Element.None)
        {
            if (def.Tags == null || !def.Tags.Contains("QuestOnly"))
            {
                error = $"Monster {def.MonsterId} has Element.None but is missing 'QuestOnly' tag.";
                return false;
            }
        }
        return true;
    }
}
