using TWL.Server.Domain.World;
using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Networking;

namespace TWL.Server.Services.World.Actions.Handlers;

public class SetFlagActionHandler : ITriggerActionHandler
{
    private readonly PlayerService _playerService;

    public SetFlagActionHandler(PlayerService playerService)
    {
        _playerService = playerService;
    }

    public string ActionType => "SetFlag";

    public void Execute(ServerCharacter character, TriggerAction action)
    {
        if (action.Parameters.TryGetValue("Flag", out var flag))
        {
            var session = _playerService.GetSession(character.Id);
            session?.QuestComponent.AddFlag(flag);
        }
    }
}
