using System.Threading;
using System.Threading.Tasks;
using TWL.Server.Features.Combat;
using TWL.Server.Simulation.Managers;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Services;
using TWL.Tests.Mocks;
using Xunit;

namespace TWL.Tests.Features.Combat;

public class UseSkillHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCallCombatManager()
    {
        if (SkillRegistry.Instance.GetSkillById(1001) == null)
        {
             string path = System.IO.Path.Combine(System.AppContext.BaseDirectory, "Content/Data/skills.json");
             if (!System.IO.File.Exists(path))
             {
                 // Try looking from project root (if running from root)
                 path = System.IO.Path.Combine(System.AppContext.BaseDirectory, "Content/Data/skills.json");
             }

             if (System.IO.File.Exists(path))
             {
                 var json = System.IO.File.ReadAllText(path);
                 SkillRegistry.Instance.LoadSkills(json);
             }
             else
             {
                 // Fallback: Manually register the skill needed for test
                 var manualJson = "[{\"SkillId\":1001,\"Name\":\"Rock Smash\",\"SpCost\":5,\"Scaling\":[{\"Stat\":\"Atk\",\"Coefficient\":1.2}],\"Effects\":[{\"Tag\":\"Damage\"}]}]";
                 SkillRegistry.Instance.LoadSkills(manualJson);
             }
        }

        var mockRng = new MockRandomService(0.5f);
        var resolver = new StandardCombatResolver(mockRng, SkillRegistry.Instance);
        var combatManager = new CombatManager(resolver, mockRng, SkillRegistry.Instance, new StatusEngine());

        var handler = new UseSkillHandler(combatManager);

        var attacker = new TWL.Server.Simulation.Networking.ServerCharacter { Id = 1, Name = "P1", Sp = 100, Str = 10 };
        var target = new TWL.Server.Simulation.Networking.ServerCharacter { Id = 2, Name = "P2", Hp = 100, Con = 5 };

        combatManager.AddCharacter(attacker);
        combatManager.AddCharacter(target);

        var request = new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = 1001 };
        var command = new UseSkillCommand(request);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result.AttackerId);
        Assert.Equal(2, result.TargetId);
    }
}
