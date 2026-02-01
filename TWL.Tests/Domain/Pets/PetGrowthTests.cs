using Xunit;
using TWL.Server.Simulation.Managers;
using TWL.Shared.Domain.Characters;

namespace TWL.Tests.Domain.Pets;

public class PetGrowthTests
{
    [Fact]
    public void TestStatsAtLevel1()
    {
        var def = new PetDefinition
        {
            BaseHp = 100,
            BaseStr = 10,
            BaseCon = 10,
            BaseInt = 10,
            BaseWis = 10,
            BaseAgi = 10,
            GrowthModel = new PetGrowthModel
            {
                HpGrowthPerLevel = 10,
                StrWeight = 1,
                ConWeight = 1,
                IntWeight = 1,
                WisWeight = 1,
                AgiWeight = 1
            }
        };

        PetGrowthCalculator.CalculateStats(def, 1,
            out int maxHp, out int maxSp,
            out int str, out int con, out int int_, out int wis, out int agi);

        Assert.Equal(100, maxHp);
        Assert.Equal(10, str);
        Assert.Equal(10, con);
        Assert.Equal(10, int_);
        Assert.Equal(10, wis);
        Assert.Equal(10, agi);
    }

    [Fact]
    public void TestStatsAtLevel2()
    {
        var def = new PetDefinition
        {
            BaseHp = 100,
            BaseStr = 10,
            BaseCon = 10,
            BaseInt = 10,
            BaseWis = 10,
            BaseAgi = 10,
            GrowthModel = new PetGrowthModel
            {
                HpGrowthPerLevel = 10,
                StrWeight = 1,
                ConWeight = 1,
                IntWeight = 1,
                WisWeight = 1,
                AgiWeight = 1
            }
        };

        // Level 6 = 15 points.
        // 15 * 1 / 5 = 3 per stat.

        PetGrowthCalculator.CalculateStats(def, 6,
            out int maxHp, out int maxSp,
            out int str, out int con, out int int_, out int wis, out int agi);

        Assert.Equal(150 + (13 - 10) * 5, maxHp);
        Assert.Equal(13, str);
        Assert.Equal(13, con);
        Assert.Equal(13, int_);
        Assert.Equal(13, wis);
        Assert.Equal(13, agi);
    }
}
