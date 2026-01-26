using Xunit;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Skills;

namespace TWL.Tests.Domain.Skills;

public class SkillSystemTests
{
    private class TestCharacter : Character
    {
        public TestCharacter(string name, Element element) : base(name, element)
        {
        }
    }

    [Fact]
    public void TestFireballSkill_LoadedFromJson_AndExecuted()
    {
        // 1. Setup Skill Registry
        string json = @"[
          {
            ""SkillId"": 999,
            ""Name"": ""Test Fireball"",
            ""Element"": ""Fire"",
            ""Branch"": ""Magical"",
            ""Tier"": 1,
            ""TargetType"": ""SingleEnemy"",
            ""SpCost"": 10,
            ""Scaling"": [
              { ""Stat"": ""Mat"", ""Coefficient"": 2.0 }
            ],
            ""Effects"": [
              { ""Tag"": ""Damage"", ""Value"": 0 }
            ]
          }
        ]";

        SkillRegistry.Instance.LoadSkills(json);

        // 2. Setup Characters
        var caster = new TestCharacter("Caster", Element.Fire) { Int = 20, Sp = 50 }; // MAT = 40
        var target = new TestCharacter("Target", Element.Wind) { Con = 5 }; // DEF = 10, MDF = 10 (Wis=5)

        var allies = new List<Character> { caster };
        var enemies = new List<Character> { target };

        var battle = new BattleInstance(allies, enemies);

        // 3. Execute Skill
        // We need to bypass the tick system and force execution for unit testing

        // Force turn for Caster logic:
        // Since BattleInstance tick logic waits for ATB, we can just manually set CurrentTurnCombatant if internal,
        // but it is private set.
        // We can simulate ticks until ready.

        // Fill ATB
        // Tick logic: (10 + Spd) * 5 * deltaTime
        // Caster Spd = 5. Rate = 15 * 5 = 75 per sec.
        // 2 seconds should be enough.
        battle.Tick(2.0f);

        // If caster is the only one in queue, they should be active.
        Assert.Equal(caster, battle.CurrentTurnCombatant?.Character);

        var action = new CombatAction
        {
            Type = CombatActionType.Skill,
            ActorId = battle.CurrentTurnCombatant.BattleId,
            TargetId = battle.Enemies[0].BattleId,
            SkillId = 999
        };

        string result = battle.ResolveAction(action);

        // 4. Verification
        // Damage = (Mat * 2.0) - Mdf
        // Mat = 20 * 2 = 40. Scaling = 40 * 2.0 = 80.
        // Element Multiplier (Fire vs Wind) = 1.5x
        // Adjusted Value = 80 * 1.5 = 120.
        // Target Wis = 5 (default). Mdf = 5 * 2 = 10.
        // Expected Damage = 120 - 10 = 110.

        Assert.Contains("uses Test Fireball on Target for 110", result);
        Assert.Equal(0, target.Health); // Clamped to 0
        Assert.Equal(40, caster.Sp); // 50 - 10
    }

    [Fact]
    public void TestSkillSpCost_NotEnoughSp()
    {
        string json = @"[
          { ""SkillId"": 998, ""Name"": ""High Cost"", ""SpCost"": 100, ""TargetType"": ""SingleEnemy"", ""Element"": ""Fire"", ""Branch"": ""Physical"", ""Effects"": [ { ""Tag"": ""Damage"" } ] }
        ]";
        SkillRegistry.Instance.LoadSkills(json);

        var caster = new TestCharacter("Caster", Element.Fire) { Sp = 10 };
        var target = new TestCharacter("Target", Element.Wind);

        var battle = new BattleInstance(new[] { caster }, new[] { target });

        // Force ATB
        battle.Tick(2.0f);

        var action = new CombatAction
        {
            Type = CombatActionType.Skill,
            ActorId = battle.CurrentTurnCombatant.BattleId,
            TargetId = battle.Enemies[0].BattleId,
            SkillId = 998
        };

        string result = battle.ResolveAction(action);

        Assert.Equal("Not enough SP!", result);
        Assert.Equal(10, caster.Sp); // SP not consumed
    }
}
