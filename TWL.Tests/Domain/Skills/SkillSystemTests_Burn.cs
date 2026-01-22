using Xunit;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Skills;

namespace TWL.Tests.Domain.Skills;

public class SkillSystemTests_Burn
{
    private class TestCharacter : Character
    {
        public TestCharacter(string name, Element element) : base(name, element)
        {
        }
    }

    [Fact]
    public void TestBurnEffect_AppliedAndProcesses()
    {
        string json = @"[
          {
            ""SkillId"": 900, ""Name"": ""Burner"", ""SpCost"": 10, ""Element"": ""Fire"", ""Branch"": ""Magical"", ""TargetType"": ""SingleEnemy"",
            ""Effects"": [ { ""Tag"": ""Burn"", ""Value"": 10, ""Duration"": 2, ""Chance"": 1.0 } ]
          }
        ]";
        SkillRegistry.Instance.LoadSkills(json);

        var caster = new TestCharacter("Caster", Element.Fire) { Sp = 20 };
        var target = new TestCharacter("Target", Element.Wind) { Health = 100 };

        var battle = new BattleInstance(new[] { caster }, new[] { target });
        battle.Tick(2.0f);

        var action = new CombatAction
        {
            Type = CombatActionType.Skill,
            ActorId = battle.CurrentTurnCombatant.BattleId,
            TargetId = battle.Enemies[0].BattleId,
            SkillId = 900
        };

        // 1. Cast Skill
        string result = battle.ResolveAction(action);

        // Verify Burn Applied
        var targetCombatant = battle.Enemies[0];
        Assert.Single(targetCombatant.StatusEffects);
        Assert.Equal(SkillEffectTag.Burn, targetCombatant.StatusEffects[0].Tag);

        // 2. Process Turn End (Simulate)
        string log = battle.ProcessStatusEffects(targetCombatant);

        Assert.Contains("takes 10 burn damage", log);
        Assert.Equal(90, target.Health);
        Assert.Equal(1, targetCombatant.StatusEffects[0].TurnsRemaining);

        // Process again
        log = battle.ProcessStatusEffects(targetCombatant);
        Assert.Contains("takes 10 burn damage", log);
        Assert.Equal(80, target.Health);
        Assert.Contains("wore off", log);
        Assert.Empty(targetCombatant.StatusEffects);
    }
}
