using System;
using System.Text.Json;
using System.IO;
using Microsoft.Extensions.Logging;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.DTO;
using TWL.Shared.Domain.Models;

namespace TWL.Server.Simulation.Managers;

public class RebirthManager : IRebirthService
{
    private readonly ILogger<RebirthManager> _logger;
    private RebirthRequirements _requirements = new();

    public RebirthManager(ILogger<RebirthManager> logger)
    {
        _logger = logger;
    }

    public void SetRequirements(RebirthRequirements requirements)
    {
        _requirements = requirements;
    }

    public void LoadRequirements(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                _requirements = JsonSerializer.Deserialize<RebirthRequirements>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new RebirthRequirements();
                _logger.LogInformation("Rebirth requirements loaded from {Path}", path);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load rebirth requirements from {Path}", path);
        }
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
        return TryRebirthCharacter(character, null, operationId);
    }

    public (bool Success, string Message, int StatPointsGained) TryRebirthCharacter(ServerCharacter character, PlayerQuestComponent? questComponent, string operationId)
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
        if (character.Level < _requirements.MinLevel)
        {
            LogAndRecordFailure(character, operationId, $"Character must be level {_requirements.MinLevel} or higher to rebirth.");
            return (false, $"Level {_requirements.MinLevel} required.", 0);
        }

        if (_requirements.RequiredQuestId.HasValue)
        {
            if (questComponent == null)
            {
                LogAndRecordFailure(character, operationId, "Quest component missing for prerequisite check.");
                return (false, "Internal error: Quest data unavailable.", 0);
            }

            if (!questComponent.QuestStates.TryGetValue(_requirements.RequiredQuestId.Value, out var state) || 
                (state != TWL.Shared.Domain.Requests.QuestState.Completed && state != TWL.Shared.Domain.Requests.QuestState.RewardClaimed))
            {
                LogAndRecordFailure(character, operationId, $"Required quest {_requirements.RequiredQuestId} not completed.");
                return (false, "Required quest not completed.", 0);
            }
        }

        if (_requirements.RequiredItemId.HasValue)
        {
            if (!character.HasItem(_requirements.RequiredItemId.Value, _requirements.RequiredItemQuantity))
            {
                LogAndRecordFailure(character, operationId, $"Missing required item(s): {_requirements.RequiredItemId} x{_requirements.RequiredItemQuantity}");
                return (false, "Required item(s) missing.", 0);
            }
<<<<<<< HEAD
        }

        // 1.1 Quest Flag Check
        if (!character.QuestComponent.Flags.Contains("REBIRTH_QUALIFIED"))
        {
            LogAndRecordFailure(character, operationId, "Character missing 'REBIRTH_QUALIFIED' flag.");
            return (false, "Rebirth quest not completed.", 0);
        }

        // 1.2 Item Check (Core Resonance Shard - 9007)
        if (!character.HasItem(9007, 1))
        {
            LogAndRecordFailure(character, operationId, "Character missing Core Resonance Shard (9007).");
            return (false, "Core Resonance Shard (9007) required.", 0);
=======
>>>>>>> gsd/M001/S06
        }

        // Avoid concurrent rebirth processing for the same character
        lock (character.ProgressLock)
        {
            // Double-check eligibility inside lock
<<<<<<< HEAD
            if (character.Level < _requirements.MinLevel || !character.QuestComponent.Flags.Contains("REBIRTH_QUALIFIED") || !character.HasItem(9007, 1))
            {
                LogAndRecordFailure(character, operationId, "Race condition or missing requirements prevented rebirth.");
                return (false, "Requirements not met.", 0);
=======
            if (character.Level < _requirements.MinLevel)
            {
                LogAndRecordFailure(character, operationId, $"Race condition prevented rebirth (Level < {_requirements.MinLevel}).");
                return (false, $"Level {_requirements.MinLevel} required.", 0);
>>>>>>> gsd/M001/S06
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
                character.ExpToNextLevel = 100;
                
                // Grant unassigned stat points
                character.StatPoints += bonusPoints;

<<<<<<< HEAD
                // Reset stats to baseline
                character.ResetStatsToBaseline();

=======
>>>>>>> gsd/M001/S06
                // Consume required items if any
                if (_requirements.RequiredItemId.HasValue && _requirements.RequiredItemQuantity > 0)
                {
                    if (!character.RemoveItem(_requirements.RequiredItemId.Value, _requirements.RequiredItemQuantity))
                    {
                        // This should theoretically not happen because of the check before the lock,
                        // but if it does, we should throw to trigger rollback.
                        throw new InvalidOperationException("Failed to consume rebirth items during atomic transaction.");
                    }
                }
<<<<<<< HEAD
                else
                {
                    // Fallback to legacy item if no data-driven requirement is set but we are here
                    character.TryConsumeItem(9007, 1);
                }
=======
>>>>>>> gsd/M001/S06

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

                // Also remove the last history record if it was added
                if (character.RebirthHistory != null && character.RebirthHistory.Count > 0 && character.RebirthHistory.Last().OperationId == operationId)
                {
                    character.RebirthHistory.RemoveAt(character.RebirthHistory.Count - 1);
                }

                _logger.LogError(ex, "Transaction failure during Rebirth for Character {Name} ({Id}). State rolled back.", character.Name, character.Id);
                LogAndRecordFailure(character, operationId, $"Transaction failure: {ex.Message}");

                return (false, "Internal error occurred during rebirth.", 0);
            }
        }
    }

    private void LogAndRecordFailure(ServerCharacter character, string operationId, string reason)
    {
        _logger.LogWarning("Rebirth failed for {Name} ({Id}): {Reason}", character.Name, character.Id, reason);

        // Record failure in history for auditability, but with strict capping to prevent DoS
        if (character.RebirthHistory == null) return;

        lock (character.ProgressLock)
        {
            character.RebirthHistory.Add(new RebirthHistoryRecord
            {
                OperationId = operationId,
                CharacterId = character.Id,
                OldLevel = character.Level,
                NewLevel = character.Level,
                OldRebirthCount = character.RebirthLevel,
                NewRebirthCount = character.RebirthLevel,
                StatPointsGranted = 0,
                TimestampUtc = DateTime.UtcNow,
                Success = false,
                Reason = reason
            });

            if (character.RebirthHistory.Count > 10)
            {
                character.RebirthHistory.RemoveAt(0);
            }
        }
    }
}
