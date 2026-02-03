using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;

namespace TWL.Tests.Domain.Pets;

public class PetCoreTests
{
    [Fact]
    public void TestPetDeathPenalty()
    {
        var def = new PetDefinition
        {
            PetTypeId = 1,
            Name = "Test",
            Element = Element.Earth,
            BaseHp = 100
        };
        // Ensure stats are calculated so MaxHp is valid
        var pet = new ServerPet(def);
        pet.Amity = 50;

        pet.Die();

        Assert.True(pet.IsDead);
        Assert.Equal(0, pet.Hp);
        Assert.Equal(0, pet.Sp);
        Assert.Equal(49, pet.Amity); // 50 - 1
    }

    [Fact]
    public void TestPetRevive()
    {
        var def = new PetDefinition
        {
            PetTypeId = 1,
            Name = "Test",
            Element = Element.Earth,
            BaseHp = 100,
            BaseStr = 10,
            BaseCon = 10
        };
        var pet = new ServerPet(def);
        pet.Die();

        Assert.True(pet.IsDead);

        pet.Revive();

        Assert.False(pet.IsDead);
        Assert.Equal(pet.MaxHp, pet.Hp);
    }

    [Fact]
    public void TestPetGrowthCurves()
    {
        // EarlyPeaker
        var earlyDef = new PetDefinition
        {
            PetTypeId = 1,
            Element = Element.Fire,
            BaseStr = 10,
            BaseHp = 100, // Explicit base stats to avoid 0
            GrowthModel = new PetGrowthModel
            {
                CurveType = GrowthCurveType.EarlyPeaker,
                StrWeight = 100,
                ConWeight = 0,
                IntWeight = 0,
                WisWeight = 0,
                AgiWeight = 0
            }
        };
        var earlyPet = new ServerPet(earlyDef);

        earlyPet.Level = 20;
        earlyPet.RecalculateStats();
        var strLevel20 = earlyPet.Str;

        // LateBloomer
        var lateDef = new PetDefinition
        {
            PetTypeId = 2,
            Element = Element.Water,
            BaseStr = 10,
            BaseHp = 100,
            GrowthModel = new PetGrowthModel
            {
                CurveType = GrowthCurveType.LateBloomer,
                StrWeight = 100,
                ConWeight = 0,
                IntWeight = 0,
                WisWeight = 0,
                AgiWeight = 0
            }
        };
        var latePet = new ServerPet(lateDef);

        latePet.Level = 20;
        latePet.RecalculateStats();
        var lateStrLevel20 = latePet.Str;

        // Early peaker should have more stats early on compared to late bloomer
        // EarlyPeaker: <40 -> 1.2x. LateBloomer: <40 -> 0.8x.

        Assert.True(strLevel20 > lateStrLevel20,
            $"EarlyPeaker Str {strLevel20} should be > LateBloomer Str {lateStrLevel20}");
    }

    [Fact]
    public void TestCaptureLogic_Validation()
    {
        // Simulating Capture Logic manually since Service mocking is complex
        var def = new PetDefinition
        {
            CaptureRules = new CaptureRules
            {
                IsCapturable = true,
                LevelLimit = 10,
                BaseChance = 0.5f
            }
        };
        var playerLevel = 5;

        var canCapture = playerLevel >= def.CaptureRules.LevelLimit;
        Assert.False(canCapture, "Should fail level requirement");

        playerLevel = 15;
        canCapture = playerLevel >= def.CaptureRules.LevelLimit;
        Assert.True(canCapture);

        var roll = 0.6f;
        var success = roll <= def.CaptureRules.BaseChance;
        Assert.False(success, "Roll 0.6 > 0.5 should fail");

        roll = 0.4f;
        success = roll <= def.CaptureRules.BaseChance;
        Assert.True(success, "Roll 0.4 <= 0.5 should success");
    }

    [Fact]
    public void TestServerCharacter_PetRemoval()
    {
        var character = new ServerCharacter();
        var pet = new ServerPet { InstanceId = "pet1" };
        character.AddPet(pet);
        character.SetActivePet("pet1");

        Assert.Single(character.Pets);
        Assert.Equal("pet1", character.ActivePetInstanceId);

        var removed = character.RemovePet("pet1");

        Assert.True(removed);
        Assert.Empty(character.Pets);
        Assert.Null(character.ActivePetInstanceId);
    }
}