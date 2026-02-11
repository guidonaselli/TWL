using System.Reflection;
using Moq;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Services;
using TWL.Tests.Skills; // For WaterTestCharacter

namespace TWL.Tests.Server.Simulation.Managers;

public class AutoBattleManagerTests
{
    private readonly AutoBattleManager _autoBattleManager;
    private readonly ISkillCatalog _skillCatalog;
    private readonly StatusEngine _statusEngine;

    public AutoBattleManagerTests()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Content/Data/skills.json"));

        var ctor = typeof(SkillRegistry).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
            Type.EmptyTypes, null);
        var registry = (SkillRegistry)ctor.Invoke(null);
        registry.LoadSkills(json);
        _skillCatalog = registry;
        _statusEngine = new StatusEngine();

        _autoBattleManager = new AutoBattleManager(_skillCatalog);
    }

    [Fact]
    public void GetBestAction_PrioritizesCleanse_WhenAllyDebuffed()
    {
        // Arrange
        // Set Con=10 => MaxHp=100. Set Hp=100.
        var actor = new WaterTestCharacter { Id = 1, Name = "Healer", Team = Team.Player, Sp = 100, Con = 10, Hp = 100 };
        var ally = new WaterTestCharacter { Id = 2, Name = "Ally", Team = Team.Player, Con = 10, Hp = 100 };
        var enemy = new WaterTestCharacter { Id = 3, Name = "Enemy", Team = Team.Enemy, Con = 10, Hp = 100 };

        actor.LearnSkill(3210); // Soothing Mist (Cleanse + Heal)
        actor.LearnSkill(3110); // Frost Bite (Damage + Seal)

        // Apply Debuff to Ally
        ally.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.DebuffStats, 10, 3, "Atk"), _statusEngine);

        var combatants = new List<ServerCombatant> { actor, ally, enemy };

        // Act
        var action = _autoBattleManager.GetBestAction(actor, combatants, AutoBattlePolicy.Balanced);

        // Assert
        Assert.NotNull(action);
        Assert.Equal(3210, action.SkillId); // Should pick Soothing Mist
        Assert.Equal(ally.Id, action.TargetId);
    }

    [Fact]
    public void GetBestAction_PrioritizesSeal_WhenEnemyDangerous()
    {
        // Arrange
        var actor = new WaterTestCharacter { Id = 1, Name = "Mage", Team = Team.Player, Sp = 100, Con = 10, Hp = 100 };
        // Boss: 1000 HP. Con 100 => MaxHp 1000. Hp 1000. Str 100 => Atk 200.
        var enemy = new WaterTestCharacter { Id = 3, Name = "Boss", Team = Team.Enemy, Con = 100, Hp = 1000, Str = 100 };

        actor.LearnSkill(3110); // Frost Bite (Seal)
        actor.LearnSkill(3001); // Aqua Impact (Damage)

        var combatants = new List<ServerCombatant> { actor, enemy };

        // Act
        // Use Balanced or Supportive/Aggressive policy. Balanced checks Control.
        var action = _autoBattleManager.GetBestAction(actor, combatants, AutoBattlePolicy.Balanced);

        // Assert
        Assert.NotNull(action);
        Assert.Equal(3110, action.SkillId); // Should pick Frost Bite (Seal)
        Assert.Equal(enemy.Id, action.TargetId);
    }

    [Fact]
    public void GetBestAction_FallsBackToDamage_WhenNoStatusNeeds()
    {
        // Arrange
        var actor = new WaterTestCharacter { Id = 1, Name = "Attacker", Team = Team.Player, Sp = 100, Con = 10, Hp = 100 };
        var enemy = new WaterTestCharacter { Id = 3, Name = "Enemy", Team = Team.Enemy, Con = 10, Hp = 100 };

        actor.LearnSkill(3010); // Aqua Crescent (Damage + Debuff)

        var combatants = new List<ServerCombatant> { actor, enemy };

        // Act
        var action = _autoBattleManager.GetBestAction(actor, combatants, AutoBattlePolicy.Aggressive);

        // Assert
        Assert.NotNull(action);
        // Should pick Damage skill. Aqua Crescent is damage.
        Assert.Equal(3010, action.SkillId);
    }

    [Fact]
    public void GetBestAction_AvoidsSeal_IfTargetIsImmune()
    {
        // Arrange
        var actor = new WaterTestCharacter { Id = 1, Name = "Mage", Team = Team.Player, Sp = 100, Con = 10, Hp = 100 };
        var enemy = new WaterTestCharacter { Id = 3, Name = "Boss", Team = Team.Enemy, Con = 100, Hp = 1000, Str = 100 };

        // Use Entangle (1210) - Pure Seal (No Damage) to ensure it's not picked as a fallback damage skill
        actor.LearnSkill(1210);
        actor.LearnSkill(3001); // Aqua Impact (Damage)

        // Give enemy 100% Seal Resistance
        enemy.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.BuffStats, 1.0f, 10, "SealResist"), _statusEngine);

        var combatants = new List<ServerCombatant> { actor, enemy };

        // Act
        var action = _autoBattleManager.GetBestAction(actor, combatants, AutoBattlePolicy.Balanced);

        // Assert
        Assert.NotNull(action);
        Assert.Equal(3001, action.SkillId); // Should pick Aqua Impact (Damage), skipping Entangle
    }
}
