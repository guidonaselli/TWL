using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Services;
using Xunit;
using TWL.Shared.Domain.Requests;
using TWL.Tests.Mocks;
using System.IO;

namespace TWL.Tests.Combat;

public class ServerEarthSkillTests
{
    public ServerEarthSkillTests()
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
    public void RockSmash_ShouldDealDamage()
    {
        if (SkillRegistry.Instance.GetSkillById(1001) == null) return;

        var mockRng = new MockRandomService(1.0f);
        var resolver = new StandardCombatResolver(mockRng, SkillRegistry.Instance);
        var manager = new CombatManager(resolver, mockRng, TWL.Shared.Domain.Skills.SkillRegistry.Instance, new TWL.Server.Simulation.Managers.StatusEngine());

        var attacker = new ServerCharacter { Id = 1, Name = "Attacker", Sp = 100, Str = 20, Agi = 10 };
        // Atk = 40.
        // Skill scaling: 1.2 * Atk = 48.

        var target = new ServerCharacter { Id = 2, Name = "Target", Hp = 100, Con = 5, Agi = 10 };
        // Def = 10.

        // Expected Damage = 48 - 10 = 38.

        manager.AddCharacter(attacker);
        manager.AddCharacter(target);

        var request = new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = 1001 }; // Rock Smash

        var result = manager.UseSkill(request);

        Assert.NotNull(result);
        Assert.Equal(38, result.Damage);
    }
}
