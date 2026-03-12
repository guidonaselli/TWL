using System;
using Microsoft.Extensions.Logging;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.DTO;

namespace TWL.Server.Simulation.Managers;

public class RebirthManager : IRebirthService
{
    private readonly ILogger<RebirthManager> _logger;

    public RebirthManager(ILogger<RebirthManager> logger)
    {
        _logger = logger;
    }

    public int GetDiminishingReturnsBonus(int currentRebirthCount)
    {
        // currentRebirthCount: 0 -> rebirth 1 (+20)
        // currentRebirthCount: 1 -> rebirth 2 (+15)
        // currentRebirthCount: 2 -> rebirth 3 (+10)
        // currentRebirthCount: 3+ -> rebirth 4+ (+5)
        return currentRebirthCount switch
        {
            0 => 20,
            1 => 15,
            2 => 10,
            _ => 5
        };
    }

    public (bool Success, string Message, int StatPointsGained) TryRebirthCharacter(ServerCharacter character, string operationId)
    {
        if (character == null)
        {
            return (false, "Character not found.", 0);
        }

        if (string.IsNullOrWhiteSpace(operationId))
        {
            operationId = Guid.NewGuid().ToString();
        }

        // 1. Eligibility Check
        if (character.Level < 100)
        {
            LogAndRecordFailure(character, operationId, "Character must be level 100 or higher to rebirth.");
            return (false, "Level 100 required.", 0);
        }

        // Avoid concurrent rebirth processing for the same character
        lock (character.ProgressLock)
        {
            // Double-check eligibility inside lock
            if (character.Level < 100)
            {
                LogAndRecordFailure(character, operationId, "Race condition prevented rebirth (Level < 100).");
                return (false, "Level 100 required.", 0);
            }

            int oldLevel = character.Level;
            int oldRebirthCount = character.RebirthLevel;
            int bonusPoints = GetDiminishingReturnsBonus(oldRebirthCount);

            try
            {
                // Atomic State Mutation
                character.Level = 1;
                character.RebirthLevel = oldRebirthCount + 1;
                character.Exp = 0; // Reset Exp
                // Next level exp calculation should ideally come from a central Exp table,
                // but setting it to a default starting value.
                character.ExpToNextLevel = 100;

                // Grant unassigned stat points
                character.StatPoints += bonusPoints;

                // Add History Record
                var historyRecord = new RebirthHistoryRecord
                {
                    OperationId = operationId,
                    CharacterId = character.Id,
                    OldLevel = oldLevel,
                    NewLevel = 1,
                    OldRebirthCount = oldRebirthCount,
                    NewRebirthCount = character.RebirthLevel,
                    StatPointsGranted = bonusPoints,
                    TimestampUtc = DateTime.UtcNow,
                    Success = true,
                    Reason = "Success"
                };

                character.RebirthHistory.Add(historyRecord);

                // Cap the history size to prevent unbounded growth
                if (character.RebirthHistory.Count > 10)
                {
                    character.RebirthHistory.RemoveAt(0);
                }

                character.IsDirty = true;

                _logger.LogInformation("Character {Name} ({Id}) rebirth {RebirthCount} successful. Granted {Points} stat points.",
                    character.Name, character.Id, character.RebirthLevel, bonusPoints);

                return (true, "Rebirth successful!", bonusPoints);
            }
            catch (Exception ex)
            {
                // Rollback state in case of unexpected failure
                character.Level = oldLevel;
                character.RebirthLevel = oldRebirthCount;
                character.StatPoints -= bonusPoints;

                _logger.LogError(ex, "Transaction failure during Rebirth for Character {Name} ({Id}). State rolled back.", character.Name, character.Id);
                LogAndRecordFailure(character, operationId, $"Transaction failure: {ex.Message}");

                return (false, "Internal error occurred during rebirth.", 0);
            }
        }
    }

    private void LogAndRecordFailure(ServerCharacter character, string operationId, string reason)
    {
        _logger.LogWarning("Rebirth failed for {Name} ({Id}): {Reason}", character.Name, character.Id, reason);
        // We do not append failures to RebirthHistory to prevent DoS via save file bloat
    }
}
