using Moq;
using System.Collections.Generic;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Skills;
using Xunit;

namespace TWL.Tests.Simulation;

public class AutoBattleTests
{
    private readonly Mock<ISkillCatalog> _skillCatalogMock;
    private readonly AutoBattleService _service;

    public AutoBattleTests()
    {
        _skillCatalogMock = new Mock<ISkillCatalog>();
        _service = new AutoBattleService(_skillCatalogMock.Object);
    }

    [Fact]
    public void SelectAction_Dispel_Prioritized_WhenEnemyBuffed()
    {
        // Arrange
        var actor = new ServerCharacter { Id = 1, Name = "Actor", Sp = 100 };
        var ally = new ServerCharacter { Id = 2, Name = "Ally" };
        ally.Con = 10; ally.Hp = 100; // Full HP

        var enemy = new ServerCharacter { Id = 3, Name = "Enemy" };
        enemy.Con = 10; enemy.Hp = 50;
        // Enemy has buff
        enemy.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.Shield, 10, 3, null), new StatusEngine());

        // Skills:
        // 10: Damage
        // 11: Dispel
        actor.KnownSkills.Add(10);
        actor.KnownSkills.Add(11);

        _skillCatalogMock.Setup(x => x.GetSkillById(10)).Returns(new Skill { SkillId = 10, SpCost = 5, Effects = new List<SkillEffect> { new SkillEffect { Tag = SkillEffectTag.Damage } }, TargetType = SkillTargetType.SingleEnemy });
        _skillCatalogMock.Setup(x => x.GetSkillById(11)).Returns(new Skill { SkillId = 11, SpCost = 5, Effects = new List<SkillEffect> { new SkillEffect { Tag = SkillEffectTag.Dispel } }, TargetType = SkillTargetType.SingleEnemy });

        // Act
        var action = _service.SelectAction(actor, new List<ServerCharacter> { ally }, new List<ServerCharacter> { enemy }, 12345, AutoBattlePolicy.Balanced);

        // Assert
        Assert.Equal(CombatActionType.Skill, action.Type);
        Assert.Equal(11, action.SkillId); // Should choose Dispel
    }

    [Fact]
    public void SelectAction_Cooldown_AvoidsSkill()
    {
        // Arrange
        var actor = new ServerCharacter { Id = 1, Name = "Actor", Sp = 100 };
        var enemy = new ServerCharacter { Id = 3, Name = "Enemy" };
        enemy.Con = 10; enemy.Hp = 50;

        // Skills: 10 (Damage)
        actor.KnownSkills.Add(10);
        _skillCatalogMock.Setup(x => x.GetSkillById(10)).Returns(new Skill
        {
            SkillId = 10,
            SpCost = 5,
            Effects = new List<SkillEffect> { new SkillEffect { Tag = SkillEffectTag.Damage } },
            TargetType = SkillTargetType.SingleEnemy
        });

        // Set Cooldown
        actor.SetSkillCooldown(10, 1);

        // Act
        var action = _service.SelectAction(actor, new List<ServerCharacter>(), new List<ServerCharacter> { enemy }, 12345, AutoBattlePolicy.Aggressive);

        // Assert
        // Should fallback to Attack (Id 0 usually implicit) or at least NOT use skill 10
        Assert.NotEqual(10, action.SkillId);
        Assert.Equal(CombatActionType.Attack, action.Type); // Fallback
    }

    [Fact]
    public void SelectAction_Deterministic()
    {
         var actor = new ServerCharacter { Id = 1, Name = "Actor", Sp = 100 };
         var enemy = new ServerCharacter { Id = 3, Name = "Enemy" };
         enemy.Con = 10; enemy.Hp = 50;

         actor.KnownSkills.Add(10);
         _skillCatalogMock.Setup(x => x.GetSkillById(10)).Returns(new Skill { SkillId = 10, SpCost = 5, Effects = new List<SkillEffect> { new SkillEffect { Tag = SkillEffectTag.Damage } }, TargetType = SkillTargetType.SingleEnemy });

         var action1 = _service.SelectAction(actor, new List<ServerCharacter>(), new List<ServerCharacter> { enemy }, 123, AutoBattlePolicy.Balanced);
         var action2 = _service.SelectAction(actor, new List<ServerCharacter>(), new List<ServerCharacter> { enemy }, 123, AutoBattlePolicy.Balanced);

         Assert.Equal(action1.Type, action2.Type);
         Assert.Equal(action1.SkillId, action2.SkillId);
         Assert.Equal(action1.TargetId, action2.TargetId);
    }
}
