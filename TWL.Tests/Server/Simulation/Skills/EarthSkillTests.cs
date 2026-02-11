using Moq;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Services;
using Xunit;

namespace TWL.Tests.Server.Simulation.Skills;

public class EarthSkillTests
{
    private class TestCombatant : ServerCombatant
    {
        public TestCombatant()
        {
            SkillMastery = new System.Collections.Concurrent.ConcurrentDictionary<int, SkillMastery>();
        }
        public override void ReplaceSkill(int oldId, int newId) { }
    }

    private readonly Mock<ICombatResolver> _resolverMock;
    private readonly Mock<IRandomService> _randomMock;
    private readonly Mock<ISkillCatalog> _skillsMock;
    private readonly IStatusEngine _statusEngine;
    private readonly CombatManager _combatManager;

    public EarthSkillTests()
    {
        _resolverMock = new Mock<ICombatResolver>();
        _randomMock = new Mock<IRandomService>();
        _skillsMock = new Mock<ISkillCatalog>();
        _statusEngine = new StatusEngine();
        _combatManager = new CombatManager(_resolverMock.Object, _randomMock.Object, _skillsMock.Object, _statusEngine);

        // Default: always hit (EffectChance), always obey
        _randomMock.Setup(r => r.NextFloat("EffectChance")).Returns(0.0f);
        _randomMock.Setup(r => r.NextFloat("PetObedience")).Returns(0.0f);
        _resolverMock.Setup(r => r.CalculateDamage(It.IsAny<ServerCombatant>(), It.IsAny<ServerCombatant>(), It.IsAny<UseSkillRequest>())).Returns(100);
    }

    private void SetupEarthSkills()
    {
        // 1001: Rock Smash (Seal, Priority 1)
        var rockSmash = new Skill
        {
            SkillId = 1001,
            Name = "Rock Smash",
            Effects = new List<SkillEffect>
            {
                new SkillEffect { Tag = SkillEffectTag.Damage, Value = 0 },
                new SkillEffect {
                    Tag = SkillEffectTag.Seal,
                    Duration = 1,
                    Chance = 1.0f,
                    StackingPolicy = StackingPolicy.NoStackOverwrite, // Changed to NoStackOverwrite for Priority logic
                    ConflictGroup = "HardControl",
                    Priority = 1,
                    ResistanceTags = new List<string> { "SealResist" },
                    Outcome = OutcomeModel.Resist
                }
            },
            TargetType = SkillTargetType.SingleEnemy,
            SpCost = 5,
            Cooldown = 0
        };

        // 1201: Gaia's Protection (Buff Def, Buff Resist, Priority 1)
        var gaias = new Skill
        {
            SkillId = 1201,
            Name = "Gaia's Protection",
            Effects = new List<SkillEffect>
            {
                new SkillEffect {
                    Tag = SkillEffectTag.BuffStats, Param = "Def", Value = 10, Duration = 3,
                    StackingPolicy = StackingPolicy.RefreshDuration, ConflictGroup = "Buff_Def", Priority = 1
                },
                new SkillEffect {
                    Tag = SkillEffectTag.BuffStats, Param = "SealResist", Value = 0.5f, Duration = 3, // 50% resist
                    StackingPolicy = StackingPolicy.RefreshDuration, ConflictGroup = "Buff_Resist", Priority = 1
                }
            },
            TargetType = SkillTargetType.SingleAlly,
            SpCost = 10,
            Cooldown = 0
        };

        // 1210: Entangle (Strong Seal, Priority 2)
        var entangle = new Skill
        {
            SkillId = 1210,
            Name = "Entangle",
            Effects = new List<SkillEffect>
            {
                new SkillEffect {
                    Tag = SkillEffectTag.Seal,
                    Duration = 2,
                    Chance = 1.0f,
                    StackingPolicy = StackingPolicy.NoStackOverwrite, // Changed to NoStackOverwrite for Priority logic
                    ConflictGroup = "HardControl",
                    Priority = 2,
                    ResistanceTags = new List<string> { "SealResist" },
                    Outcome = OutcomeModel.Resist
                }
            },
            TargetType = SkillTargetType.SingleEnemy,
            SpCost = 20,
            Cooldown = 0
        };

        _skillsMock.Setup(s => s.GetSkillById(1001)).Returns(rockSmash);
        _skillsMock.Setup(s => s.GetSkillById(1201)).Returns(gaias);
        _skillsMock.Setup(s => s.GetSkillById(1210)).Returns(entangle);
    }

    [Fact]
    public void RockSmash_AppliesSeal()
    {
        SetupEarthSkills();
        var attacker = new TestCombatant { Id = 1, Sp = 100, Team = Team.Player };
        var target = new TestCombatant { Id = 2, Hp = 100, Team = Team.Enemy };
        _combatManager.RegisterCombatant(attacker);
        _combatManager.RegisterCombatant(target);

        var request = new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = 1001 };
        _combatManager.UseSkill(request);

        Assert.Contains(target.StatusEffects, e => e.Tag == SkillEffectTag.Seal);
    }

    [Fact]
    public void GaiasProtection_AppliesBuffs_AndRefreshLogic()
    {
        SetupEarthSkills();
        var caster = new TestCombatant { Id = 1, Sp = 100, Team = Team.Player };
        var target = new TestCombatant { Id = 2, Hp = 100, Team = Team.Player }; // Ally
        _combatManager.RegisterCombatant(caster);
        _combatManager.RegisterCombatant(target);

        // Cast 1
        _combatManager.UseSkill(new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = 1201 });
        var defBuff = target.StatusEffects.First(e => e.ConflictGroup == "Buff_Def");
        Assert.Equal(3, defBuff.TurnsRemaining);
        Assert.Equal(10, defBuff.Value);

        // Tick 1 turn (Simulate)
        defBuff.TurnsRemaining--;

        // Cast 2 (Refresh)
        _combatManager.UseSkill(new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = 1201 });
        defBuff = target.StatusEffects.First(e => e.ConflictGroup == "Buff_Def");
        Assert.Equal(3, defBuff.TurnsRemaining); // Should be refreshed to 3
    }

    [Fact]
    public void Entangle_Overwrites_RockSmashSeal()
    {
        SetupEarthSkills();
        var attacker = new TestCombatant { Id = 1, Sp = 100, Team = Team.Player };
        var target = new TestCombatant { Id = 2, Hp = 100, Team = Team.Enemy };
        _combatManager.RegisterCombatant(attacker);
        _combatManager.RegisterCombatant(target);

        // Apply Rock Smash (Weak Seal, Priority 1)
        _combatManager.UseSkill(new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = 1001 });
        var seal = target.StatusEffects.First(e => e.Tag == SkillEffectTag.Seal);
        Assert.Equal(1, seal.Priority);

        // Apply Entangle (Strong Seal, Priority 2)
        _combatManager.UseSkill(new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = 1210 });
        seal = target.StatusEffects.First(e => e.Tag == SkillEffectTag.Seal);
        Assert.Equal(2, seal.Priority);
    }

    [Fact]
    public void RockSmash_FailsToOverwrite_Entangle()
    {
        SetupEarthSkills();
        var attacker = new TestCombatant { Id = 1, Sp = 100, Team = Team.Player };
        var target = new TestCombatant { Id = 2, Hp = 100, Team = Team.Enemy };
        _combatManager.RegisterCombatant(attacker);
        _combatManager.RegisterCombatant(target);

        // Apply Entangle (Strong Seal, Priority 2)
        _combatManager.UseSkill(new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = 1210 });

        // Try Rock Smash (Weak Seal, Priority 1)
        _combatManager.UseSkill(new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = 1001 });

        var seal = target.StatusEffects.First(e => e.Tag == SkillEffectTag.Seal);
        Assert.Equal(2, seal.Priority); // Should remain Strong Seal
    }

    [Fact]
    public void GaiasProtection_IncreasesResistance_AndBlocksSeal()
    {
        SetupEarthSkills();
        var attacker = new TestCombatant { Id = 1, Sp = 100, Team = Team.Enemy };
        var target = new TestCombatant { Id = 2, Hp = 100, Team = Team.Player };
        _combatManager.RegisterCombatant(attacker);
        _combatManager.RegisterCombatant(target);

        // Apply Gaia's Protection (+0.5 SealResist)
        // Note: Caster is Enemy (Id 1), Target is Player (Id 2). Gaia is Ally skill.
        // Caster must be ally of target.
        // Wait, here Attacker is Enemy and Target is Player.
        // We want Player to cast Gaia on themselves (or Ally).
        // Let's make Target cast Gaia on Self.
        var gaiaRequest = new UseSkillRequest { PlayerId = 2, TargetId = 2, SkillId = 1201 };

        // We need target (Id 2) to have SP.
        target.Sp = 100;

        _combatManager.UseSkill(gaiaRequest);

        Assert.Contains(target.StatusEffects, e => e.Param == "SealResist");

        // Mock RNG for Resistance Roll
        // SealResist is 0.5. Random roll needs to be < 0.5 to resist.
        // We set NextFloat("ResistanceRoll") to 0.4 (Success resist)
        _randomMock.Setup(r => r.NextFloat("ResistanceRoll")).Returns(0.4f);

        // Try Rock Smash (from Enemy Id 1 to Player Id 2)
        _combatManager.UseSkill(new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = 1001 });

        // Verify NO Seal
        Assert.DoesNotContain(target.StatusEffects, e => e.Tag == SkillEffectTag.Seal);
    }
}
