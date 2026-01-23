using System;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Services;

namespace TWL.Server.Simulation.Managers;

public class StandardCombatResolver : ICombatResolver
{
    private readonly IRandomService _random;

    public StandardCombatResolver(IRandomService random)
    {
        _random = random;
    }

    public int CalculateDamage(ServerCharacter attacker, ServerCharacter target, UseSkillRequest request)
    {
        // Simple damage formula: Str * 2
        var baseDamage = attacker.Str * 2;

        // Variance +/- 5%
        float variance = _random.NextFloat(0.95f, 1.05f);
        int finalDamage = (int)Math.Round(baseDamage * variance);

        return finalDamage;
    }
}
