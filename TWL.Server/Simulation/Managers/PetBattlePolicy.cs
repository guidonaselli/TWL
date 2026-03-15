using Microsoft.Extensions.Logging;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Services;
using TWL.Shared.Domain.Skills;

namespace TWL.Server.Simulation.Managers;

public class PetBattlePolicy : IPetBattlePolicy
{
    private readonly AutoBattleManager _autoBattle;
    private readonly ILogger<PetBattlePolicy> _logger;

    public PetBattlePolicy(AutoBattleManager autoBattle, ILogger<PetBattlePolicy> logger)
    {
        _autoBattle = autoBattle;
        _logger = logger;
    }

    public UseSkillRequest? GetAction(
        ServerCombatant pet,
        IEnumerable<ServerCombatant> participants,
        IRandomService random)
    {
        // Pet AI should be intelligent. We'll use "Balanced" policy by default for now,
        // but we can refine it based on pet traits or owner settings in the future.
        var action = _autoBattle.GetBestAction(pet, participants, AutoBattlePolicy.Balanced, random);

        if (action != null)
        {
            _logger.LogDebug("Pet {PetId} AI Decision: Use Skill {SkillId} on Target {TargetId}",
                pet.Id, action.SkillId, action.TargetId);
        }
        else
        {
            _logger.LogWarning("Pet {PetId} AI could not determine an action.", pet.Id);
        }

        return action;
    }
}
