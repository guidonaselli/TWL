using TWL.Server.Domain.World;
using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Networking;

namespace TWL.Server.Services.World.Handlers;

public class QuestTriggerHandler : ITriggerHandler
{
    private readonly PlayerService _playerService;

    public QuestTriggerHandler(PlayerService playerService)
    {
        _playerService = playerService;
    }

    public bool CanHandle(string triggerType)
    {
        return triggerType.Equals("Quest", StringComparison.OrdinalIgnoreCase) ||
               triggerType.Equals("Explore", StringComparison.OrdinalIgnoreCase) ||
               triggerType.Equals("Visit", StringComparison.OrdinalIgnoreCase);
    }

    public void ExecuteEnter(ServerCharacter character, ServerTrigger trigger, IWorldTriggerService context)
    {
        var session = _playerService.GetSession(character.Id);
        if (session != null)
        {
            var targetName = trigger.Properties.TryGetValue("TargetName", out var val) ? val : trigger.Id;
            var updated = session.QuestComponent.TryProgress("Explore", targetName);
        }
    }

    public void ExecuteInteract(ServerCharacter character, ServerTrigger trigger, IWorldTriggerService context)
    {
        ExecuteEnter(character, trigger, context);
    }
}
