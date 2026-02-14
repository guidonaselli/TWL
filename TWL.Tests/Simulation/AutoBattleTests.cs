using TWL.Shared.Domain.Battle;
using Moq;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Services;

namespace TWL.Tests.Simulation;

public class AutoBattleTests
{
    private readonly AutoBattleService _autoBattleManager;
    private readonly Mock<ISkillCatalog> _skillCatalogMock;
    private readonly Mock<IRandomService> _randomMock;
    private readonly StatusEngine _statusEngine; // Use real engine for logic

    public AutoBattleTests()
    {
        _skillCatalogMock = new Mock<ISkillCatalog>();
        _autoBattleManager = new AutoBattleService(_skillCatalogMock.Object);
        _randomMock = new Mock<IRandomService>();
        _statusEngine = new StatusEngine();
    }

    private void SetupSkill(int id, SkillEffectTag tag, SkillTargetType targetType, int spCost, string? param = null)
    {
        var skill = new Skill
        {
            SkillId = id,
            Name = $"Skill_{id}",
            Effects = new List<SkillEffect>
            {
                new() { Tag = tag, Param = param, Value = 10, Duration = 3 }
            },
            TargetType = targetType,
            SpCost = spCost,
            Cooldown = 0
        };
        _skillCatalogMock.Setup(x => x.GetSkillById(id)).Returns(skill);
    }

    [Fact]
    public void GetBestAction_Survival_PrioritizesHeal_WhenAllyLowHp()
    {
        // Arrange
        var actor = new AutoBattleTestCharacter { Id = 1, Team = Team.Player, Sp = 100 };
        var ally = new AutoBattleTestCharacter { Id = 2, Team = Team.Player, Con = 10 }; // MaxHp = 100
        ally.Hp = 20; // 20% < 30% Critical
        var enemy = new AutoBattleTestCharacter { Id = 3, Team = Team.Enemy, Hp = 100 };

        SetupSkill(100, SkillEffectTag.Heal, SkillTargetType.SingleAlly, 10);
        actor.SkillMastery[100] = new SkillMastery();

        // Act
        var result = _autoBattleManager.GetBestAction(actor, new[] { actor, ally, enemy });

        // Assert
        Assert.NotNull(result);
        Assert.Equal(100, result.SkillId);
        Assert.Equal(ally.Id, result.TargetId);
    }

    [Fact]
    public void GetBestAction_Support_PrioritizesCleanse_WhenAllyDebuffed()
    {
        // Arrange
        var actor = new AutoBattleTestCharacter { Id = 1, Team = Team.Player, Sp = 100 };
        var ally = new AutoBattleTestCharacter { Id = 2, Team = Team.Player, Hp = 100, Con = 10 };
        ally.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.DebuffStats, 10, 3, "Atk"), _statusEngine);
        var enemy = new AutoBattleTestCharacter { Id = 3, Team = Team.Enemy, Hp = 100 };

        SetupSkill(101, SkillEffectTag.Cleanse, SkillTargetType.SingleAlly, 10);
        actor.SkillMastery[101] = new SkillMastery();

        // Act
        var result = _autoBattleManager.GetBestAction(actor, new[] { actor, ally, enemy }, AutoBattlePolicy.Supportive);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(101, result.SkillId);
        Assert.Equal(ally.Id, result.TargetId);
    }

    [Fact]
    public void GetBestAction_Control_PrioritizesDispel_WhenEnemyBuffed()
    {
        // Arrange
        var actor = new AutoBattleTestCharacter { Id = 1, Team = Team.Player, Sp = 100 };
        var enemy = new AutoBattleTestCharacter { Id = 2, Team = Team.Enemy, Hp = 100 };
        enemy.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.BuffStats, 10, 3, "Atk"), _statusEngine);

        SetupSkill(102, SkillEffectTag.Dispel, SkillTargetType.SingleEnemy, 10);
        actor.SkillMastery[102] = new SkillMastery();

        // Act
        var result = _autoBattleManager.GetBestAction(actor, new[] { actor, enemy }, AutoBattlePolicy.Supportive);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(102, result.SkillId);
        Assert.Equal(enemy.Id, result.TargetId);
    }

    [Fact]
    public void GetBestAction_Control_PrioritizesSeal_OnStrongEnemy()
    {
        // Arrange
        var actor = new AutoBattleTestCharacter { Id = 1, Team = Team.Player, Sp = 100 };
        var weakEnemy = new AutoBattleTestCharacter { Id = 2, Team = Team.Enemy, Str = 5 }; // Weak
        var strongEnemy = new AutoBattleTestCharacter { Id = 3, Team = Team.Enemy, Str = 20 }; // Strong

        SetupSkill(103, SkillEffectTag.Seal, SkillTargetType.SingleEnemy, 10);
        actor.SkillMastery[103] = new SkillMastery();

        // Act
        var result = _autoBattleManager.GetBestAction(actor, new[] { actor, weakEnemy, strongEnemy }, AutoBattlePolicy.Aggressive);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(103, result.SkillId);
        Assert.Equal(strongEnemy.Id, result.TargetId);
    }

    [Fact]
    public void GetBestAction_Control_SkipsSeal_IfEnemyResistant()
    {
        // Arrange
        var actor = new AutoBattleTestCharacter { Id = 1, Team = Team.Player, Sp = 100 };
        var resistantEnemy = new AutoBattleTestCharacter { Id = 2, Team = Team.Enemy, Str = 20 };
        // Add 100% Seal Resist
        resistantEnemy.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.BuffStats, 1.0f, 10, "SealResist"), _statusEngine);

        SetupSkill(103, SkillEffectTag.Seal, SkillTargetType.SingleEnemy, 10);
        SetupSkill(105, SkillEffectTag.Damage, SkillTargetType.SingleEnemy, 10); // Fallback

        actor.SkillMastery[103] = new SkillMastery();
        actor.SkillMastery[105] = new SkillMastery();

        // Act
        var result = _autoBattleManager.GetBestAction(actor, new[] { actor, resistantEnemy }, AutoBattlePolicy.Aggressive);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(103, result.SkillId); // Should NOT seal
        Assert.Equal(105, result.SkillId); // Should fallback to damage
    }

    [Fact]
    public void GetBestAction_SpConstraint_SkipsHighCostSkill_IfSpLow()
    {
        // Arrange
        var actor = new AutoBattleTestCharacter { Id = 1, Team = Team.Player, Sp = 15 }; // 15 SP
        _autoBattleManager.MinSpThreshold = 10; // Reserve 10 SP

        var enemy = new AutoBattleTestCharacter { Id = 2, Team = Team.Enemy, Hp = 100 };

        SetupSkill(105, SkillEffectTag.Damage, SkillTargetType.SingleEnemy, 10); // Cost 10. SP 15 - 10 = 5 < 10 Threshold. Should skip.
        SetupSkill(106, SkillEffectTag.Damage, SkillTargetType.SingleEnemy, 2); // Cost 2. SP 15 - 2 = 13 > 10 Threshold. OK.

        actor.SkillMastery[105] = new SkillMastery();
        actor.SkillMastery[106] = new SkillMastery();

        // Act
        var result = _autoBattleManager.GetBestAction(actor, new[] { actor, enemy }, AutoBattlePolicy.Balanced);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(106, result.SkillId); // Should pick cheap skill
    }

    [Fact]
    public void GetBestAction_Attack_PrioritizesElementalAdvantage()
    {
        // Arrange
        // Attacker is Water
        var actor = new AutoBattleTestCharacter { Id = 1, Team = Team.Player, Sp = 100 };
        actor.CharacterElement = Element.Water;

        // Enemies: Fire (Weak to Water), Wind (Neutral), Earth (Strong against Water -> Water does 0.5x)
        // Water > Fire (1.5x)
        // Water vs Wind (1.0x)
        // Water vs Earth (0.5x)

        var fireEnemy = new AutoBattleTestCharacter { Id = 2, Team = Team.Enemy, Hp = 100 };
        fireEnemy.CharacterElement = Element.Fire;

        var windEnemy = new AutoBattleTestCharacter { Id = 3, Team = Team.Enemy, Hp = 100 };
        windEnemy.CharacterElement = Element.Wind;

        var earthEnemy = new AutoBattleTestCharacter { Id = 4, Team = Team.Enemy, Hp = 100 };
        earthEnemy.CharacterElement = Element.Earth;

        // Setup simple damage skill
        var skillId = 200;
        var skill = new Skill
        {
            SkillId = skillId,
            Name = "WaterAttack",
            TargetType = SkillTargetType.SingleEnemy,
            SpCost = 5,
            Effects = new List<SkillEffect> { new() { Tag = SkillEffectTag.Damage } }
        };
        _skillCatalogMock.Setup(x => x.GetSkillById(skillId)).Returns(skill);
        actor.SkillMastery[skillId] = new SkillMastery();

        // Act
        // Pass all enemies
        var result = _autoBattleManager.GetBestAction(actor, new[] { actor, fireEnemy, windEnemy, earthEnemy }, AutoBattlePolicy.Aggressive);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(fireEnemy.Id, result.TargetId); // Should pick Fire (1.5x) over others
    }
}

// Helper class for accessing protected members/logic if needed, but mostly standard
public class AutoBattleTestCharacter : ServerCharacter
{
    public AutoBattleTestCharacter()
    {
        // Initialize defaults
        Str = 10; Con = 10; Int = 10; Wis = 10; Agi = 10;
        Hp = MaxHp;
        Sp = MaxSp;
    }

    public override void ReplaceSkill(int oldId, int newId)
    {
        // Mock
        SkillMastery.TryRemove(oldId, out _);
        SkillMastery[newId] = new SkillMastery();
    }
}
