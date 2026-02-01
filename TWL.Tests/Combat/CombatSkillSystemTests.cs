using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Skills;
using Xunit;

namespace TWL.Tests.Combat;

public class MockSkillCatalog2 : ISkillCatalog
{
    private Dictionary<int, Skill> _skills = new();

    public void AddSkill(Skill skill)
    {
        _skills[skill.SkillId] = skill;
    }

    public IEnumerable<int> GetAllSkillIds() => _skills.Keys;

    public Skill? GetSkillById(int id) => _skills.GetValueOrDefault(id);
}

public class TestCharacter2 : Character
{
    public TestCharacter2(string name, Element element = Element.Earth) : base(name, element)
    {
    }

    public void SetId(int id) => Id = id;
}

public class CombatSkillSystemTests
{
    private readonly MockSkillCatalog2 _mockCatalog;
    private readonly TestCharacter2 _actor;
    private readonly TestCharacter2 _target;

    public CombatSkillSystemTests()
    {
        _mockCatalog = new MockSkillCatalog2();
        _actor = new TestCharacter2("Actor", Element.Wind);
        _target = new TestCharacter2("Target", Element.Earth);

        // Init stats
        _actor.SetId(1);
        _actor.Health = 100; _actor.Sp = 100; _actor.Str = 10; _actor.Int = 10; _actor.Wis = 10; _actor.Agi = 20;

        _target.SetId(2);
        _target.Health = 100; _target.Sp = 100;
        _target.Con = 2; // Def = 4
        _target.Wis = 2; // Mdf = 4
    }

    [Fact]
    public void WindBlade_DealsPhysicalDamage_And_CalculatesSpeedScaling()
    {
        // Wind Blade: Atk * 1.0 + Spd * 0.5
        // Atk = Str * 2 = 20
        // Spd = Agi = 20
        // Base = 20 * 1.0 + 20 * 0.5 = 30
        // Target Def = 4
        // Expected Damage = 30 - 4 = 26

        var skill = new Skill
        {
            SkillId = 80,
            Name = "Wind Blade",
            Branch = SkillBranch.Physical,
            SpCost = 8,
            Scaling = new List<SkillScaling> {
                new() { Stat = StatType.Atk, Coefficient = 1.0f },
                new() { Stat = StatType.Spd, Coefficient = 0.5f }
            },
            Effects = new List<SkillEffect> { new() { Tag = SkillEffectTag.Damage } },
            TargetType = SkillTargetType.SingleEnemy
        };
        _mockCatalog.AddSkill(skill);

        var battle = new BattleInstance(new[] { _actor }, new[] { _target }, _mockCatalog);
        var actorC = battle.Allies[0];
        var targetC = battle.Enemies[0];

        // Force turn
        actorC.Atb = 100;
        battle.Tick(0.1f);

        var action = CombatAction.UseSkill(actorC.BattleId, targetC.BattleId, 80);
        var msg = battle.ResolveAction(action);

        // We verify the output message contains the damage value
        Assert.Contains("for 26!", msg); // "Actor uses Wind Blade on Target for 26!"
    }

    [Fact]
    public void Seal_PreventsAction()
    {
        var sealSkill = new Skill
        {
            SkillId = 100,
            Name = "Seal Skill",
            TargetType = SkillTargetType.SingleEnemy,
            Effects = new List<SkillEffect> {
                new() { Tag = SkillEffectTag.Seal, Duration = 2, Chance = 1.0f }
            }
        };
        _mockCatalog.AddSkill(sealSkill);

        var battle = new BattleInstance(new[] { _actor }, new[] { _target }, _mockCatalog);
        var actorC = battle.Allies[0];
        var targetC = battle.Enemies[0];

        // 1. Actor Seals Target
        actorC.Atb = 100;
        battle.Tick(0.1f);
        battle.ResolveAction(CombatAction.UseSkill(actorC.BattleId, targetC.BattleId, 100));

        Assert.Contains(targetC.StatusEffects, e => e.Tag == SkillEffectTag.Seal);

        // 2. Target Turn
        targetC.Atb = 100;
        actorC.Atb = 0;

        battle.Tick(0.1f); // Should pick target

        Assert.Equal(targetC, battle.CurrentTurnCombatant);

        // Target tries to attack
        var result = battle.ResolveAction(CombatAction.Attack(targetC.BattleId, actorC.BattleId));

        Assert.Contains("is sealed", result);
        Assert.Null(battle.CurrentTurnCombatant); // Turn ended
        Assert.Equal(0, targetC.Atb);
    }

    [Fact]
    public void Dispel_RemovesBuffs()
    {
        var dispelSkill = new Skill
        {
            SkillId = 101,
            Name = "Dispel",
            TargetType = SkillTargetType.SingleEnemy,
            Effects = new List<SkillEffect> { new() { Tag = SkillEffectTag.Dispel } }
        };
        _mockCatalog.AddSkill(dispelSkill);

        var battle = new BattleInstance(new[] { _actor }, new[] { _target }, _mockCatalog);
        var actorC = battle.Allies[0];
        var targetC = battle.Enemies[0];

        targetC.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.BuffStats, 10, 3, "Spd"));

        actorC.Atb = 100;
        battle.Tick(0.1f);
        battle.ResolveAction(CombatAction.UseSkill(actorC.BattleId, targetC.BattleId, 101));

        Assert.DoesNotContain(targetC.StatusEffects, e => e.Tag == SkillEffectTag.BuffStats);
    }

    [Fact]
    public void Mastery_EventFires()
    {
        var skill = new Skill
        {
            SkillId = 200,
            Name = "Test",
            TargetType = SkillTargetType.SingleEnemy,
            Effects = new List<SkillEffect> { new() { Tag = SkillEffectTag.Damage } }
        };
        _mockCatalog.AddSkill(skill);

        var battle = new BattleInstance(new[] { _actor }, new[] { _target }, _mockCatalog);
        var actorC = battle.Allies[0];
        var targetC = battle.Enemies[0];

        bool eventFired = false;
        battle.OnSkillUsed += (actorId, skillId) =>
        {
            if (actorId == actorC.Character.Id && skillId == 200) eventFired = true;
        };

        actorC.Atb = 100;
        battle.Tick(0.1f);
        battle.ResolveAction(CombatAction.UseSkill(actorC.BattleId, targetC.BattleId, 200));

        Assert.True(eventFired);
    }

    [Fact]
    public void Evolution_SwapsId_At_Threshold()
    {
        // Setup Skill with Upgrade Rule
        var skill1 = new Skill { SkillId = 80, Name = "Wind Blade I", StageUpgradeRules = new() { RankThreshold = 2, NextSkillId = 83 } };
        var skill2 = new Skill { SkillId = 83, Name = "Wind Blade II" };

        _mockCatalog.AddSkill(skill1);
        _mockCatalog.AddSkill(skill2);

        var sChar = new TWL.Server.Simulation.Networking.ServerCharacter();
        sChar.LearnSkill(80);

        // Simulate usage
        // Usage 1 -> Rank 1
        int rank = sChar.IncrementSkillUsage(80);
        Assert.Equal(1, rank);

        // Usage 10 -> Rank 2 (Logic is % 10 == 0)
        // Loop 9 more times to reach 10 usages
        for (int i = 0; i < 9; i++) rank = sChar.IncrementSkillUsage(80);

        Assert.Equal(2, rank);

        // Manager Logic Simulation: Check if evolution is needed
        if (rank >= skill1.StageUpgradeRules.RankThreshold)
        {
            if (skill1.StageUpgradeRules.NextSkillId.HasValue)
                sChar.ReplaceSkill(skill1.SkillId, skill1.StageUpgradeRules.NextSkillId.Value);
        }

        Assert.DoesNotContain(80, sChar.KnownSkills);
        Assert.Contains(83, sChar.KnownSkills);
    }
}
