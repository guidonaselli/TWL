using TWL.Server.Domain.World;
using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;

namespace TWL.Server.Services.World.Actions.Handlers;

public class SpawnActionHandler : ITriggerActionHandler
{
    private readonly PlayerService _playerService;
    private readonly SpawnManager _spawnManager;

    public SpawnActionHandler(PlayerService playerService, SpawnManager spawnManager)
    {
        _playerService = playerService;
        _spawnManager = spawnManager;
    }

    public string ActionType => "Spawn";

    public void Execute(ServerCharacter character, TriggerAction action)
    {
        if (action.Parameters.TryGetValue("MonsterId", out var midStr) && int.TryParse(midStr, out var mid))
        {
            var count = 1;
            if (action.Parameters.TryGetValue("Count", out var countStr) && int.TryParse(countStr, out var c))
            {
                count = c;
            }

            var session = _playerService.GetSession(character.Id);
            if (session != null)
            {
                _spawnManager.StartScriptedEncounter(session, mid, count);
            }
        }
    }
}
