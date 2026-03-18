using TWL.Server.Domain.World;
using System.Text.Json;
using TWL.Server.Persistence.Services;
using TWL.Server.Services.World;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Net.Network;

namespace TWL.Server.Services.World.Actions.Handlers;

public class EnterInstanceActionHandler : ITriggerActionHandler
{
    private readonly InstanceService _instanceService;
    private readonly PlayerService _playerService;

    public EnterInstanceActionHandler(PlayerService playerService, InstanceService instanceService)
    {
        _playerService = playerService;
        _instanceService = instanceService;
    }

    public string ActionType => "EnterInstance";

    public void Execute(ServerCharacter character, TriggerAction action)
    {
        if (action.Parameters.TryGetValue("InstanceId", out var instanceId))
        {
            var session = _playerService.GetSession(character.Id);
            if (session != null)
            {
                if (_instanceService.CanEnterInstance(character, instanceId))
                {
                    _instanceService.RecordInstanceRun(character, instanceId);
                    _instanceService.StartInstance(session, instanceId);
                }
                else
                {
                    _ = session.SendAsync(new NetMessage
                    {
                        Op = Opcode.SystemMessage,
                        JsonPayload = JsonSerializer.Serialize(new { Message = "Daily instance limit reached." }, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    });
                }
            }
        }
    }
}
