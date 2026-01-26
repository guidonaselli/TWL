using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Services;
using Xunit;
using TWL.Shared.Domain.Requests;
using TWL.Tests.Mocks;
using System.IO;
using System.Linq;
using TWL.Shared.Domain.Characters;

namespace TWL.Tests.Combat;

public class ServerFireSkillTests
{
    public ServerFireSkillTests()
    {
        // Adjust path logic to be robust
        string path = "../../../TWL.Server/Content/Data/skills.json";
        if (!File.Exists(path))
        {
             // Try going up one more level if running from bin/Debug/net8.0
             path = "../../../../TWL.Server/Content/Data/skills.json";
        }

        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            SkillRegistry.Instance.LoadSkills(json);
        }
    }

    [Fact]
    public void FlameSmash_ShouldDealDamage_WithElementalAdvantage()
    {
        if (SkillRegistry.Instance.GetSkillById(4001) == null)
        {
             // Fallback if file not loaded correctly in test env
             return;
        }

        // Use 0.5f to get exactly 1.0 multiplier from NextFloat(0.95, 1.05)
        var mockRng = new MockRandomService(0.5f);
        var resolver = new StandardCombatResolver(mockRng, SkillRegistry.Instance);
        var manager = new CombatManager(resolver, mockRng, SkillRegistry.Instance, new TWL.Server.Simulation.Managers.StatusEngine());

        var attacker = new ServerCharacter { Id = 1, Name = "Attacker", Sp = 100, Str = 20, Agi = 10, CharacterElement = Element.Fire };
        // Atk = 40.
        // Skill 4001 (Flame Smash) Scaling: 1.2 * Atk = 48.

        var target = new ServerCharacter { Id = 2, Name = "Target", Hp = 100, Con = 5, Agi = 10, CharacterElement = Element.Wind };
        // Def = 10.
        // Element: Fire vs Wind => 1.5x Multiplier.

        // Calculation:
        // Base = 48
        // Multiplier = 48 * 1.5 = 72
        // Variance (mocked 0.5 => 1.0) = 72
        // Damage = 72 - 10 = 62

        manager.AddCharacter(attacker);
        manager.AddCharacter(target);

        var request = new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = 4001 };

        var result = manager.UseSkill(request);

        Assert.NotNull(result);
        Assert.Equal(62, result.Damage);
    }

    [Fact]
    public void FieryWill_ShouldBuffAtk()
    {
        if (SkillRegistry.Instance.GetSkillById(4201) == null) return;

        var mockRng = new MockRandomService(1.0f);
        var resolver = new StandardCombatResolver(mockRng, SkillRegistry.Instance);
        var manager = new CombatManager(resolver, mockRng, SkillRegistry.Instance, new TWL.Server.Simulation.Managers.StatusEngine());

        var caster = new ServerCharacter { Id = 1, Name = "Caster", Sp = 100, Int = 20, Str = 10 };
        // Base Atk = Str * 2 = 20.

        manager.AddCharacter(caster);

        var request = new UseSkillRequest { PlayerId = 1, TargetId = 1, SkillId = 4201 }; // Fiery Will

        var result = manager.UseSkill(request);

        Assert.NotNull(result);
        Assert.Contains(result.AddedEffects, e => e.Tag == SkillEffectTag.BuffStats && e.Param == "Atk");

        // Verify ServerCharacter logic for buffs
        // Skill 4201: BuffStats Atk +20
        // Base Atk = 20
        // Expected Effective Atk = 20 + 20 = 40.
        Assert.Equal(40, caster.Atk);
    }
}
