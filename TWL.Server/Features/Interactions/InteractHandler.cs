using TWL.Server.Architecture.Pipeline;
using TWL.Server.Simulation.Managers;

namespace TWL.Server.Features.Interactions;

public class InteractHandler : ICommandHandler<InteractCommand, InteractResult>
{
    private readonly InteractionManager _interactionManager;

    public InteractHandler(InteractionManager interactionManager)
    {
        _interactionManager = interactionManager;
    }

    public Task<InteractResult> Handle(InteractCommand command, CancellationToken cancellationToken)
    {
        var character = command.Character;
        var questComponent = command.QuestComponent;
        var targetName = command.TargetName;

        // Process Interaction Rules (Give Items, Craft, etc.)
        string? interactionType = null;
        if (character != null)
        {
            interactionType = _interactionManager.ProcessInteraction(character, questComponent, targetName);
        }

        // Notify character of interaction (Event-Driven)
        // PlayerQuestComponent listens to OnInteract and updates state.
        // ClientSession listens to OnQuestUpdated and sends packets.
        character.NotifyInteract(targetName, interactionType);

        var result = new InteractResult
        {
            Success = true,
            UpdatedQuestIds = new List<int>() // Events handle updates now
        };

        return Task.FromResult(result);
    }
}