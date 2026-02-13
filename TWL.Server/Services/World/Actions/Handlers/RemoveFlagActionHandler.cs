using TWL.Server.Domain.World;
using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Networking;

namespace TWL.Server.Services.World.Actions.Handlers;

public class RemoveFlagActionHandler : ITriggerActionHandler
{
    private readonly PlayerService _playerService;

    public RemoveFlagActionHandler(PlayerService playerService)
    {
        _playerService = playerService;
    }

    public string ActionType => "RemoveFlag";

    public void Execute(ServerCharacter character, TriggerAction action)
    {
        if (action.Parameters.TryGetValue("Flag", out var flag))
        {
            var session = _playerService.GetSession(character.Id);
            session?.QuestComponent.RemoveFlag(flag);
        }
    }
}
