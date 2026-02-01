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

        var uniqueUpdates = new HashSet<int>();

        // Try "Talk", "Collect", "Interact"
        questComponent.TryProgress(uniqueUpdates, targetName, "Talk", "Collect", "Interact");

        // Try "Deliver" objectives
        var deliveredQuests = questComponent.TryDeliver(targetName);
        foreach (var qid in deliveredQuests)
        {
            uniqueUpdates.Add(qid);
        }

        // If interaction was successful (e.g. Crafting done), try "Craft" objectives
        if (!string.IsNullOrEmpty(interactionType))
        {
            questComponent.TryProgress(uniqueUpdates, targetName, interactionType);
        }

        var result = new InteractResult
        {
            Success = true,
            UpdatedQuestIds = new List<int>(uniqueUpdates)
        };

        return Task.FromResult(result);
    }
}