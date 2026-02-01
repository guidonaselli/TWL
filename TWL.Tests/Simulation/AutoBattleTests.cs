using Moq;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Skills;

namespace TWL.Tests.Simulation;

public class AutoBattleTests
{
    private readonly AutoBattleService _service;
    private readonly Mock<ISkillCatalog> _skillCatalogMock;

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
        var ally = new ServerCharacter { Id = 2, Name = "Ally", Con = 10, Hp = 100 }; // Full HP

        var enemy = new ServerCharacter { Id = 3, Name = "Enemy", Con = 10, Hp = 50 };
        // Enemy has buff
        enemy.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.Shield, 10, 3), new StatusEngine());

        // Skills:
        // 10: Damage
        // 11: Dispel
        actor.LearnSkill(10);
        actor.LearnSkill(11);

        _skillCatalogMock.Setup(x => x.GetSkillById(10)).Returns(new Skill
        {
            SkillId = 10, SpCost = 5, Effects = new List<SkillEffect> { new() { Tag = SkillEffectTag.Damage } },
            TargetType = SkillTargetType.SingleEnemy
        });
        _skillCatalogMock.Setup(x => x.GetSkillById(11)).Returns(new Skill
        {
            SkillId = 11, SpCost = 5, Effects = new List<SkillEffect> { new() { Tag = SkillEffectTag.Dispel } },
            TargetType = SkillTargetType.SingleEnemy
        });

        // Act
        var action = _service.SelectAction(actor, new List<ServerCharacter> { ally },
            new List<ServerCharacter> { enemy }, 12345);

        // Assert
        Assert.Equal(CombatActionType.Skill, action.Type);
        Assert.Equal(11, action.SkillId); // Should choose Dispel
    }

    [Fact]
    public void SelectAction_Cooldown_AvoidsSkill()
    {
        // Arrange
        var actor = new ServerCharacter { Id = 1, Name = "Actor", Sp = 100 };
        var enemy = new ServerCharacter { Id = 3, Name = "Enemy", Con = 10, Hp = 50 };

        // Skills: 10 (Damage)
        actor.LearnSkill(10);
        _skillCatalogMock.Setup(x => x.GetSkillById(10)).Returns(new Skill
        {
            SkillId = 10,
            SpCost = 5,
            Effects = new List<SkillEffect> { new() { Tag = SkillEffectTag.Damage } },
            TargetType = SkillTargetType.SingleEnemy
        });

        // Set Cooldown
        actor.SetSkillCooldown(10, 1);

        // Act
        var action = _service.SelectAction(actor, new List<ServerCharacter>(), new List<ServerCharacter> { enemy },
            12345, AutoBattlePolicy.Aggressive);

        // Assert
        // Should fallback to Attack (Id 0 usually implicit) or at least NOT use skill 10
        Assert.NotEqual(10, action.SkillId);
        Assert.Equal(CombatActionType.Attack, action.Type); // Fallback
    }

    [Fact]
    public void SelectAction_Deterministic()
    {
        var actor = new ServerCharacter { Id = 1, Name = "Actor", Sp = 100 };
        var enemy = new ServerCharacter { Id = 3, Name = "Enemy", Con = 10, Hp = 50 };

        actor.LearnSkill(10);
        _skillCatalogMock.Setup(x => x.GetSkillById(10)).Returns(new Skill
        {
            SkillId = 10, SpCost = 5, Effects = new List<SkillEffect> { new() { Tag = SkillEffectTag.Damage } },
            TargetType = SkillTargetType.SingleEnemy
        });

        var action1 =
            _service.SelectAction(actor, new List<ServerCharacter>(), new List<ServerCharacter> { enemy }, 123);
        var action2 =
            _service.SelectAction(actor, new List<ServerCharacter>(), new List<ServerCharacter> { enemy }, 123);

        Assert.Equal(action1.Type, action2.Type);
        Assert.Equal(action1.SkillId, action2.SkillId);
        Assert.Equal(action1.TargetId, action2.TargetId);
    }

    [Fact]
    public void SelectAction_Cleanse_Prioritized_WhenAllyDebuffed()
    {
        // Arrange
        var actor = new ServerCharacter { Id = 1, Name = "Actor", Sp = 100 };
        var ally = new ServerCharacter { Id = 2, Name = "Ally", Con = 10, Hp = 100 };
        // Ally has Seal debuff
        ally.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.Seal, 0, 3), new StatusEngine());

        // Skills: Cleanse (12)
        actor.LearnSkill(12);
        _skillCatalogMock.Setup(x => x.GetSkillById(12)).Returns(new Skill
        {
            SkillId = 12, SpCost = 5,
            Effects = new List<SkillEffect> { new() { Tag = SkillEffectTag.Cleanse } },
            TargetType = SkillTargetType.SingleAlly
        });

        // Act
        var action = _service.SelectAction(actor, new List<ServerCharacter> { ally }, new List<ServerCharacter>(), 123,
            AutoBattlePolicy.Supportive);

        // Assert
        Assert.Equal(CombatActionType.Skill, action.Type);
        Assert.Equal(12, action.SkillId);
    }

    [Fact]
    public void SelectAction_SP_Conservation_SkipsHighCostSkill()
    {
        // Arrange
        var actor = new ServerCharacter { Id = 1, Name = "Actor", Sp = 20 };
        _service.MinSpThreshold = 10;
        var enemy = new ServerCharacter { Id = 3, Name = "Enemy", Con = 10, Hp = 100 };

        // Skills:
        // 20: Cheap Damage (5 SP)
        // 21: Expensive Damage (15 SP) -> Remainder 5 < 10. Skip.

        actor.LearnSkill(20);
        actor.LearnSkill(21);
        _skillCatalogMock.Setup(x => x.GetSkillById(20)).Returns(new Skill
        {
            SkillId = 20, SpCost = 5, Effects = new List<SkillEffect> { new() { Tag = SkillEffectTag.Damage } },
            TargetType = SkillTargetType.SingleEnemy
        });
        _skillCatalogMock.Setup(x => x.GetSkillById(21)).Returns(new Skill
        {
            SkillId = 21, SpCost = 15, Effects = new List<SkillEffect> { new() { Tag = SkillEffectTag.Damage } },
            TargetType = SkillTargetType.SingleEnemy
        });

        // Act
        var action =
            _service.SelectAction(actor, new List<ServerCharacter>(), new List<ServerCharacter> { enemy }, 123);

        // Assert
        Assert.Equal(20, action.SkillId); // Picks cheap one
    }

    [Fact]
    public void SelectAction_ConflictingBuff_Avoided()
    {
        // Arrange
        var actor = new ServerCharacter { Id = 1, Name = "Actor", Sp = 100 };
        var ally = new ServerCharacter { Id = 2, Name = "Ally", Con = 10, Hp = 100 };

        // Ally already has "Buff_Def"
        ally.AddStatusEffect(
            new StatusEffectInstance(SkillEffectTag.BuffStats, 10, 3, "Def") { ConflictGroup = "Buff_Def" },
            new StatusEngine());

        // Skills: Buff Def (Cost 10, ConflictGroup "Buff_Def")
        actor.LearnSkill(30);
        _skillCatalogMock.Setup(x => x.GetSkillById(30)).Returns(new Skill
        {
            SkillId = 30, SpCost = 10,
            Effects = new List<SkillEffect>
                { new() { Tag = SkillEffectTag.BuffStats, Param = "Def", ConflictGroup = "Buff_Def" } },
            TargetType = SkillTargetType.SingleAlly
        });

        // Act
        var action = _service.SelectAction(actor, new List<ServerCharacter> { ally }, new List<ServerCharacter>(), 123,
            AutoBattlePolicy.Supportive);

        // Assert
        // Should NOT pick skill 30 because conflict exists.
        // Should fallback to Defend/Attack (but no enemies, so Defend)
        Assert.NotEqual(30, action.SkillId);
        Assert.Equal(CombatActionType.Defend, action.Type);
    }
}