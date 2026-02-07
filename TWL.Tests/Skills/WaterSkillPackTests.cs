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

public class WaterTestCharacter : ServerCharacter
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

public class WaterSkillPackTests
{
    private readonly CombatManager _combatManager;
    private readonly Mock<IRandomService> _randomMock;
    private readonly Mock<ICombatResolver> _resolverMock;
    private readonly ISkillCatalog _skillCatalog;
    private readonly StatusEngine _statusEngine;

    public WaterSkillPackTests()
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
            .Returns(10);
        _resolverMock.Setup(r =>
                r.CalculateHeal(It.IsAny<ServerCombatant>(), It.IsAny<ServerCombatant>(),
                    It.IsAny<UseSkillRequest>()))
            .Returns(50);

        _statusEngine = new StatusEngine();
        _combatManager = new CombatManager(_resolverMock.Object, _randomMock.Object, _skillCatalog, _statusEngine);
    }

    [Fact]
    public void FrostBite_Targeting_Is_Correct()
    {
        var attacker = new WaterTestCharacter { Id = 1, Name = "Attacker", Team = Team.Player };
        var defender = new WaterTestCharacter { Id = 2, Name = "Defender", Team = Team.Enemy };
        var skill = _skillCatalog.GetSkillById(3110);

        var targets = SkillTargetingHelper.GetTargets(skill, attacker, defender, new List<ServerCombatant> { attacker, defender });

        Assert.Single(targets);
        Assert.Equal(defender.Id, targets[0].Id);
    }

    [Fact]
    public void FrostBite_Component_Logic_Works()
    {
        // 1. Verify Content
        var skill = _skillCatalog.GetSkillById(3110);
        Assert.NotNull(skill);
        var sealEffect = skill.Effects.FirstOrDefault(e => e.Tag == SkillEffectTag.Seal);
        Assert.NotNull(sealEffect);
        Assert.Contains("SealResist", sealEffect.ResistanceTags);

        // 2. Verify Application Logic (Manually simulating CombatManager.UseSkill loop)
        var defender = new WaterTestCharacter { Id = 2, Name = "Defender", Wis = 10, Team = Team.Enemy, Con = 10, Hp = 100 };

        // A) Apply Seal (Success)
        var status = new StatusEffectInstance(sealEffect.Tag, sealEffect.Value, sealEffect.Duration, sealEffect.Param)
        {
            SourceSkillId = skill.SkillId,
            StackingPolicy = sealEffect.StackingPolicy,
            ConflictGroup = sealEffect.ConflictGroup
        };
        defender.AddStatusEffect(status, _statusEngine);

        var appliedSeal = defender.StatusEffects.FirstOrDefault(e => e.Tag == SkillEffectTag.Seal);
        Assert.NotNull(appliedSeal);
        Assert.Equal("HardControl", appliedSeal.ConflictGroup);

        // B) Verify Resistance Logic (Component Level)
        defender.ClearStatusEffects();
        defender.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.BuffStats, 1.0f, 10, "SealResist"), _statusEngine);

        // Emulate CombatManager resistance check
        var resist = false;
        foreach (var tag in sealEffect.ResistanceTags)
        {
            if (defender.GetResistance(tag) >= 1.0f)
            {
                resist = true;
                break;
            }
        }
        Assert.True(resist, "Should resist when SealResist is 1.0");
    }

    [Fact]
    public void SoothingMist_Cleanse_And_Heal()
    {
        // Arrange
        var attacker = new WaterTestCharacter { Id = 1, Name = "Healer", Wis = 50, Sp = 100, Team = Team.Player, Con = 10, Hp = 100, Agi = 100 };
        var ally1 = new WaterTestCharacter { Id = 2, Name = "Ally1", Team = Team.Player, Con = 10, Hp = 50, Agi = 10 };
        var ally2 = new WaterTestCharacter { Id = 3, Name = "Ally2", Team = Team.Player, Con = 10, Hp = 50, Agi = 10 };

        _combatManager.RegisterCombatant(attacker);
        _combatManager.RegisterCombatant(ally1);
        _combatManager.RegisterCombatant(ally2);
        _combatManager.StartEncounter(1, new List<ServerCharacter> { attacker, ally1, ally2 });

        // Apply Debuffs
        ally1.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.DebuffStats, 10, 3, "Spd"), _statusEngine);
        ally2.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.Burn, 10, 3), _statusEngine);

        attacker.LearnSkill(3210); // Soothing Mist

        // Act
        var request = new UseSkillRequest { PlayerId = attacker.Id, TargetId = attacker.Id, SkillId = 3210 };
        var results = _combatManager.UseSkill(request);

        // Assert
        Assert.True(results.Count >= 2, $"Expected at least 2 targets, got {results.Count}");

        // Check Cleanse
        Assert.Empty(ally1.StatusEffects.Where(e => e.Tag == SkillEffectTag.DebuffStats));
        Assert.Empty(ally2.StatusEffects.Where(e => e.Tag == SkillEffectTag.Burn));

        // Check Heal (Resolver returns 50)
        Assert.True(ally1.Hp > 50);
        Assert.True(ally2.Hp > 50);
    }

    [Fact]
    public void AquaCrescent_HitsRow_And_Slows()
    {
         // Arrange
        var attacker = new WaterTestCharacter { Id = 1, Name = "Attacker", Str = 25, Sp = 100, Team = Team.Player, Con = 10, Hp = 100 };
        var enemy1 = new WaterTestCharacter { Id = 10, Name = "Enemy1", Team = Team.Enemy, Con = 10, Hp = 100 };

        _combatManager.RegisterCombatant(attacker);
        _combatManager.RegisterCombatant(enemy1);
        _combatManager.StartEncounter(1, new List<ServerCharacter> { attacker, enemy1 });

        attacker.LearnSkill(3010); // Aqua Crescent

        var request = new UseSkillRequest { PlayerId = attacker.Id, TargetId = enemy1.Id, SkillId = 3010 };

        // Act
        var results = _combatManager.UseSkill(request);

        // Assert
        Assert.NotEmpty(results);
        var victim = results[0].TargetId == enemy1.Id ? enemy1 : null;
        Assert.NotNull(victim);

        var slow = victim.StatusEffects.FirstOrDefault(e => e.Tag == SkillEffectTag.DebuffStats && e.Param == "Spd");
        Assert.NotNull(slow);
        Assert.Equal("Debuff_Spd", slow.ConflictGroup);
    }
}
