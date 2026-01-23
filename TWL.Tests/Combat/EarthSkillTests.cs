using System.Collections.Generic;
using System.Linq;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Skills;
using Xunit;

namespace TWL.Tests.Combat;

public class EarthTestCharacter : Character
{
    public EarthTestCharacter(string name, Element element = Element.Earth) : base(name, element) { }
}

public class EarthSkillTests
{
    private const string EarthSkillsJson = @"
[
  {
    ""SkillId"": 60,
    ""Name"": ""Rock Smash"",
    ""Description"": ""A heavy strike with a solid rock."",
    ""Element"": ""Earth"",
    ""Branch"": ""Physical"",
    ""Tier"": 1,
    ""TargetType"": ""SingleEnemy"",
    ""SpCost"": 8,
    ""Cooldown"": 0,
    ""Scaling"": [
      { ""Stat"": ""Atk"", ""Coefficient"": 1.2 },
      { ""Stat"": ""Str"", ""Coefficient"": 0.5 }
    ],
    ""Effects"": [
      { ""Tag"": ""Damage"", ""Value"": 0, ""Duration"": 0 }
    ],
    ""Requirements"": { ""Str"": 5 },
    ""Stage"": 1,
    ""UnlockRules"": { ""Level"": 1 }
  },
  {
    ""SkillId"": 62,
    ""Name"": ""Earth Barrier"",
    ""Description"": ""Surrounds an ally with floating stones, increasing defense."",
    ""Element"": ""Earth"",
    ""Branch"": ""Support"",
    ""Tier"": 1,
    ""TargetType"": ""SingleAlly"",
    ""SpCost"": 15,
    ""Cooldown"": 0,
    ""Scaling"": [
      { ""Stat"": ""Wis"", ""Coefficient"": 1.5 }
    ],
    ""Effects"": [
      { ""Tag"": ""BuffStats"", ""Value"": 0, ""Duration"": 3, ""Param"": ""Def"" }
    ],
    ""Requirements"": { ""Wis"": 5 },
    ""Stage"": 1,
    ""UnlockRules"": { ""Level"": 1 }
  }
]
";

    public EarthSkillTests()
    {
        // Initialize Registry with test skills
        SkillRegistry.Instance.LoadSkills(EarthSkillsJson);
    }

    [Fact]
    public void RockSmash_ShouldDealDamage_BasedOnStrAndAtk()
    {
        var attacker = new EarthTestCharacter("EarthUser", Element.Earth) { Str = 20, Sp = 100, Agi = 10 };
        // Atk = 20*2 = 40.
        var defender = new EarthTestCharacter("Target", Element.Water) { Con = 5, Health = 100, MaxHealth = 100, Agi = 10 };
        // Def = 5*2 = 10.

        var battle = new BattleInstance(new[] { attacker }, new[] { defender });

        // Force turn
        battle.Tick(100f);
        var actor = battle.CurrentTurnCombatant;

        // Ensure we got the attacker
        if (actor.Character != attacker)
        {
            // If speed matches, queue order depends on iteration. But we set attacker Agi=10, defender Agi=10.
            // Wait, Spd is Agi.
            // Let's make Attacker faster to ensure turn.
            attacker.Agi = 20;
            battle.Tick(100f); // Re-tick if needed or reset
            actor = battle.CurrentTurnCombatant;
        }

        var action = new CombatAction
        {
            Type = CombatActionType.Skill,
            ActorId = actor.BattleId,
            TargetId = battle.Enemies[0].BattleId,
            SkillId = 60
        };

        var result = battle.ResolveAction(action);

        Assert.Contains("uses Rock Smash", result);
        Assert.True(defender.Health < 100);
    }

    [Fact]
    public void EarthBarrier_ShouldIncreaseDefense_AndReduceDamage()
    {
        // Setup
        var attacker = new EarthTestCharacter("Attacker", Element.Earth) { Str = 20, Sp = 100, Agi = 10 };
        var defender = new EarthTestCharacter("Defender", Element.Water) { Con = 0, Wis = 20, Health = 1000, MaxHealth = 1000, Sp = 100, Agi = 20 }; // Faster

        var battle = new BattleInstance(new[] { attacker }, new[] { defender });

        // Defender Turn (Faster)
        // Defender Speed 20 -> Rate 150/s. Attacker Speed 10 -> Rate 100/s.
        // Tick 0.8s: Defender 120 (ready), Attacker 80 (not ready).
        battle.Tick(0.8f);
        var defCombatant = battle.CurrentTurnCombatant;
        Assert.Equal("Defender", defCombatant.Character.Name);

        // Cast Earth Barrier (62) on Self
        var buffAction = new CombatAction
        {
            Type = CombatActionType.Skill,
            ActorId = defCombatant.BattleId,
            TargetId = defCombatant.BattleId,
            SkillId = 62
        };
        battle.ResolveAction(buffAction);

        // Verify Buff
        Assert.Contains(defCombatant.StatusEffects, e => e.Tag == SkillEffectTag.BuffStats && e.Param == "Def");
        // Buff Value should be Wis(20) * 1.5 = 30
        var buff = defCombatant.StatusEffects.First(e => e.Tag == SkillEffectTag.BuffStats);
        Assert.Equal(30, buff.Value);

        // Attacker Turn
        battle.Tick(100f);
        var atkCombatant = battle.CurrentTurnCombatant;
        Assert.Equal("Attacker", atkCombatant.Character.Name);

        // Attack normally
        var attackAction = new CombatAction
        {
            Type = CombatActionType.Attack,
            ActorId = atkCombatant.BattleId,
            TargetId = defCombatant.BattleId
        };

        battle.ResolveAction(attackAction);

        int damageDealt = 1000 - defender.Health;

        // Unbuffed comparison
        var attacker2 = new EarthTestCharacter("Attacker2", Element.Earth) { Str = 20, Agi = 20 };
        var defender2 = new EarthTestCharacter("Defender2", Element.Water) { Con = 0, Health = 1000, MaxHealth = 1000, Agi = 10 };
        var battle2 = new BattleInstance(new[] { attacker2 }, new[] { defender2 });
        battle2.Tick(100f);

        battle2.ResolveAction(new CombatAction
        {
            Type = CombatActionType.Attack,
            ActorId = battle2.Allies[0].BattleId,
            TargetId = battle2.Enemies[0].BattleId
        });
        int damageUnbuffed = 1000 - defender2.Health;

        Assert.True(damageDealt < damageUnbuffed, $"Buffed Damage ({damageDealt}) should be less than Unbuffed ({damageUnbuffed})");
    }

    [Fact]
    public void Seal_ShouldPreventAction()
    {
        var attacker = new EarthTestCharacter("SealedGuy", Element.Earth) { Agi = 20 };
        var defender = new EarthTestCharacter("Target", Element.Water) { Agi = 10 };
        var battle = new BattleInstance(new[] { attacker }, new[] { defender });

        battle.Tick(100f);
        var actor = battle.CurrentTurnCombatant;

        // Apply Seal manually
        actor.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.Seal, 0, 1));

        var action = new CombatAction
        {
            Type = CombatActionType.Attack,
            ActorId = actor.BattleId,
            TargetId = battle.Enemies[0].BattleId
        };

        var result = battle.ResolveAction(action);

        Assert.Contains("sealed", result);
        Assert.Null(battle.CurrentTurnCombatant); // Turn ended
        Assert.Equal(0, actor.Atb);
    }
}
