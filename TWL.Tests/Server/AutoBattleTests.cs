using TWL.Shared.Domain.Battle;
using Moq;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Services;

namespace TWL.Tests.Server;

public class AutoBattleTests
{
    private readonly AutoBattleService _manager;
    private readonly Mock<ISkillCatalog> _skillCatalogMock;
    private readonly Mock<IRandomService> _randomMock;

    public AutoBattleTests()
    {
        _skillCatalogMock = new Mock<ISkillCatalog>();
        _randomMock = new Mock<IRandomService>();
        _manager = new AutoBattleService(_skillCatalogMock.Object);
    }

    private ServerCharacter CreateCharacter(int id, Team team, int hp, int maxHp)
    {
        var c = new ServerCharacter { Id = id, Team = team, Con = maxHp / 10 };
        c.Hp = hp;
        c.Sp = 100;
        return c;
    }

    private void SetupSkill(int skillId, SkillEffectTag tag, SkillTargetType targetType, int spCost = 10)
    {
        var skill = new Skill
        {
            SkillId = skillId,
            SpCost = spCost,
            TargetType = targetType,
            Effects = new List<SkillEffect> { new() { Tag = tag } }
        };
        _skillCatalogMock.Setup(x => x.GetSkillById(skillId)).Returns(skill);
    }

    [Fact]
    public void GetBestAction_Survival_HealsLowHpAlly()
    {
        // Arrange
        var actor = CreateCharacter(1, Team.Player, 100, 100);
        var ally = CreateCharacter(2, Team.Player, 20, 100); // 20% HP
        var enemy = CreateCharacter(3, Team.Enemy, 100, 100);

        // Add Heal Skill
        actor.IncrementSkillUsage(101); // Learn skill
        SetupSkill(101, SkillEffectTag.Heal, SkillTargetType.SingleAlly);

        // Act
        var result = _manager.GetBestAction(actor, new[] { actor, ally, enemy }, AutoBattlePolicy.Balanced);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(101, result.SkillId);
        Assert.Equal(ally.Id, result.TargetId);
    }

    [Fact]
    public void GetBestAction_Aggressive_IgnoresHealing()
    {
        // Arrange
        var actor = CreateCharacter(1, Team.Player, 100, 100);
        var ally = CreateCharacter(2, Team.Player, 20, 100); // 20% HP
        var enemy = CreateCharacter(3, Team.Enemy, 100, 100);

        // Add Heal Skill & Damage Skill
        actor.IncrementSkillUsage(101);
        SetupSkill(101, SkillEffectTag.Heal, SkillTargetType.SingleAlly);

        actor.IncrementSkillUsage(102);
        SetupSkill(102, SkillEffectTag.Damage, SkillTargetType.SingleEnemy);

        // Act
        var result = _manager.GetBestAction(actor, new[] { actor, ally, enemy }, AutoBattlePolicy.Aggressive);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(102, result.SkillId); // Should attack
        Assert.Equal(enemy.Id, result.TargetId);
    }

    [Fact]
    public void GetBestAction_Support_CleansesDebuff()
    {
        // Arrange
        var actor = CreateCharacter(1, Team.Player, 100, 100);
        var ally = CreateCharacter(2, Team.Player, 80, 100);
        var enemy = CreateCharacter(3, Team.Enemy, 100, 100);

        // Apply debuff to ally
        ally.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.DebuffStats, 10, 3), new StatusEngine());

        // Add Cleanse Skill
        actor.IncrementSkillUsage(201);
        SetupSkill(201, SkillEffectTag.Cleanse, SkillTargetType.SingleAlly);

        // Act
        var result = _manager.GetBestAction(actor, new[] { actor, ally, enemy }, AutoBattlePolicy.Supportive);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(201, result.SkillId);
        Assert.Equal(ally.Id, result.TargetId);
    }

    [Fact]
    public void GetBestAction_Control_SealsEnemy()
    {
        // Arrange
        var actor = CreateCharacter(1, Team.Player, 100, 100);
        var enemy = CreateCharacter(2, Team.Enemy, 100, 100);

        // Add Seal Skill
        actor.IncrementSkillUsage(301);
        SetupSkill(301, SkillEffectTag.Seal, SkillTargetType.SingleEnemy);

        // Act
        var result = _manager.GetBestAction(actor, new[] { actor, enemy }, AutoBattlePolicy.Balanced);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(301, result.SkillId);
        Assert.Equal(enemy.Id, result.TargetId);
    }

    [Fact]
    public void GetBestAction_Attack_ChoosesWeakestEnemy()
    {
         // Arrange
        var actor = CreateCharacter(1, Team.Player, 100, 100);
        var enemy1 = CreateCharacter(2, Team.Enemy, 100, 100);
        var enemy2 = CreateCharacter(3, Team.Enemy, 50, 100); // Weaker

        // Add Damage Skill
        actor.IncrementSkillUsage(401);
        SetupSkill(401, SkillEffectTag.Damage, SkillTargetType.SingleEnemy);

        // Act
        var result = _manager.GetBestAction(actor, new[] { actor, enemy1, enemy2 }, AutoBattlePolicy.Balanced);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(401, result.SkillId);
        Assert.Equal(enemy2.Id, result.TargetId);
    }
}
