using System.Reflection;
using Moq;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Services;

namespace TWL.Tests.Skills;

public class TestServerCharacter : ServerCharacter
{
    public void TickStatus(IStatusEngine engine)
    {
        // Access protected _statusEffects via subclass
        // But _statusEffects is List<StatusEffectInstance>, and engine.Tick takes IList.
        // List implements IList.
        // We need to lock as well.
        // Since we can't access _statusLock (it's protected? yes), we can use it.
        // Wait, checking ServerCombatant.cs: protected readonly object _statusLock = new();
        // Yes, it is protected.

        // Reflection might be needed if I can't access _statusLock directly from subclass if it was private, but it is protected.
        // Let's assume it is accessible.
        // Wait, I saw it was protected in ServerCombatant.cs

        // Actually, check ServerCombatant.cs again.
        // protected readonly List<StatusEffectInstance> _statusEffects = new();
        // protected readonly object _statusLock = new();
        // Yes.

        lock (_statusLock)
        {
            engine.Tick(_statusEffects);
        }
    }
}

public class EarthSkillPackTests
{
    private readonly CombatManager _combatManager;
    private readonly Mock<IRandomService> _randomMock;
    private readonly Mock<ICombatResolver> _resolverMock;
    private readonly ISkillCatalog _skillCatalog;
    private readonly StatusEngine _statusEngine;

    public EarthSkillPackTests()
    {
        // Load actual skills.json
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Content/Data/skills.json"));

        // Use reflection to create a new instance of SkillRegistry to avoid Singleton pollution if possible,
        // or just use Instance. Instance is shared.
        // Let's use Instance for now as it's easier, assuming tests run sequentially or don't conflict too much.
        // Or better, reset it? No clear reset method.

        // Let's use reflection to instantiate private constructor.
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
            .Returns(10); // Fixed damage

        _statusEngine = new StatusEngine();
        _combatManager = new CombatManager(_resolverMock.Object, _randomMock.Object, _skillCatalog, _statusEngine);
    }

    [Fact]
    public void RockSmash_Evolution_Chain_Works()
    {
        // Arrange
        var attacker = new TestServerCharacter { Id = 1, Name = "Attacker" };
        var defender = new TestServerCharacter { Id = 2, Name = "Defender", Con = 10 }; // MaxHp = 100
        _combatManager.RegisterCombatant(attacker);
        _combatManager.RegisterCombatant(defender);

        // Learn Rock Smash I (1001)
        attacker.LearnSkill(1001);
        attacker.Sp = 100; // Ensure enough SP
        attacker.SkillMastery[1001] = new SkillMastery { Rank = 5 }; // Nearly Rank 6

        // Act - Use skill to reach Rank 6
        // Simulate evolution trigger manually since UseSkill doesn't auto-rankup in this mocked context (IncrementSkillUsage does, but we set Rank=5)
        // CombatManager.UseSkill calls IncrementSkillUsage.
        // If UsageCount % 10 == 0 -> Rank++.
        // We set Rank=5, UsageCount=0.
        // We need UsageCount to hit 10 to rank up?
        // No, CheckSkillEvolution checks Rank.
        // If I set Rank=6 immediately, then UseSkill will see Rank 6 and evolve.

        attacker.SkillMastery[1001].Rank = 6;

        var request = new UseSkillRequest { PlayerId = attacker.Id, TargetId = defender.Id, SkillId = 1001 };
        _combatManager.UseSkill(request);

        // Assert - Should have evolved to 1002
        Assert.DoesNotContain(1001, attacker.KnownSkills);
        Assert.Contains(1002, attacker.KnownSkills);

        // Now test 1002 -> 1003
        attacker.SkillMastery[1002] = new SkillMastery { Rank = 12 };

        var request2 = new UseSkillRequest { PlayerId = attacker.Id, TargetId = defender.Id, SkillId = 1002 };
        _combatManager.UseSkill(request2);

        Assert.DoesNotContain(1002, attacker.KnownSkills);
        Assert.Contains(1003, attacker.KnownSkills);
    }

    [Fact]
    public void GaiasProtection_StackingPolicy_RefreshDuration()
    {
        // Arrange
        var attacker = new TestServerCharacter { Id = 1, Name = "Attacker", Wis = 20, Sp = 100 };
        var ally = new TestServerCharacter { Id = 2, Name = "Ally" };
        _combatManager.RegisterCombatant(attacker);
        _combatManager.RegisterCombatant(ally);

        attacker.LearnSkill(1201); // Gaia's Protection I

        // Act - Cast once
        var request = new UseSkillRequest { PlayerId = attacker.Id, TargetId = ally.Id, SkillId = 1201 };
        _combatManager.UseSkill(request);

        var buff = ally.StatusEffects.FirstOrDefault(e => e.Tag == SkillEffectTag.BuffStats && e.Param == "Def");
        Assert.NotNull(buff);
        Assert.Equal("Buff_Def", buff.ConflictGroup); // Verify metadata
        Assert.Equal(StackingPolicy.RefreshDuration, buff.StackingPolicy);
        Assert.Equal(3, buff.TurnsRemaining); // Duration is 3

        // Simulate 1 turn pass
        ally.TickStatus(_statusEngine);
        buff = ally.StatusEffects.FirstOrDefault(e => e.Tag == SkillEffectTag.BuffStats && e.Param == "Def");
        Assert.Equal(2, buff.TurnsRemaining);

        // Reset cooldown to allow casting again
        attacker.TickCooldowns(); // Reduce 2 -> 1
        attacker.TickCooldowns(); // Reduce 1 -> 0 (Removed)

        // Cast again
        var result = _combatManager.UseSkill(request);
        Assert.NotNull(result); // Ensure skill executed
        Assert.NotEmpty(result[0].AddedEffects); // Ensure effect applied

        // Assert - Should refresh to 3
        buff = ally.StatusEffects.FirstOrDefault(e => e.Tag == SkillEffectTag.BuffStats && e.Param == "Def");
        Assert.Equal(3, buff.TurnsRemaining);
        // Should NOT have 2 buffs
        Assert.Single(ally.StatusEffects.Where(e => e.Tag == SkillEffectTag.BuffStats && e.Param == "Def"));
    }

    [Fact]
    public void Entangle_Outcome_Resist()
    {
        // Arrange
        var attacker = new TestServerCharacter { Id = 1, Name = "Attacker", Int = 10, Sp = 100 };
        var defender = new TestServerCharacter { Id = 2, Name = "Defender", Wis = 10 };
        _combatManager.RegisterCombatant(attacker);
        _combatManager.RegisterCombatant(defender);

        attacker.LearnSkill(1210); // Entangle

        // Force resist via immunity
        defender.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.BuffStats, 1.0f, 10, "SealResist"),
            _statusEngine);

        // Act
        var request = new UseSkillRequest { PlayerId = attacker.Id, TargetId = defender.Id, SkillId = 1210 };
        var result = _combatManager.UseSkill(request);

        // Assert
        // Check if Seal was applied
        var seal = defender.StatusEffects.FirstOrDefault(e => e.Tag == SkillEffectTag.Seal);
        Assert.Null(seal); // Should be resisted
    }
}