using System.Text.Json;
using Microsoft.Extensions.Logging;
using TWL.Server.Domain.World;
using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Net.Network;
using TWL.Shared.Net.Payloads;

namespace TWL.Server.Services.World.Handlers;

public class DamageTriggerHandler : ITriggerHandler
{
    private readonly ILogger<DamageTriggerHandler> _logger;
    private readonly PlayerService _playerService;

    public DamageTriggerHandler(ILogger<DamageTriggerHandler> logger, PlayerService playerService)
    {
        _logger = logger;
        _playerService = playerService;
    }

    public bool CanHandle(string triggerType) => triggerType == "DamageRegion";

    public void ExecuteEnter(ServerCharacter character, ServerTrigger trigger, IWorldTriggerService context)
    {
        ApplyDamage(character, trigger);
    }

    public void ExecuteInteract(ServerCharacter character, ServerTrigger trigger, IWorldTriggerService context)
    {
        // No interaction
    }

    public void ExecuteTick(ServerTrigger trigger, int mapId, IWorldTriggerService context)
    {
        var players = context.GetPlayersInTrigger(trigger, mapId);
        foreach (var player in players)
        {
            ApplyDamage(player, trigger);
        }
    }

    private void ApplyDamage(ServerCharacter character, ServerTrigger trigger)
    {
        if (trigger.Properties.TryGetValue("DamageAmount", out var dmgStr) && int.TryParse(dmgStr, out var dmg))
        {
            var oldHp = character.Hp;
            character.ApplyDamage(dmg);
            if (oldHp != character.Hp)
            {
                _logger.LogDebug("Character {CharId} damaged by trigger {TriggerId}. HP: {Old} -> {New}",
                    character.Id, trigger.Id, oldHp, character.Hp);

                var session = _playerService.GetSession(character.Id);
                if (session != null)
                {
                    _ = session.SendAsync(new NetMessage
                    {
                        Op = Opcode.StatsUpdate,
                        JsonPayload = JsonSerializer.Serialize(new { hp = character.Hp, maxHp = character.MaxHp })
                    });
                }
            }
        }
    }
}
