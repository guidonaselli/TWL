using TWL.Shared.Domain.Battle;
using System.Reflection;
using Moq;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Services;
using TWL.Tests.Skills;
using Xunit;

namespace TWL.Tests.Server.Simulation.Managers;

public class AutoBattleServiceTests
{
    private readonly AutoBattleService _autoBattleManager;
    private readonly ISkillCatalog _skillCatalog;
    private readonly StatusEngine _statusEngine;

    public AutoBattleServiceTests()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Content/Data/skills.json"));

        var ctor = typeof(SkillRegistry).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
            Type.EmptyTypes, null);
        var registry = (SkillRegistry)ctor.Invoke(null);
        registry.LoadSkills(json);
        _skillCatalog = registry;
        _statusEngine = new StatusEngine();

        _autoBattleManager = new AutoBattleService(_skillCatalog);
    }

    [Fact]
    public void GetBestAction_PrioritizesCleanse_WhenAllyDebuffed()
    {
        var actor = new WaterTestCharacter { Id = 1, Name = "Healer", Team = Team.Player, Sp = 100, Con = 10, Hp = 100 };
        var ally = new WaterTestCharacter { Id = 2, Name = "Ally", Team = Team.Player, Con = 10, Hp = 100 };
        var enemy = new WaterTestCharacter { Id = 3, Name = "Enemy", Team = Team.Enemy, Con = 10, Hp = 100 };

        actor.LearnSkill(3210); // Soothing Mist (Cleanse + Heal)
        actor.LearnSkill(3110); // Frost Bite (Damage + Seal)

        ally.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.DebuffStats, 10, 3, "Atk"), _statusEngine);

        var combatants = new List<ServerCombatant> { actor, ally, enemy };

        var action = _autoBattleManager.GetBestAction(actor, combatants, AutoBattlePolicy.Balanced);

        Assert.NotNull(action);
        Assert.Equal(3210, action.SkillId);
        Assert.Equal(ally.Id, action.TargetId);
    }

    [Fact]
    public void GetBestAction_PrioritizesSeal_WhenEnemyDangerous()
    {
        var actor = new WaterTestCharacter { Id = 1, Name = "Mage", Team = Team.Player, Sp = 100, Con = 10, Hp = 100 };
        var enemy = new WaterTestCharacter { Id = 3, Name = "Boss", Team = Team.Enemy, Con = 100, Hp = 1000, Str = 100 };

        actor.LearnSkill(3110);
        actor.LearnSkill(3001);

        var combatants = new List<ServerCombatant> { actor, enemy };

        var action = _autoBattleManager.GetBestAction(actor, combatants, AutoBattlePolicy.Balanced);

        Assert.NotNull(action);
        Assert.Equal(3110, action.SkillId);
        Assert.Equal(enemy.Id, action.TargetId);
    }

    [Fact]
    public void GetBestAction_FallsBackToDamage_WhenNoStatusNeeds()
    {
        var actor = new WaterTestCharacter { Id = 1, Name = "Attacker", Team = Team.Player, Sp = 100, Con = 10, Hp = 100 };
        var enemy = new WaterTestCharacter { Id = 3, Name = "Enemy", Team = Team.Enemy, Con = 10, Hp = 100 };

        actor.LearnSkill(3010);

        var combatants = new List<ServerCombatant> { actor, enemy };

        var action = _autoBattleManager.GetBestAction(actor, combatants, AutoBattlePolicy.Aggressive);

        Assert.NotNull(action);
        Assert.Equal(3010, action.SkillId);
    }

    [Fact]
    public void GetBestAction_AvoidsSeal_IfTargetIsImmune()
    {
        var actor = new WaterTestCharacter { Id = 1, Name = "Mage", Team = Team.Player, Sp = 100, Con = 10, Hp = 100 };
        var enemy = new WaterTestCharacter { Id = 3, Name = "Boss", Team = Team.Enemy, Con = 100, Hp = 1000, Str = 100 };

        actor.LearnSkill(1210);
        actor.LearnSkill(3001);

        enemy.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.BuffStats, 1.0f, 10, "SealResist"), _statusEngine);

        var combatants = new List<ServerCombatant> { actor, enemy };

        var action = _autoBattleManager.GetBestAction(actor, combatants, AutoBattlePolicy.Balanced);

        Assert.NotNull(action);
        Assert.Equal(3001, action.SkillId);
    }
}
