using System.Numerics;
using TWL.Server.Architecture.Pipeline;
using TWL.Server.Security;
using TWL.Server.Simulation.Managers;

namespace TWL.Server.Features.Interactions;

public class InteractHandler : ICommandHandler<InteractCommand, InteractResult>
{
    private readonly InteractionManager _interactionManager;
    private const float MaxInteractDistance = 5.0f * 32.0f; // 5 tiles

    public InteractHandler(InteractionManager interactionManager)
    {
        _interactionManager = interactionManager;
    }

    public Task<InteractResult> Handle(InteractCommand command, CancellationToken cancellationToken)
    {
        var character = command.Character;
        var questComponent = command.QuestComponent;
        var targetName = command.TargetName;

        if (character != null)
        {
            // Validate Proximity (SEC-001)
            var targetPos = _interactionManager.GetTargetPosition(character.MapId, targetName);

            if (!targetPos.HasValue)
            {
                // Fail-Closed: If the target does not exist on the map or its position cannot be verified, reject the interaction.
                SecurityLogger.LogSecurityEvent("InteractTargetNotFound", character.Id, $"Target:{targetName} MapId:{character.MapId}");
                return Task.FromResult(new InteractResult { Success = false, UpdatedQuestIds = new List<int>() });
            }

            var distanceSq = Vector2.DistanceSquared(new Vector2(character.X, character.Y), targetPos.Value);
            if (distanceSq > MaxInteractDistance * MaxInteractDistance)
            {
                SecurityLogger.LogSecurityEvent("InteractOutOfRange", character.Id, $"Target:{targetName} Distance:{Math.Sqrt(distanceSq):F1} > {MaxInteractDistance}");
                return Task.FromResult(new InteractResult { Success = false, UpdatedQuestIds = new List<int>() });
            }
        }

        // Process Interaction Rules (Give Items, Craft, etc.)
        string? interactionType = null;
        if (character != null)
        {
            interactionType = _interactionManager.ProcessInteraction(character, questComponent, targetName);
        }

        // Notify character of interaction (Event-Driven)
        // PlayerQuestComponent listens to OnInteract and updates state.
        // ClientSession listens to OnQuestUpdated and sends packets.
        character?.NotifyInteract(targetName, interactionType);

        var result = new InteractResult
        {
            Success = true,
            UpdatedQuestIds = new List<int>() // Events handle updates now
        };

        return Task.FromResult(result);
    }
}