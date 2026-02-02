using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Skills;
using TWL.Tests.Mocks;

namespace TWL.Tests.Simulation;

public class AutoBattleManagerTests
{
    private MockSkillCatalog _catalog;
    private MockRandomService _random;
    private AutoBattleManager _manager;

    public AutoBattleManagerTests()
    {
        _catalog = new MockSkillCatalog();
        _random = new MockRandomService(0.5f);
        _manager = new AutoBattleManager(_catalog);
    }

    private ServerCharacter CreateActor(int id, Team team)
    {
        return new ServerCharacter { Id = id, Team = team, Hp = 100, Con = 10, Sp = 100, Int = 10 };
    }

    [Fact]
    public void GetBestAction_Survival_HealsAllyLowHp()
    {
        var actor = CreateActor(1, Team.Player);
        var ally = CreateActor(2, Team.Player);
        ally.Hp = 20; // 20% < 30%
        var enemy = CreateActor(3, Team.Enemy);

        var healSkill = new Skill
        {
            SkillId = 100,
            Name = "Heal",
            SpCost = 10,
            Effects = new List<SkillEffect> { new SkillEffect { Tag = SkillEffectTag.Heal } },
            TargetType = SkillTargetType.SingleAlly,
            Scaling = new List<SkillScaling> { new SkillScaling { Stat = StatType.Wis, Coefficient = 2.0f } }
        };
        _catalog.AddSkill(healSkill);
        actor.SkillMastery.TryAdd(100, new SkillMastery());

        var result = _manager.GetBestAction(actor, new[] { actor, ally, enemy });

        Assert.NotNull(result);
        Assert.Equal(100, result.SkillId);
        Assert.Equal(ally.Id, result.TargetId);
    }

    [Fact]
    public void GetBestAction_Aggression_UsesStrongestAttack()
    {
        var actor = CreateActor(1, Team.Player);
        var enemy = CreateActor(2, Team.Enemy);

        var weakAttack = new Skill { SkillId = 101, Name = "Weak", SpCost = 5, Branch = SkillBranch.Physical, TargetType = SkillTargetType.SingleEnemy };
        var strongAttack = new Skill { SkillId = 102, Name = "Strong", SpCost = 20, Branch = SkillBranch.Physical, TargetType = SkillTargetType.SingleEnemy };

        _catalog.AddSkill(weakAttack);
        _catalog.AddSkill(strongAttack);

        actor.SkillMastery.TryAdd(101, new SkillMastery());
        actor.SkillMastery.TryAdd(102, new SkillMastery());

        var result = _manager.GetBestAction(actor, new[] { actor, enemy });

        Assert.NotNull(result);
        Assert.Equal(102, result.SkillId);
        Assert.Equal(enemy.Id, result.TargetId);
    }
}
