using System.Collections.Concurrent;
using TWL.Server.Simulation.Networking;

namespace TWL.Server.Services.Combat;

public record DeathPenaltyResult(bool Applied, int ExpLost, int PreviousExp, int NewExp, bool WasDuplicate);

public class DeathPenaltyService
{
    private readonly ConcurrentDictionary<string, bool> _processedDeaths = new();

    public DeathPenaltyResult ApplyExpPenalty(ServerCharacter character, string deathEventId)
    {
        if (string.IsNullOrWhiteSpace(deathEventId))
        {
            throw new ArgumentException("Death event ID cannot be null or empty", nameof(deathEventId));
        }

        if (!_processedDeaths.TryAdd(deathEventId, true))
        {
            return new DeathPenaltyResult(false, 0, character.Exp, character.Exp, true);
        }

        lock (character.ProgressLock)
        {
            int previousExp = character.Exp;
            int expLost = (int)Math.Floor(previousExp * 0.01);
            int newExp = Math.Max(0, previousExp - expLost);

            character.Exp = newExp;

            return new DeathPenaltyResult(true, previousExp - newExp, previousExp, newExp, false);
        }
    }
}
