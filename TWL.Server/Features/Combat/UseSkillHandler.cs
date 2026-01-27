using System.Threading;
using System.Threading.Tasks;
using TWL.Server.Architecture.Pipeline;
using TWL.Server.Simulation.Managers;
using TWL.Shared.Domain.Requests;

namespace TWL.Server.Features.Combat;

public class UseSkillHandler : ICommandHandler<UseSkillCommand, CombatResult>
{
    private readonly CombatManager _combatManager;

    public UseSkillHandler(CombatManager combatManager)
    {
        _combatManager = combatManager;
    }

    public Task<CombatResult> Handle(UseSkillCommand command, CancellationToken cancellationToken)
    {
        var result = _combatManager.UseSkill(command.Request);
        return Task.FromResult(result);
    }
}
