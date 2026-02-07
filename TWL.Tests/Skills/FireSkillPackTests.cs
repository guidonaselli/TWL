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

public class FireTestCharacter : ServerCharacter
{
    public void ClearStatusEffects()
    {
        lock (_statusLock)
        {
            _statusEffects.Clear();
            IsDirty = true;
        }
    }
}

public class FireSkillPackTests
{
    private readonly CombatManager _combatManager;
    private readonly Mock<IRandomService> _randomMock;
    private readonly Mock<ICombatResolver> _resolverMock;
    private readonly ISkillCatalog _skillCatalog;
    private readonly StatusEngine _statusEngine;

    public FireSkillPackTests()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Content/Data/skills.json"));

        var ctor = typeof(SkillRegistry).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
            Type.EmptyTypes, null);
        var registry = (SkillRegistry)ctor.Invoke(null);
        registry.LoadSkills(json);
        _skillCatalog = registry;

        _randomMock = new Mock<IRandomService>();
        _randomMock.Setup(r => r.NextFloat()).Returns(0.5f); // Predictable RNG
        _randomMock.Setup(r => r.NextFloat(It.IsAny<float>(), It.IsAny<float>())).Returns(1.0f);

        _resolverMock = new Mock<ICombatResolver>();
        _resolverMock.Setup(r =>
                r.CalculateDamage(It.IsAny<ServerCombatant>(), It.IsAny<ServerCombatant>(),
                    It.IsAny<UseSkillRequest>()))
            .Returns(20); // Base damage for testing

        _statusEngine = new StatusEngine();
        _combatManager = new CombatManager(_resolverMock.Object, _randomMock.Object, _skillCatalog, _statusEngine);
    }

    [Fact]
    public void BlazingStrike_IsLoaded_And_UnlockedByT1()
    {
        var skill = _skillCatalog.GetSkillById(4010);
        Assert.NotNull(skill);
        Assert.Equal("Blazing Strike", skill.Name);
        Assert.Equal(2, skill.Tier);
        Assert.Equal(SkillTargetType.RowEnemies, skill.TargetType);

        Assert.NotNull(skill.UnlockRules);
        Assert.Equal(4003, skill.UnlockRules.ParentSkillId);
        Assert.Equal(10, skill.UnlockRules.ParentSkillRank);
    }

    [Fact]
    public void Inferno_IsLoaded_And_UnlockedByT1()
    {
        var skill = _skillCatalog.GetSkillById(4110);
        Assert.NotNull(skill);
        Assert.Equal("Inferno", skill.Name);
        Assert.Equal(2, skill.Tier);
        Assert.Equal(SkillTargetType.AllEnemies, skill.TargetType);

        Assert.NotNull(skill.UnlockRules);
        Assert.Equal(4103, skill.UnlockRules.ParentSkillId);
        Assert.Equal(10, skill.UnlockRules.ParentSkillRank);
    }

    [Fact]
    public void InnerFire_IsLoaded_And_UnlockedByT1()
    {
        var skill = _skillCatalog.GetSkillById(4210);
        Assert.NotNull(skill);
        Assert.Equal("Inner Fire", skill.Name);
        Assert.Equal(2, skill.Tier);
        Assert.Equal(SkillTargetType.SingleAlly, skill.TargetType);

        Assert.NotNull(skill.UnlockRules);
        Assert.Equal(4203, skill.UnlockRules.ParentSkillId);
        Assert.Equal(10, skill.UnlockRules.ParentSkillRank);
    }

    [Fact]
    public void InnerFire_BuffsMat_And_RespectsStacking()
    {
        // Arrange
        var caster = new FireTestCharacter { Id = 1, Name = "Mage", Wis = 20, Sp = 100, Team = Team.Player, Con = 10, Hp = 100 };
        var ally = new FireTestCharacter { Id = 2, Name = "Ally", Team = Team.Player, Con = 10, Hp = 100 };

        _combatManager.RegisterCombatant(caster);
        _combatManager.RegisterCombatant(ally);
        _combatManager.StartEncounter(1, new List<ServerCharacter> { caster, ally });

        caster.LearnSkill(4210); // Inner Fire

        var request = new UseSkillRequest { PlayerId = caster.Id, TargetId = ally.Id, SkillId = 4210 };

        // Act 1: Apply Buff
        var results = _combatManager.UseSkill(request);

        // Assert 1
        Assert.NotEmpty(results);
        var buff = ally.StatusEffects.FirstOrDefault(e => e.Tag == SkillEffectTag.BuffStats && e.Param == "Mat");
        Assert.NotNull(buff);
        Assert.Equal(25, buff.Value);
        Assert.Equal(3, buff.TurnsRemaining);
        Assert.Equal("Buff_Mat", buff.ConflictGroup);

        // Act 2: Apply Again (RefreshDuration)
        // Reduce duration artificially
        buff.TurnsRemaining = 1;
        _combatManager.UseSkill(request);

        // Assert 2
        var buffAfter = ally.StatusEffects.FirstOrDefault(e => e.Tag == SkillEffectTag.BuffStats && e.Param == "Mat");
        Assert.NotNull(buffAfter);
        Assert.Equal(25, buffAfter.Value); // Value should not stack
        Assert.Equal(3, buffAfter.TurnsRemaining); // Duration should refresh to max (3)
        Assert.Single(ally.StatusEffects.Where(e => e.ConflictGroup == "Buff_Mat"));
    }

    [Fact]
    public void BlazingStrike_HitsRow()
    {
        // Arrange
        var attacker = new FireTestCharacter { Id = 1, Name = "Attacker", Str = 50, Sp = 100, Team = Team.Player, Con = 10, Hp = 100, Agi = 100 };
        var enemy1 = new FireTestCharacter { Id = 10, Name = "Enemy1", Team = Team.Enemy, Con = 10, Hp = 100, Agi = 10 };

        _combatManager.RegisterCombatant(attacker);
        _combatManager.RegisterCombatant(enemy1);
        _combatManager.StartEncounter(1, new List<ServerCharacter> { attacker, enemy1 });

        attacker.LearnSkill(4010); // Blazing Strike

        var request = new UseSkillRequest { PlayerId = attacker.Id, TargetId = enemy1.Id, SkillId = 4010 };

        // Act
        var results = _combatManager.UseSkill(request);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.TargetId == enemy1.Id && r.Damage > 0);
    }

    [Fact]
    public void Inferno_HitsAll()
    {
        // Arrange
        var attacker = new FireTestCharacter { Id = 1, Name = "Mage", Int = 50, Sp = 100, Team = Team.Player, Con = 10, Hp = 100, Agi = 100 };
        var enemy1 = new FireTestCharacter { Id = 10, Name = "Enemy1", Team = Team.Enemy, Con = 10, Hp = 100, Agi = 10 };
        var enemy2 = new FireTestCharacter { Id = 11, Name = "Enemy2", Team = Team.Enemy, Con = 10, Hp = 100, Agi = 10 };

        _combatManager.RegisterCombatant(attacker);
        _combatManager.RegisterCombatant(enemy1);
        _combatManager.RegisterCombatant(enemy2);
        _combatManager.StartEncounter(1, new List<ServerCharacter> { attacker, enemy1, enemy2 });

        attacker.LearnSkill(4110); // Inferno

        // Target can be any enemy, logic should pick all
        var request = new UseSkillRequest { PlayerId = attacker.Id, TargetId = enemy1.Id, SkillId = 4110 };

        // Act
        var results = _combatManager.UseSkill(request);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.TargetId == enemy1.Id);
        Assert.Contains(results, r => r.TargetId == enemy2.Id);
    }

    [Fact]
    public void AutoBattle_Selects_InnerFire_ForMageAlly()
    {
        // Arrange
        var autoBattle = new AutoBattleManager(_skillCatalog);
        var supporter = new FireTestCharacter { Id = 1, Name = "Supporter", Team = Team.Player, Sp = 100, Con = 10, Hp = 100 };
        var mageAlly = new FireTestCharacter { Id = 2, Name = "MageAlly", Team = Team.Player, Con = 10, Hp = 100, Int = 50 }; // High Int
        var enemy = new FireTestCharacter { Id = 3, Name = "Enemy", Team = Team.Enemy, Con = 10, Hp = 100 };

        supporter.LearnSkill(4210); // Inner Fire (Mat Buff)

        // Act
        // AutoBattle logic currently picks any valid buff if stats allow.
        // We verify it CAN pick it.
        var action = autoBattle.GetBestAction(supporter, new List<ServerCombatant> { supporter, mageAlly, enemy }, AutoBattlePolicy.Supportive);

        // Assert
        Assert.NotNull(action);
        Assert.Equal(4210, action.SkillId);
        Assert.Equal(mageAlly.Id, action.TargetId);
    }
}
