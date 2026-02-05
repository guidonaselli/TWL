using TWL.Server.Features.Combat;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Domain.Skills;
using TWL.Tests.Mocks;

namespace TWL.Tests.Features.Combat;

public class UseSkillHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCallCombatManager()
    {
        if (SkillRegistry.Instance.GetSkillById(1001) == null)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Content/Data/skills.json");
            if (!File.Exists(path))
            {
                // Try looking from project root (if running from root)
                path = Path.Combine(AppContext.BaseDirectory, "Content/Data/skills.json");
            }

            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                SkillRegistry.Instance.LoadSkills(json);
            }
            else
            {
                // Fallback: Manually register the skill needed for test
                var manualJson =
                    "[{\"SkillId\":1001,\"Name\":\"Rock Smash\",\"SpCost\":5,\"Scaling\":[{\"Stat\":\"Atk\",\"Coefficient\":1.2}],\"Effects\":[{\"Tag\":\"Damage\"}]}]";
                SkillRegistry.Instance.LoadSkills(manualJson);
            }
        }

        var mockRng = new MockRandomService(0.5f);
        var resolver = new StandardCombatResolver(mockRng, SkillRegistry.Instance);
        var combatManager = new CombatManager(resolver, mockRng, SkillRegistry.Instance, new StatusEngine());

        var handler = new UseSkillHandler(combatManager);

        var attacker = new ServerCharacter { Id = 1, Name = "P1", Sp = 100, Str = 10 };
        var target = new ServerCharacter { Id = 2, Name = "P2", Hp = 100, Con = 5, Team = Team.Enemy };

        combatManager.AddCharacter(attacker);
        combatManager.AddCharacter(target);

        var request = new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = 1001 };
        var command = new UseSkillCommand(request);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result);
        var hit = result[0];
        Assert.Equal(1, hit.AttackerId);
        Assert.Equal(2, hit.TargetId);
    }
}