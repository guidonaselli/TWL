using TWL.Server.Simulation.Networking;

namespace TWL.Tests.Server;

public class ServerCharacterLevelingTests
{
    [Fact]
    public void InitialStats_ShouldBeDefault()
    {
        var chara = new ServerCharacter { Name = "Test" };
        Assert.Equal(1, chara.Level);
        Assert.Equal(0, chara.Exp);
        Assert.Equal(0, chara.StatPoints);
        Assert.Equal(8, chara.Str);
        Assert.Equal(8, chara.Con);
        Assert.Equal(8, chara.Int);
        Assert.Equal(8, chara.Wis);
        Assert.Equal(8, chara.Agi);
    }

    [Fact]
    public void AddExp_ShouldLevelUp_AndAwardStatPoints()
    {
        var chara = new ServerCharacter { Name = "Test" };
        // ExpToNextLevel is 100 initially.

        chara.AddExp(100);

        Assert.Equal(2, chara.Level);
        Assert.Equal(0, chara.Exp); // Consumed exact amount
        Assert.Equal(3, chara.StatPoints);
        Assert.NotEqual(100, chara.ExpToNextLevel); // Should have increased

        // Stats should NOT change
        Assert.Equal(8, chara.Str);
        Assert.Equal(8, chara.Con);
    }

    [Fact]
    public void AddExp_ShouldHandleMultipleLevelUps()
    {
        var chara = new ServerCharacter { Name = "Test" };
        // Lv1->2 requires 100. New ExpToNextLevel = 120 (100*1.2).
        // Lv2->3 requires 120. Total 220.

        chara.AddExp(220);

        Assert.Equal(3, chara.Level);
        Assert.Equal(6, chara.StatPoints);
        Assert.Equal(0, chara.Exp);
    }

    [Fact]
    public void AddExp_ShouldNotLevelUp_WhenInsufficient()
    {
        var chara = new ServerCharacter { Name = "Test" };
        chara.AddExp(50);

        Assert.Equal(1, chara.Level);
        Assert.Equal(50, chara.Exp);
        Assert.Equal(0, chara.StatPoints);
    }
}