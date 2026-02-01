using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Skills;

namespace TWL.Tests.Combat;

public class SkillLoadingTests
{
    private const string TestSkillsJson = @"[
      {
        ""SkillId"": 20,
        ""Name"": ""Fireball"",
        ""Description"": ""Throws a ball of fire."",
        ""Element"": ""Fire"",
        ""Branch"": ""Magical"",
        ""Tier"": 1,
        ""TargetType"": ""SingleEnemy"",
        ""SpCost"": 10,
        ""Cooldown"": 0,
        ""Scaling"": [
          { ""Stat"": ""Mat"", ""Coefficient"": 1.2 }
        ],
        ""Effects"": [
          { ""Tag"": ""Damage"", ""Value"": 0, ""Duration"": 0 }
        ],
        ""Requirements"": { ""Int"": 5 }
      }
    ]";

    [Fact]
    public void LoadSkills_ShouldPopulateRegistry()
    {
        // Act
        SkillRegistry.Instance.LoadSkills(TestSkillsJson);

        // Assert
        var skill = SkillRegistry.Instance.GetSkillById(20);
        Assert.NotNull(skill);
        Assert.Equal("Fireball", skill.Name);
        Assert.Equal(10, skill.SpCost);
        Assert.Equal(SkillTargetType.SingleEnemy, skill.TargetType);
    }

    [Fact]
    public void BattleInstance_ShouldUseLoadedSkill()
    {
        // Arrange
        SkillRegistry.Instance.LoadSkills(TestSkillsJson);

        var player = new PlayerCharacter(Guid.NewGuid(), "Hero", Element.Fire);
        player.Int = 10;
        player.Sp = 50; // Enough SP

        var enemy = new EnemyCharacter("Slime", Element.Earth, false);
        enemy.Health = 100;
        enemy.MaxHealth = 100;
        enemy.Con = 5;

        var battle = new BattleInstance(new[] { player }, new[] { enemy });

        // Advance time to give player turn
        player.Agi = 999; // Make player fast
        battle.Tick(1.0f);

        var combatantPlayer = battle.CurrentTurnCombatant;
        Assert.NotNull(combatantPlayer);
        Assert.Equal(player.Name, combatantPlayer.Character.Name);

        var enemyCombatant = battle.Enemies.First();

        // Act
        // Use Skill 20 (Fireball)
        var action = CombatAction.UseSkill(combatantPlayer.BattleId, enemyCombatant.BattleId, 20);
        var result = battle.ResolveAction(action);

        // Assert
        Assert.Contains("uses Fireball", result);
        Assert.True(enemy.Health < 100, "Enemy should take damage");
    }
}