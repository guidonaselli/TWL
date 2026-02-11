using TWL.Server.Domain.World;
using TWL.Server.Persistence.Services;
using TWL.Server.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Models;
using TWL.Shared.Net.Network;
using System.Text.Json;

namespace TWL.Server.Services.World.Handlers;

public class GenericTriggerHandler : ITriggerHandler
{
    private readonly PlayerService _playerService;
    private readonly SpawnManager _spawnManager;
    private readonly InstanceService _instanceService;

    public GenericTriggerHandler(PlayerService playerService, SpawnManager spawnManager, InstanceService instanceService)
    {
        _playerService = playerService;
        _spawnManager = spawnManager;
        _instanceService = instanceService;
    }

    public bool CanHandle(string triggerType)
    {
        // Handle "Script", "Event", or "Generic" types, or fallback if Type is empty/null but Actions exist?
        // Let's explicitly handle "Generic" or "Script".
        // Also support existing types if they just have Actions attached?
        // MapLoader sets Type from property "TriggerType".
        return triggerType.Equals("Generic", StringComparison.OrdinalIgnoreCase) ||
               triggerType.Equals("Script", StringComparison.OrdinalIgnoreCase) ||
               triggerType.Equals("Event", StringComparison.OrdinalIgnoreCase);
    }

    public void ExecuteEnter(ServerCharacter character, ServerTrigger trigger, IWorldTriggerService context)
    {
        ExecuteActions(character, trigger);
    }

    public void ExecuteInteract(ServerCharacter character, ServerTrigger trigger, IWorldTriggerService context)
    {
        ExecuteActions(character, trigger);
    }

    public void ExecuteTick(ServerTrigger trigger, int mapId, IWorldTriggerService context)
    {
        // Ticks are harder because they don't have a specific character context usually.
        // But context.GetPlayersInTrigger can find them.
        foreach (var character in context.GetPlayersInTrigger(trigger, mapId))
        {
            ExecuteActions(character, trigger);
        }
    }

    private void ExecuteActions(ServerCharacter character, ServerTrigger trigger)
    {
        foreach (var action in trigger.Actions)
        {
            try
            {
                switch (action.Type)
                {
                    case "Teleport":
                        HandleTeleport(character, action);
                        break;
                    case "Spawn":
                        HandleSpawn(character, action);
                        break;
                    case "SetFlag":
                        HandleSetFlag(character, action);
                        break;
                    case "RemoveFlag":
                        HandleRemoveFlag(character, action);
                        break;
                    case "GiveItem":
                        HandleGiveItem(character, action);
                        break;
                    case "Heal":
                        HandleHeal(character, action);
                        break;
                    case "Damage":
                        HandleDamage(character, action);
                        break;
                    case "Message":
                        HandleMessage(character, action);
                        break;
                    case "EnterInstance":
                        HandleEnterInstance(character, action);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing action {action.Type} for trigger {trigger.Id}: {ex.Message}");
            }
        }
    }

    private void HandleTeleport(ServerCharacter character, TriggerAction action)
    {
        var mapId = character.MapId;
        if (action.Parameters.TryGetValue("MapId", out var mapIdStr) && int.TryParse(mapIdStr, out var mId))
        {
            mapId = mId;
        }

        var x = character.X;
        if (action.Parameters.TryGetValue("X", out var xStr) && float.TryParse(xStr, out var xVal))
        {
            x = xVal;
        }

        var y = character.Y;
        if (action.Parameters.TryGetValue("Y", out var yStr) && float.TryParse(yStr, out var yVal))
        {
            y = yVal;
        }

        character.Teleport(mapId, x, y);
    }

    private void HandleSpawn(ServerCharacter character, TriggerAction action)
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

    private void HandleSetFlag(ServerCharacter character, TriggerAction action)
    {
        if (action.Parameters.TryGetValue("Flag", out var flag))
        {
            var session = _playerService.GetSession(character.Id);
            session?.QuestComponent.AddFlag(flag);
        }
    }

    private void HandleRemoveFlag(ServerCharacter character, TriggerAction action)
    {
        if (action.Parameters.TryGetValue("Flag", out var flag))
        {
            var session = _playerService.GetSession(character.Id);
            session?.QuestComponent.RemoveFlag(flag);
        }
    }

    private void HandleGiveItem(ServerCharacter character, TriggerAction action)
    {
        if (action.Parameters.TryGetValue("ItemId", out var iidStr) && int.TryParse(iidStr, out var iid))
        {
            var count = 1;
            if (action.Parameters.TryGetValue("Count", out var countStr) && int.TryParse(countStr, out var c))
            {
                count = c;
            }

            var policy = BindPolicy.Unbound;
            if (action.Parameters.TryGetValue("Policy", out var polStr) && Enum.TryParse<BindPolicy>(polStr, out var pol))
            {
                policy = pol;
            }

            int? boundTo = null;
            if (action.Parameters.TryGetValue("BoundTo", out var boundStr) && int.TryParse(boundStr, out var boundId))
            {
                boundTo = boundId;
            }

            if (character.CanAddItem(iid, count, policy, boundTo))
            {
                character.AddItem(iid, count, policy, boundTo);
            }
            else
            {
                Console.WriteLine($"[Trigger] Failed to give item {iid} x{count} to {character.Name} (Inventory Full)");
                // Potentially send message to player
            }
        }
    }

    private void HandleEnterInstance(ServerCharacter character, TriggerAction action)
    {
        if (action.Parameters.TryGetValue("InstanceId", out var instanceId))
        {
            var session = _playerService.GetSession(character.Id);
            if (session != null)
            {
                _instanceService.StartInstance(session, instanceId);
            }
        }
    }

    private void HandleHeal(ServerCharacter character, TriggerAction action)
    {
        if (action.Parameters.TryGetValue("Amount", out var amtStr) && int.TryParse(amtStr, out var amt))
        {
            character.Heal(amt);
        }
    }

    private void HandleDamage(ServerCharacter character, TriggerAction action)
    {
        if (action.Parameters.TryGetValue("Amount", out var amtStr) && int.TryParse(amtStr, out var amt))
        {
            character.ApplyDamage(amt);
        }
    }

    private void HandleMessage(ServerCharacter character, TriggerAction action)
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
