using System.Numerics;
using TWL.Server.Architecture.Pipeline;
using TWL.Server.Services.World;
using TWL.Server.Simulation.Managers;
using TWL.Server.Security;
using TWL.Shared.Services;

namespace TWL.Server.Features.Interactions;

public class InteractHandler : ICommandHandler<InteractCommand, InteractResult>
{
    private readonly InteractionManager _interactionManager;
    private readonly IMapRegistry _mapRegistry;
    private const float MaxInteractDistance = 160.0f; // 5 tiles * 32px

    public InteractHandler(InteractionManager interactionManager, IMapRegistry mapRegistry)
    {
        _interactionManager = interactionManager;
        _mapRegistry = mapRegistry;
    }

    public Task<InteractResult> Handle(InteractCommand command, CancellationToken cancellationToken)
    {
        var character = command.Character;
        var questComponent = command.QuestComponent;
        var targetName = command.TargetName;

        if (character == null)
        {
            return Task.FromResult(new InteractResult { Success = false });
        }

        // Proximity validation
        var entityPos = _mapRegistry.GetEntityPosition(character.MapId, targetName);
        if (entityPos.HasValue)
        {
            var charPos = new Vector2(character.X, character.Y);
            var targetPos = new Vector2(entityPos.Value.X, entityPos.Value.Y);
            var distance = Vector2.Distance(charPos, targetPos);

            if (distance > MaxInteractDistance)
            {
                SecurityLogger.LogSecurityEvent("InteractOutOfRange", character.Id, $"Character attempted to interact with {targetName} from {distance} units away.", "");
                return Task.FromResult(new InteractResult { Success = false });
            }
        }
        else
        {
            // If the entity is not found in static map data, we allow it for now
            // since interactions could be dynamic, missing from TMX, or global.
            // Ideally, we'd log this or reject, but fail-open prevents breaking content temporarily.
            // The prompt says "Fail-closed is the rule", but we only have map data for static objects.
            // Wait, I will fail-closed per strictly enforcing server-authority.
            SecurityLogger.LogSecurityEvent("InteractEntityNotFound", character.Id, $"Character attempted to interact with unknown entity {targetName}.", "");
            return Task.FromResult(new InteractResult { Success = false });
        }

        // Process Interaction Rules (Give Items, Craft, etc.)
        var interactionType = _interactionManager.ProcessInteraction(character, questComponent, targetName);

        // Notify character of interaction (Event-Driven)
        // PlayerQuestComponent listens to OnInteract and updates state.
        // ClientSession listens to OnQuestUpdated and sends packets.
        character.NotifyInteract(targetName, interactionType?.ToString());

        var result = new InteractResult
        {
            Success = true,
            UpdatedQuestIds = new List<int>(), // Events handle updates now
            InteractionType = interactionType
        };

        return Task.FromResult(result);
    }
}