using Xunit;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Server.Simulation.Managers;


namespace TWL.Tests.PetTests;

/// <summary>
/// Validates pet evolution (skill unlock and level reset) on rebirth and multi-generation progression.
/// Covers requirements PET-04 (evolution/action routing).
/// </summary>
public class PetRebirthEvolutionTests
{
    private const int RebirthSkillId = 5001;

    private static PetDefinition MakeQuestPetDefWithRebirthSkill() => new()
    {
        PetTypeId = 1002,
        Name = "Evolving Quest Pet",
        Type = PetType.Quest,
        RebirthEligible = true,
        RebirthSkillId = RebirthSkillId,
        Element = Element.Fire,
        BaseHp = 200,
        BaseStr = 10, BaseCon = 10, BaseInt = 10, BaseWis = 10, BaseAgi = 10,
        GrowthModel = new PetGrowthModel { HpGrowthPerLevel = 5, StrWeight = 20, ConWeight = 20, IntWeight = 20, WisWeight = 20, AgiWeight = 20 }

    };

    private static PetDefinition MakeQuestPetDefNoSkill() => new()
    {
        PetTypeId = 1003,
        Name = "Simple Quest Pet",
        Type = PetType.Quest,
        RebirthEligible = true,
        RebirthSkillId = 0, // No rebirth skill
        Element = Element.Wind,
        BaseHp = 150,
        BaseStr = 8, BaseCon = 8, BaseInt = 8, BaseWis = 8, BaseAgi = 8,
        GrowthModel = new PetGrowthModel { HpGrowthPerLevel = 3, StrWeight = 20, ConWeight = 20, IntWeight = 20, WisWeight = 20, AgiWeight = 20 }

    };

    // ─── Skill Unlock on Evolution ───────────────────────────────────────────────

    [Fact]
    public void TryRebirth_WithRebirthSkillId_UnlocksSkillOnFirstRebirth()
    {
        var pet = new ServerPet(MakeQuestPetDefWithRebirthSkill());
        pet.SetLevel(100);

        var result = pet.TryRebirth();

        Assert.True(result);
        Assert.Contains(RebirthSkillId, pet.UnlockedSkillIds);
    }

    [Fact]
    public void TryRebirth_SecondRebirth_DoesNotDuplicateRebirthSkill()
    {
        var pet = new ServerPet(MakeQuestPetDefWithRebirthSkill());
        pet.SetLevel(100);
        pet.TryRebirth();         // Gen 1 — skill unlocked
        int skillCountAfterGen1 = pet.UnlockedSkillIds.Count(id => id == RebirthSkillId);

        pet.SetLevel(100);
        pet.TryRebirth();         // Gen 2 — skill should not be duplicated

        int skillCountAfterGen2 = pet.UnlockedSkillIds.Count(id => id == RebirthSkillId);
        Assert.Equal(skillCountAfterGen1, skillCountAfterGen2);
    }

    [Fact]
    public void TryRebirth_WithNoRebirthSkill_DoesNotAddAnyRebirthSkill()
    {
        var pet = new ServerPet(MakeQuestPetDefNoSkill());
        pet.SetLevel(100);
        var skillsBefore = pet.UnlockedSkillIds.ToList();

        pet.TryRebirth();

        // Skill list unchanged (no rebirth skill to unlock)
        Assert.Equal(skillsBefore.Count, pet.UnlockedSkillIds.Count);
    }

    // ─── Level Reset on Rebirth ──────────────────────────────────────────────────

    [Fact]
    public void TryRebirth_ResetsLevelToOne()
    {
        var pet = new ServerPet(MakeQuestPetDefNoSkill());
        pet.SetLevel(100);

        pet.TryRebirth();

        Assert.Equal(1, pet.Level);
        Assert.Equal(0, pet.Exp);
    }

    [Fact]
    public void TryRebirth_ResetsExpToNextLevel()
    {
        var pet = new ServerPet(MakeQuestPetDefNoSkill());
        pet.SetLevel(100);
        int expectedExpForLevel1 = PetGrowthCalculator.GetExpForLevel(1);

        pet.TryRebirth();

        Assert.Equal(expectedExpForLevel1, pet.ExpToNextLevel);
    }

    // ─── HP/MP Reset on Rebirth ──────────────────────────────────────────────────

    [Fact]
    public void TryRebirth_RestoresFullHpAfterReset()
    {
        var pet = new ServerPet(MakeQuestPetDefNoSkill());
        pet.SetLevel(100);
        pet.Hp = 1; // Simulate low HP

        pet.TryRebirth();

        Assert.Equal(pet.MaxHp, pet.Hp);
    }

    // ─── Generation Tracking & IsDirty ──────────────────────────────────────────

    [Fact]
    public void TryRebirth_SetsIsDirtyForPersistence()
    {
        var pet = new ServerPet(MakeQuestPetDefNoSkill());
        pet.SetLevel(100);

        pet.TryRebirth();

        Assert.True(pet.IsDirty);
    }

    [Fact]
    public void TryRebirth_IncreasesGenerationWithEachRebirth()
    {
        var pet = new ServerPet(MakeQuestPetDefNoSkill());

        for (int gen = 1; gen <= 5; gen++)
        {
            pet.SetLevel(100);
            pet.TryRebirth();
            Assert.Equal(gen, pet.RebirthGeneration);
        }
    }

    // ─── Save/Load Roundtrip ─────────────────────────────────────────────────────

    [Fact]
    public void GetSaveData_PersistsRebirthGeneration()
    {
        var pet = new ServerPet(MakeQuestPetDefNoSkill());
        pet.SetLevel(100);
        pet.TryRebirth();          // Gen 1

        var saveData = pet.GetSaveData();

        Assert.Equal(1, saveData.RebirthGeneration);
    }

    [Fact]
    public void LoadSaveData_RestoresRebirthGeneration()
    {
        var original = new ServerPet(MakeQuestPetDefNoSkill());
        original.SetLevel(100);
        original.TryRebirth(); // Gen 1
        original.SetLevel(100);
        original.TryRebirth(); // Gen 2
        var saveData = original.GetSaveData();

        var restored = new ServerPet(MakeQuestPetDefNoSkill());
        restored.LoadSaveData(saveData);

        Assert.Equal(2, restored.RebirthGeneration);
        Assert.True(restored.HasRebirthed);
    }

    [Fact]
    public void LoadSaveData_MigratesLegacyHasRebirthBoolToGenerationOne()
    {
        // Simulate an old save with HasRebirthed=true but RebirthGeneration=0
        var legacySaveData = new TWL.Server.Persistence.ServerPetData
        {
            InstanceId = System.Guid.NewGuid().ToString(),
            DefinitionId = 1003,
            Name = "Old Save Pet",
            Level = 1,
            Exp = 0,
            Amity = 50,
            HasRebirthed = true,
            RebirthGeneration = 0  // Old save format
        };

        var pet = new ServerPet(MakeQuestPetDefNoSkill());
        pet.LoadSaveData(legacySaveData);

        Assert.Equal(1, pet.RebirthGeneration);
        Assert.True(pet.HasRebirthed);
    }
}
