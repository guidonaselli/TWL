using System.Text.Json;
using TWL.Server.Domain.World;
using TWL.Server.Persistence.Services;
using TWL.Server.Services.World.Actions;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Net.Network;
using TWL.Shared.Net.Payloads;

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
                var payload = new SystemMessageDto { Text = text };
                var netMsg = new NetMessage
                {
                    Op = Opcode.SystemMessage,
                    JsonPayload = JsonSerializer.Serialize(payload)
                };

                // Fire and forget
                _ = session.SendAsync(netMsg);

                Console.WriteLine($"[System Message to {character.Name}]: {text}");
            }
        }
    }
}
