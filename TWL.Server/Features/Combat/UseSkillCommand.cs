using TWL.Server.Architecture.Pipeline;
using TWL.Shared.Domain.Requests;

namespace TWL.Server.Features.Combat;

public class UseSkillCommand : ICommand<CombatResult>
{
    public UseSkillCommand(UseSkillRequest request)
    {
        Request = request;
    }

    public UseSkillRequest Request { get; }
}