using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Services;

namespace TWL.Server.Simulation.Managers;

public interface IPetBattlePolicy
{
    UseSkillRequest? GetAction(
        ServerCombatant pet,
        IEnumerable<ServerCombatant> participants,
        IRandomService random);
}
