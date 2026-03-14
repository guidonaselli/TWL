using Xunit;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Server.Simulation.Managers;


namespace TWL.Tests.PetTests;

/// <summary>
/// Validates quest-vs-capturable differentiation and 10/8/5 diminishing bonus schedule for pet rebirth.
/// Covers requirements PET-03 and PET-04.
/// </summary>
public class PetRebirthPolicyTests
{
    // ─── Helpers ────────────────────────────────────────────────────────────────

    private static PetDefinition MakeQuestPetDef(int petTypeId = 1001) => new()
    {
        PetTypeId = petTypeId,
        Name = "Quest Pet",
        Type = PetType.Quest,
        RebirthEligible = true,
        Element = Element.Earth,
        BaseHp = 200,
        BaseStr = 10,
        BaseCon = 10,
        BaseInt = 10,
        BaseWis = 10,
        BaseAgi = 10,
        GrowthModel = new PetGrowthModel { HpGrowthPerLevel = 5, StrWeight = 20, ConWeight = 20, IntWeight = 20, WisWeight = 20, AgiWeight = 20 }

    };

    private static PetDefinition MakeCapturePetDef(int petTypeId = 2001) => new()
    {
        PetTypeId = petTypeId,
        Name = "Capturable Pet",
        Type = PetType.Capture,
        RebirthEligible = false,  // Capturable pets cannot rebirth
        Element = Element.Water,
        BaseHp = 150,
        BaseStr = 8,
        BaseCon = 8,
        BaseInt = 8,
        BaseWis = 8,
        BaseAgi = 8,
        CaptureRules = new CaptureRules { IsCapturable = true, BaseChance = 0.5f },
        GrowthModel = new PetGrowthModel { HpGrowthPerLevel = 3, StrWeight = 20, ConWeight = 20, IntWeight = 20, WisWeight = 20, AgiWeight = 20 }

    };

    private static ServerPet MakeQuestPet(int level = 100)
    {
        var pet = new ServerPet(MakeQuestPetDef());
        pet.SetLevel(level);
        return pet;
    }

    private static ServerPet MakeCapturePet(int level = 100)
    {
        var pet = new ServerPet(MakeCapturePetDef());
        pet.SetLevel(level);
        return pet;
    }

    // ─── Eligibility Policy ─────────────────────────────────────────────────────

    [Fact]
    public void TryRebirth_QuestPet_AtLevel100_Succeeds()
    {
        var pet = MakeQuestPet(100);

        var result = pet.TryRebirth();

        Assert.True(result);
        Assert.Equal(1, pet.RebirthGeneration);
        Assert.True(pet.HasRebirthed);
        Assert.Equal(1, pet.Level); // Reset to level 1
    }

    [Fact]
    public void TryRebirth_CapturePet_IsRejected()
    {
        var pet = MakeCapturePet(100);

        var result = pet.TryRebirth();

        Assert.False(result, "Capturable pets must be blocked from rebirth.");
        Assert.Equal(0, pet.RebirthGeneration);
        Assert.False(pet.HasRebirthed);
        Assert.Equal(100, pet.Level); // Unchanged
    }

    [Fact]
    public void TryRebirth_QuestPet_BelowLevel100_IsRejected()
    {
        var pet = MakeQuestPet(99);

        var result = pet.TryRebirth();

        Assert.False(result);
        Assert.Equal(0, pet.RebirthGeneration);
        Assert.Equal(99, pet.Level);
    }

    [Fact]
    public void TryRebirth_NotRebirthEligible_IsRejected()
    {
        var def = MakeQuestPetDef();
        def.RebirthEligible = false; // Override — even quest pets must be individually marked
        var pet = new ServerPet(def);
        pet.SetLevel(100);

        var result = pet.TryRebirth();

        Assert.False(result);
        Assert.Equal(0, pet.RebirthGeneration);
    }

    // ─── Multi-Generation Support ────────────────────────────────────────────────

    [Fact]
    public void TryRebirth_QuestPet_CanRebirthMultipleTimes()
    {
        var pet = MakeQuestPet(100);

        // First rebirth
        Assert.True(pet.TryRebirth());
        Assert.Equal(1, pet.RebirthGeneration);

        // Level up to 100 again for second rebirth
        pet.SetLevel(100);
        Assert.True(pet.TryRebirth());
        Assert.Equal(2, pet.RebirthGeneration);

        // Third rebirth
        pet.SetLevel(100);
        Assert.True(pet.TryRebirth());
        Assert.Equal(3, pet.RebirthGeneration);
    }

    [Fact]
    public void TryRebirth_SecondRebirth_RequiresLevel100Again()
    {
        var pet = MakeQuestPet(100);
        pet.TryRebirth(); // Gen 1 — now at level 1

        // Not at level 100 yet after reset
        var result = pet.TryRebirth();

        Assert.False(result, "Pet must reach level 100 again before second rebirth.");
        Assert.Equal(1, pet.RebirthGeneration);
    }

    // ─── Diminishing Bonus Schedule ─────────────────────────────────────────────

    [Theory]
    [InlineData(1, 10)]
    [InlineData(2, 8)]
    [InlineData(3, 5)]
    [InlineData(4, 5)]
    [InlineData(10, 5)]
    public void GetRebirthBonusPoints_MatchesSchedule(int generation, int expectedBonus)
    {
        var bonus = ServerPet.GetRebirthBonusPoints(generation);

        Assert.Equal(expectedBonus, bonus);
    }

    [Theory]
    [InlineData(1, 1.10f)]    // +10%
    [InlineData(2, 1.18f)]    // +10% + 8%
    [InlineData(3, 1.23f)]    // +10% + 8% + 5%
    [InlineData(4, 1.28f)]    // +10% + 8% + 5% + 5%
    public void GetCumulativeStatMultiplier_IsCorrect(int generations, float expectedMultiplier)
    {
        var multiplier = ServerPet.GetCumulativeStatMultiplier(generations);

        Assert.Equal(expectedMultiplier, multiplier, precision: 2);
    }

    [Fact]
    public void RecalculateStats_AfterFirstRebirth_AppliesTenPercentBonus()
    {
        var pet = MakeQuestPet(100);
        var baseStr = pet.Str;
        Assert.True(baseStr > 0, "Baseline Str should be positive.");

        pet.TryRebirth();
        // Gen 1 → level 1 stats × 1.10 multiplier
        var expectedStr = (int)(MakeQuestPet(1).Str * 1.10f);

        Assert.Equal(expectedStr, pet.Str);
    }

    [Fact]
    public void RecalculateStats_AfterSecondRebirth_AppliesCumulativeBonus()
    {
        var pet = MakeQuestPet(100);
        pet.TryRebirth();          // Gen 1 → 1.10x
        pet.SetLevel(100);
        pet.TryRebirth();          // Gen 2 → 1.18x

        var baselineStr = MakeQuestPet(1).Str;
        var expectedStr = (int)(baselineStr * 1.18f);

        Assert.Equal(expectedStr, pet.Str);
    }
}
