using TWL.Server.Simulation.Networking;

namespace TWL.Tests.Skills;

public class SkillProgressionTests
{
    [Fact]
    public void IncrementSkillUsage_UpdatesRank_Every10Uses()
    {
        var character = new ServerCharacter();
        var skillId = 1001;
        character.LearnSkill(skillId);

        // Use 9 times - Rank should be 1
        for (var i = 0; i < 9; i++)
        {
            character.IncrementSkillUsage(skillId);
        }

        Assert.Equal(1, character.SkillMastery[skillId].Rank);

        // Use 10th time - Rank should be 2
        character.IncrementSkillUsage(skillId);
        Assert.Equal(2, character.SkillMastery[skillId].Rank);
    }

    [Fact]
    public void ReplaceSkill_ResetsRank()
    {
        var character = new ServerCharacter();
        var oldId = 1001;
        var newId = 1002;
        character.LearnSkill(oldId);
        character.SkillMastery[oldId].Rank = 10;

        character.ReplaceSkill(oldId, newId);

        Assert.False(character.SkillMastery.ContainsKey(oldId));
        Assert.True(character.SkillMastery.ContainsKey(newId));
        Assert.Equal(1, character.SkillMastery[newId].Rank);
        Assert.Equal(0, character.SkillMastery[newId].UsageCount);
    }

    [Fact]
    public void Persistence_SavesAndLoads_SkillMastery()
    {
        var original = new ServerCharacter { Id = 1, Name = "Test" };
        original.LearnSkill(1001);
        original.SkillMastery[1001].Rank = 5;
        original.SkillMastery[1001].UsageCount = 45;

        var saveData = original.GetSaveData();

        var loaded = new ServerCharacter();
        loaded.LoadSaveData(saveData);

        Assert.True(loaded.SkillMastery.ContainsKey(1001));
        Assert.Equal(5, loaded.SkillMastery[1001].Rank);
        Assert.Equal(45, loaded.SkillMastery[1001].UsageCount);
    }
}