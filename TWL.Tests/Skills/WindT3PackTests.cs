using System.Reflection;
using Moq;
using TWL.Server.Features.Combat;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Services;

namespace TWL.Tests.Skills;

public class WindTestCharacter : ServerCharacter
{
    public WindTestCharacter()
    {
        CharacterElement = Element.Wind;
        Con = 10;
        Int = 10;
        Wis = 10;
        Str = 10;
        Agi = 10;
        Hp = MaxHp;
        Sp = MaxSp;
    }
}

public class WindT3PackTests
{
    private readonly CombatManager _combatManager;
    private readonly Mock<IRandomService> _randomMock;
    private readonly Mock<ICombatResolver> _resolverMock;
    private readonly ISkillCatalog _skillCatalog;
    private readonly StatusEngine _statusEngine;

    public WindT3PackTests()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Content/Data/skills.json"));

        var ctor = typeof(SkillRegistry).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
            Type.EmptyTypes, null);
        var registry = (SkillRegistry)ctor.Invoke(null);
        registry.LoadSkills(json);
        _skillCatalog = registry;

        _randomMock = new Mock<IRandomService>();
        _randomMock.Setup(r => r.NextFloat(It.IsAny<string>())).Returns(0.5f);
        _randomMock.Setup(r => r.NextFloat(It.IsAny<float>(), It.IsAny<float>())).Returns(1.0f);

        _resolverMock = new Mock<ICombatResolver>();
        _resolverMock.Setup(r =>
                r.CalculateDamage(It.IsAny<ServerCombatant>(), It.IsAny<ServerCombatant>(),
                    It.IsAny<UseSkillRequest>()))
            .Returns(100);
        _resolverMock.Setup(r =>
                r.CalculateHeal(It.IsAny<ServerCombatant>(), It.IsAny<ServerCombatant>(),
                    It.IsAny<UseSkillRequest>()))
            .Returns(50);

        _statusEngine = new StatusEngine();
        _combatManager = new CombatManager(_resolverMock.Object, _randomMock.Object, _skillCatalog, _statusEngine);
    }

    [Fact]
    public void WindT3Pack_AreLoaded()
    {
        Assert.NotNull(_skillCatalog.GetSkillById(5020)); // Hurricane Slash I
        Assert.NotNull(_skillCatalog.GetSkillById(5021)); // Hurricane Slash II
        Assert.NotNull(_skillCatalog.GetSkillById(5022)); // Hurricane Slash III

        Assert.NotNull(_skillCatalog.GetSkillById(5120)); // Typhoon I
        Assert.NotNull(_skillCatalog.GetSkillById(5121)); // Typhoon II
        Assert.NotNull(_skillCatalog.GetSkillById(5122)); // Typhoon III

        Assert.NotNull(_skillCatalog.GetSkillById(5220)); // Nullify I
        Assert.NotNull(_skillCatalog.GetSkillById(5221)); // Nullify II
        Assert.NotNull(_skillCatalog.GetSkillById(5222)); // Nullify III
    }

    [Fact]
    public void Nullify_DispelsEnemyBuff()
    {
        // Arrange
        var attacker = new WindTestCharacter { Id = 1, Name = "WindMage", Team = Team.Player };
        var enemy = new WindTestCharacter { Id = 2, Name = "BuffedEnemy", Team = Team.Enemy };

        _combatManager.RegisterCombatant(attacker);
        _combatManager.RegisterCombatant(enemy);
        _combatManager.StartEncounter(1, new List<ServerCharacter> { attacker, enemy });

        // Apply Buff to Enemy
        enemy.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.BuffStats, 50, 3, "Atk")
        {
            ConflictGroup = "Buff_Atk"
        }, _statusEngine);

        Assert.Contains(enemy.StatusEffects, e => e.Tag == SkillEffectTag.BuffStats);

        // Learn Nullify
        attacker.LearnSkill(5220);

        // Act
        var request = new UseSkillRequest { PlayerId = attacker.Id, TargetId = enemy.Id, SkillId = 5220 };
        _combatManager.UseSkill(request);

        // Assert
        Assert.Empty(enemy.StatusEffects.Where(e => e.Tag == SkillEffectTag.BuffStats));
    }

    [Fact]
    public void Nullify_DispelsEnemyShield()
    {
        // Arrange
        var attacker = new WindTestCharacter { Id = 1, Name = "WindMage", Team = Team.Player };
        var enemy = new WindTestCharacter { Id = 2, Name = "ShieldedEnemy", Team = Team.Enemy };

        _combatManager.RegisterCombatant(attacker);
        _combatManager.RegisterCombatant(enemy);
        _combatManager.StartEncounter(1, new List<ServerCharacter> { attacker, enemy });

        // Apply Shield to Enemy
        enemy.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.Shield, 100, 3, "General")
        {
            ConflictGroup = "Shield_General"
        }, _statusEngine);

        Assert.Contains(enemy.StatusEffects, e => e.Tag == SkillEffectTag.Shield);

        // Learn Nullify
        attacker.LearnSkill(5220);

        // Act
        var request = new UseSkillRequest { PlayerId = attacker.Id, TargetId = enemy.Id, SkillId = 5220 };
        _combatManager.UseSkill(request);

        // Assert
        Assert.Empty(enemy.StatusEffects.Where(e => e.Tag == SkillEffectTag.Shield));
    }

    [Fact]
    public void AutoBattle_UsesNullify_WhenEnemyBuffed()
    {
        // Arrange
        var autoBattleManager = new AutoBattleService(_skillCatalog);
        var actor = new WindTestCharacter { Id = 1, Name = "Support", Team = Team.Player };
        var enemy = new WindTestCharacter { Id = 2, Name = "Boss", Team = Team.Enemy };

        actor.LearnSkill(5220); // Nullify (Dispel)
        actor.LearnSkill(5101); // Air Blade (Damage)

        // Apply Buff to Enemy
        enemy.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.BuffStats, 50, 3, "Def"), _statusEngine);

        var combatants = new List<ServerCombatant> { actor, enemy };

        // Act
        var action = autoBattleManager.GetBestAction(actor, combatants, AutoBattlePolicy.Supportive);

        // Assert
        Assert.NotNull(action);
        Assert.Equal(5220, action.SkillId); // Should prioritize Dispel
        Assert.Equal(enemy.Id, action.TargetId);
    }
}
