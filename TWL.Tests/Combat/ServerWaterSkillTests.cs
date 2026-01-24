using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Services;
using Xunit;
using TWL.Shared.Domain.Requests;
using TWL.Tests.Mocks;
using System.IO;

namespace TWL.Tests.Combat;

public class ServerWaterSkillTests
{
    public ServerWaterSkillTests()
    {
        string path = "../../../TWL.Server/Content/Data/skills.json";
        if (!File.Exists(path))
        {
             path = "TWL.Server/Content/Data/skills.json";
        }

        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            SkillRegistry.Instance.LoadSkills(json);
        }
    }

    [Fact]
    public void AquaImpact_ShouldDealDamage()
    {
        if (SkillRegistry.Instance.GetSkillById(3001) == null) return;

        var mockRngMean = new MockRandomService(0.5f);

        var resolver = new StandardCombatResolver(mockRngMean, SkillRegistry.Instance);
        var manager = new CombatManager(resolver, mockRngMean);

        var attacker = new ServerCharacter { Id = 1, Name = "Attacker", Sp = 100, Str = 20, CharacterElement = TWL.Shared.Domain.Characters.Element.Water };
        // Atk = 40.
        // Skill scaling: 1.2 * Atk = 48.

        var target = new ServerCharacter { Id = 2, Name = "Target", Hp = 100, Con = 5, CharacterElement = TWL.Shared.Domain.Characters.Element.Water };
        // Def = 10.
        // Element same: 1.0x

        // Expected Damage = 48 * 1.0 - 10 = 38.

        manager.AddCharacter(attacker);
        manager.AddCharacter(target);

        var request = new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = 3001 };

        var result = manager.UseSkill(request);

        Assert.NotNull(result);
        Assert.Equal(38, result.Damage);
    }

    [Fact]
    public void AquaRecover_ShouldHeal_AndNotDealDamage()
    {
        if (SkillRegistry.Instance.GetSkillById(3201) == null) return;

        var mockRngMean = new MockRandomService(0.5f);
        var resolver = new StandardCombatResolver(mockRngMean, SkillRegistry.Instance);
        var manager = new CombatManager(resolver, mockRngMean);

        var healer = new ServerCharacter { Id = 1, Name = "Healer", Sp = 100, Wis = 20, CharacterElement = TWL.Shared.Domain.Characters.Element.Water };
        // Wis = 20.
        // Scaling: 2.5 * Wis = 50.
        // Mean variance (1.0).
        // Heal Amount = 50.

        var target = new ServerCharacter { Id = 2, Name = "Target", Hp = 50, Con = 10, CharacterElement = TWL.Shared.Domain.Characters.Element.Water };
        // MaxHp = Con * 10 = 100.
        // Current Hp = 50.

        manager.AddCharacter(healer);
        manager.AddCharacter(target);

        var request = new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = 3201 };

        var result = manager.UseSkill(request);

        Assert.NotNull(result);
        Assert.Equal(0, result.Damage); // Should not deal damage
        Assert.Equal(100, target.Hp); // 50 + 50 = 100.
    }
}
