using System.Collections.Generic;
using System.Linq;
using Moq;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Services;
using Xunit;

namespace TWL.Tests.Skills;

public class DailySkillsTests
{
    private readonly Mock<ISkillCatalog> _skillCatalogMock;
    private readonly AutoBattleService _autoBattle;

    public DailySkillsTests()
    {
        _skillCatalogMock = new Mock<ISkillCatalog>();
        _autoBattle = new AutoBattleService(_skillCatalogMock.Object);
    }

    [Fact]
    public void WindSkills_AreLoaded_InRegistry()
    {
        var json = @"[
          {
            ""SkillId"": 5001,
            ""Name"": ""Wind Slash I"",
            ""Stage"": 1,
            ""StageUpgradeRules"": { ""RankThreshold"": 6, ""NextSkillId"": 5002 }
          },
          {
            ""SkillId"": 5002,
            ""Name"": ""Wind Slash II"",
            ""Stage"": 2
          }
        ]";

        SkillRegistry.Instance.LoadSkills(json);
        var skill = SkillRegistry.Instance.GetSkillById(5001);

        Assert.NotNull(skill);
        Assert.Equal("Wind Slash I", skill.Name);
        Assert.Equal(1, skill.Stage);
        Assert.NotNull(skill.StageUpgradeRules);
        Assert.Equal(6, skill.StageUpgradeRules.RankThreshold);
        Assert.Equal(5002, skill.StageUpgradeRules.NextSkillId);
    }

    [Fact]
    public void AutoBattle_Selects_Seal_WhenEnemyDangerous()
    {
        // Arrange
        var actor = new ServerCharacter { Id = 1, Sp = 100 };
        actor.LearnSkill(100);
        // Enemy: Con=10->Hp100, Str=25->Atk50, Int=25->Mat50
        var enemy = new ServerCharacter { Id = 2, Hp = 100, Con = 10, Str = 25, Int = 25 };
        var ally = new ServerCharacter { Id = 3, Hp = 100, Con = 10 };

        var sealSkill = new Skill
        {
            SkillId = 100,
            SpCost = 10,
            TargetType = SkillTargetType.SingleEnemy,
            Effects = new List<SkillEffect> { new SkillEffect { Tag = SkillEffectTag.Seal } }
        };

        _skillCatalogMock.Setup(x => x.GetSkillById(100)).Returns(sealSkill);

        // Act
        var action = _autoBattle.SelectAction(actor, new List<ServerCharacter> { ally }, new List<ServerCharacter> { enemy }, 12345, AutoBattlePolicy.Supportive);

        // Assert
        Assert.Equal(CombatActionType.Skill, action.Type);
        Assert.Equal(100, action.SkillId);
        Assert.Equal(2, action.TargetId);
    }

    [Fact]
    public void AutoBattle_Selects_Buff_WhenAllyUnbuffed()
    {
        // Arrange
        var actor = new ServerCharacter { Id = 1, Sp = 100 };
        actor.LearnSkill(200);
        var enemy = new ServerCharacter { Id = 2, Hp = 10 };
        var ally = new ServerCharacter { Id = 3, Hp = 100, Con = 10 }; // Needs buff

        var buffSkill = new Skill
        {
            SkillId = 200,
            SpCost = 10,
            TargetType = SkillTargetType.SingleAlly,
            Effects = new List<SkillEffect> {
                new SkillEffect { Tag = SkillEffectTag.BuffStats, Param = "Agi" }
            }
        };

        _skillCatalogMock.Setup(x => x.GetSkillById(200)).Returns(buffSkill);

        // Act
        var action = _autoBattle.SelectAction(actor, new List<ServerCharacter> { ally }, new List<ServerCharacter> { enemy }, 12345, AutoBattlePolicy.Supportive);

        // Assert
        Assert.Equal(CombatActionType.Skill, action.Type);
        Assert.Equal(200, action.SkillId);
        Assert.Equal(3, action.TargetId);
    }

    [Fact]
    public void AutoBattle_Skips_Buff_IfAllyHasIt()
    {
        // Arrange
        var actor = new ServerCharacter { Id = 1, Sp = 100 };
        actor.LearnSkill(200);
        var ally = new ServerCharacter { Id = 3, Hp = 100 };
        // Ally already has Agi Buff
        ally.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.BuffStats, 10, 3, "Agi"), new StatusEngine());

        var buffSkill = new Skill
        {
            SkillId = 200,
            SpCost = 10,
            TargetType = SkillTargetType.SingleAlly,
            Effects = new List<SkillEffect> {
                new SkillEffect { Tag = SkillEffectTag.BuffStats, Param = "Agi" }
            }
        };

        _skillCatalogMock.Setup(x => x.GetSkillById(200)).Returns(buffSkill);

        // Act
        var action = _autoBattle.SelectAction(actor, new List<ServerCharacter> { ally }, new List<ServerCharacter> { }, 12345, AutoBattlePolicy.Supportive);

        // Assert
        Assert.NotEqual(CombatActionType.Skill, action.Type);
    }
}
