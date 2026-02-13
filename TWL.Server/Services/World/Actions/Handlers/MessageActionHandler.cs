using TWL.Server.Domain.World;
using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Networking;

namespace TWL.Server.Services.World.Actions.Handlers;

public class MessageActionHandler : ITriggerActionHandler
{
    private readonly PlayerService _playerService;

    public MessageActionHandler(PlayerService playerService)
    {
        _playerService = playerService;
    }

    public string ActionType => "Message";

    public void Execute(ServerCharacter character, TriggerAction action)
    {
        if (action.Parameters.TryGetValue("Text", out var text))
        {
            var session = _playerService.GetSession(character.Id);
            if (session != null)
            {
                // TODO: Implement SystemMessage opcode
                Console.WriteLine($"[System Message to {character.Name}]: {text}");
            }
        }
    }
}
