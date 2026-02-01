using TWL.Server.Features.Combat;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Skills;

namespace TWL.Tests.Features.Combat;

public class SkillTargetingTests
{
    private ServerCharacter CreateChar(int id, Team team)
    {
        return new ServerCharacter { Id = id, Team = team, Con = 10, Hp = 100 };
    }

    [Fact]
    public void GetTargets_SingleEnemy_ReturnsPrimary()
    {
        var p1 = CreateChar(1, Team.Player);
        var e1 = CreateChar(2, Team.Enemy);
        var e2 = CreateChar(3, Team.Enemy);
        var all = new[] { p1, e1, e2 };

        var skill = new Skill { TargetType = SkillTargetType.SingleEnemy };
        var targets = SkillTargetingHelper.GetTargets(skill, p1, e1, all);

        Assert.Single(targets);
        Assert.Equal(e1.Id, targets[0].Id);
    }

    [Fact]
    public void GetTargets_AllEnemies_ReturnsAllEnemies()
    {
        var p1 = CreateChar(1, Team.Player);
        var e1 = CreateChar(2, Team.Enemy);
        var e2 = CreateChar(3, Team.Enemy);
        var all = new[] { p1, e1, e2 };

        var skill = new Skill { TargetType = SkillTargetType.AllEnemies };
        var targets = SkillTargetingHelper.GetTargets(skill, p1, e1, all);

        Assert.Equal(2, targets.Count);
        Assert.Contains(targets, c => c.Id == e1.Id);
        Assert.Contains(targets, c => c.Id == e2.Id);
    }

    [Fact]
    public void GetTargets_Self_ReturnsSelf()
    {
        var p1 = CreateChar(1, Team.Player);
        var skill = new Skill { TargetType = SkillTargetType.Self };
        var targets = SkillTargetingHelper.GetTargets(skill, p1, p1, new[] { p1 });

        Assert.Single(targets);
        Assert.Equal(p1.Id, targets[0].Id);
    }
}
