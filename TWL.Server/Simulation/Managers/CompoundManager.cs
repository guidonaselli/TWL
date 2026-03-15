using TWL.Shared.Domain.DTO;
using TWL.Server.Simulation.Networking;
using Microsoft.Extensions.Logging;
using TWL.Shared.Services;
using System.Linq;
using TWL.Shared.Domain.Models;

namespace TWL.Server.Simulation.Managers;

public enum CompoundResult
{
    Success,
    Failure,
    InvalidItems
}

public class CompoundManager : ICompoundService
{
    private readonly ILogger<CompoundManager> _logger;
    private readonly ICompoundRatePolicy _compoundRatePolicy;
    private readonly IRandomService _randomService;

    public CompoundManager(ILogger<CompoundManager> logger, ICompoundRatePolicy compoundRatePolicy, IRandomService randomService)
    {
        _logger = logger;
        _compoundRatePolicy = compoundRatePolicy;
        _randomService = randomService;
    }

    public async Task<CompoundResponseDTO> ProcessCompoundRequest(ServerCharacter character, CompoundRequestDTO request)
    {
        _logger.LogInformation("CompoundOperationStarted: Player {PlayerId} attempting compound on {TargetItemId} with material {MaterialItemId}", 
            character.Id, request.TargetItemId, request.IngredientItemId);

        var inventory = character.Inventory;
        var targetItem = inventory.FirstOrDefault(i => i.InstanceId == request.TargetItemId);
        var materialItem = inventory.FirstOrDefault(i => i.InstanceId == request.IngredientItemId);

        if (targetItem == null || materialItem == null)
        {
            _logger.LogWarning("CompoundOperationCompleted: Player {PlayerId} - Invalid items specified.", character.Id);
            return new CompoundResponseDTO { Success = false, Message = "Invalid items specified.", Outcome = CompoundOutcome.Fail };
        }
        
        // CMP-03: "Success chance is calculated server-side"
        var successChance = _compoundRatePolicy.GetSuccessChance(targetItem, materialItem);
        _logger.LogInformation("CompoundSuccessRateCalculated: Player {PlayerId} has {SuccessChance:P2} chance for item {TargetItemId}", character.Id, successChance, targetItem.ItemId);
        
        var roll = _randomService.NextDouble("CompoundRoll");

        // The material is always consumed, regardless of outcome, as per CMP-05.
        if (!character.RemoveItemByInstanceId(materialItem.InstanceId, 1))
        {
            _logger.LogError("CompoundIntegrityError: Player {PlayerId} - Material item {MaterialItemId} could not be removed.", character.Id, materialItem.InstanceId);
            return new CompoundResponseDTO { Success = false, Message = "An internal error occurred.", Outcome = CompoundOutcome.Fail };
        }

        if (roll < successChance)
        {
            // CMP-04: "Successful compound attempts apply permanent enhancement bonuses"
            if (!character.EnhanceItem(targetItem.InstanceId, 1))
            {
                _logger.LogError("CompoundIntegrityError: Player {PlayerId} - Target item {TargetItemId} could not be enhanced.", character.Id, targetItem.InstanceId);
                // CRITICAL: At this point, the material has been consumed but the enhancement failed. This should be handled.
                // For now, we return an error. A more robust system might try to refund the material.
                return new CompoundResponseDTO { Success = false, Message = "An internal error occurred during enhancement.", Outcome = CompoundOutcome.Fail };
            }
            
            // We need to get the updated item details for the response message.
            var updatedItem = character.Inventory.FirstOrDefault(i => i.InstanceId == targetItem.InstanceId);
            var newEnhancementLevel = updatedItem?.EnhancementLevel ?? targetItem.EnhancementLevel + 1;

            _logger.LogInformation("CompoundAttemptSuccess: Player {PlayerId} successfully compounded {TargetItemId} to +{EnhancementLevel}", 
                character.Id, targetItem.ItemId, newEnhancementLevel);

            return new CompoundResponseDTO
            {
                Success = true,
                Message = $"Compound successful! Your {targetItem.Name} is now +{newEnhancementLevel}.",
                Outcome = CompoundOutcome.Success,
                NewEnhancementLevel = newEnhancementLevel
            };
        }
        else
        {
            // CMP-05: "Failed attempts consume materials but preserve the base equipment item"
            // The material was already removed above. We just need to log and notify the user.

            _logger.LogWarning("CompoundAttemptFailure: Player {PlayerId} failed to compound {TargetItemId}. Material consumed.", 
                character.Id, targetItem.ItemId);
            
            return new CompoundResponseDTO
            {
                Success = false,
                Message = "Compound failed. The material was consumed.",
                Outcome = CompoundOutcome.Fail
            };
        }
    }
}
