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

        if (roll < successChance)
        {
            // CMP-04: "Successful compound attempts apply permanent enhancement bonuses"
            targetItem.EnhancementLevel++;
            inventory.Remove(materialItem);
            
            _logger.LogInformation("CompoundAttemptSuccess: Player {PlayerId} successfully compounded {TargetItemId} to +{EnhancementLevel}", 
                character.Id, targetItem.ItemId, targetItem.EnhancementLevel);

            return new CompoundResponseDTO
            {
                Success = true,
                Message = $"Compound successful! Your {targetItem.Name} is now +{targetItem.EnhancementLevel}.",
                Outcome = CompoundOutcome.Success,
                NewEnhancementLevel = targetItem.EnhancementLevel
            };
        }
        else
        {
            // CMP-05: "Failed attempts consume materials but preserve the base equipment item"
            inventory.Remove(materialItem);

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
