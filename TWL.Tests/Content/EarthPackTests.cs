using System.Text.Json;
using System.Text.Json.Serialization;
using TWL.Server.Features.Combat;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Domain.Skills;
using TWL.Tests.Mocks;

namespace TWL.Tests.Content;

public class EarthPackTests
{
    private void LoadSkills()
    {
        var contentRoot = GetContentRoot();
        var skillsPath = Path.Combine(contentRoot, "Content/Data/skills.json");
        var json = File.ReadAllText(skillsPath);

        SkillRegistry.Instance.LoadSkills(json);
    }

    private string GetContentRoot()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var current = new DirectoryInfo(baseDir);
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, "Content/Data");
            if (Directory.Exists(candidate))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return "../../../../";
    }

    private CombatManager CreateCombatManager()
    {
        var random = new MockRandomService(0.5f); // No variance
        var catalog = SkillRegistry.Instance;
        var resolver = new StandardCombatResolver(random, catalog);
        var statusEngine = new StatusEngine();
        return new CombatManager(resolver, random, catalog, statusEngine);
    }

    [Fact]
    public void RockSmash_ShouldDealPhysicalDamage()
    {
        LoadSkills();
        var manager = CreateCombatManager();

        var player = new ServerCharacter
        {
            Id = 1,
            Name = "Hero",
            CharacterElement = Element.Earth,
            Str = 25, // Atk ~ 50
            Sp = 100,
            Team = Team.Player
        };
        // Learn Rock Smash I
        player.LearnSkill(1001);

        var enemy = new ServerCharacter
        {
            Id = 2,
            Name = "Enemy",
            CharacterElement = Element.Water, // Weak to Earth (1.5x)
            Con = 5, // Def ~ 10
            Team = Team.Enemy
        };
        enemy.Hp = 500; // Hp is virtual, can set

        manager.AddCharacter(player);
        manager.AddCharacter(enemy);

        // Act: Use Rock Smash I
        var request = new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = 1001 };
        var results = manager.UseSkill(request);

        // Assert
        Assert.NotEmpty(results);
        Assert.Equal(2, results[0].TargetId);

        // Damage check: Atk=50 * 1.2 = 60. * 1.5 = 90. - Def(10) = 80.
        // Assuming StandardCombatResolver follows this.
        // MockRandom is 0.5, so if variance exists, it is mean value.
        Assert.True(results[0].Damage > 50, $"Damage {results[0].Damage} should be > 50");
        Assert.True(enemy.Hp < 500);
    }

    [Fact]
    public void StoneBullet_ShouldDealMagicalDamage()
    {
        LoadSkills();
        var manager = CreateCombatManager();

        var player = new ServerCharacter
        {
            Id = 1,
            Name = "Mage",
            CharacterElement = Element.Earth,
            Int = 25, // Mat ~ 50
            Sp = 100,
            Team = Team.Player
        };
        player.LearnSkill(1101); // Stone Bullet I

        var enemy = new ServerCharacter
        {
            Id = 2,
            Name = "Enemy",
            CharacterElement = Element.Water,
            Con = 5,
            Wis = 5, // Mdf ~ 10
            Team = Team.Enemy
        };
        enemy.Hp = 500;

        manager.AddCharacter(player);
        manager.AddCharacter(enemy);

        var request = new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = 1101 };
        var results = manager.UseSkill(request);

        Assert.NotEmpty(results);
        Assert.True(results[0].Damage > 50);
        Assert.True(enemy.Hp < 500);
    }

    [Fact]
    public void GaiasProtection_ShouldApplyBuff_AndRefreshDuration()
    {
        LoadSkills();
        var manager = CreateCombatManager();

        var player = new ServerCharacter
        {
            Id = 1,
            Name = "Supporter",
            CharacterElement = Element.Earth,
            Wis = 25,
            Sp = 100,
            Team = Team.Player
        };
        player.LearnSkill(1201); // Gaia's Protection I (Buff Def +10, 3 turns)

        manager.AddCharacter(player);

        // Act 1: Cast
        var request1 = new UseSkillRequest { PlayerId = 1, TargetId = 1, SkillId = 1201 };
        manager.UseSkill(request1);

        Assert.Single(player.StatusEffects);
        var buff = player.StatusEffects.First();
        Assert.Equal("BuffStats", buff.Tag.ToString());
        Assert.Equal("Def", buff.Param);
        Assert.Equal(3, buff.TurnsRemaining); // Use TurnsRemaining
        Assert.Equal(10, buff.Value);

        // Advance: Simulate turn (decrement duration)
        buff.TurnsRemaining--;
        Assert.Equal(2, buff.TurnsRemaining);

        // Act 2: Cast Again
        // Clear cooldown manually by ticking
        // Cooldown is 2. Tick twice.
        player.TickCooldowns();
        player.TickCooldowns();

        manager.UseSkill(request1);

        // Assert Refresh
        Assert.Single(player.StatusEffects);
        var refreshedBuff = player.StatusEffects.First();
        Assert.Equal(3, refreshedBuff.TurnsRemaining); // Refreshed to max
        Assert.Equal(10, refreshedBuff.Value);
    }
}
