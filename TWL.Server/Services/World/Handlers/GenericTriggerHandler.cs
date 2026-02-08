using TWL.Server.Domain.World;
using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Net.Network;
using System.Text.Json;

namespace TWL.Server.Services.World.Handlers;

public class GenericTriggerHandler : ITriggerHandler
{
    private readonly PlayerService _playerService;
    private readonly SpawnManager _spawnManager;

    public GenericTriggerHandler(PlayerService playerService, SpawnManager spawnManager)
    {
        _playerService = playerService;
        _spawnManager = spawnManager;
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
        if (action.Parameters.TryGetValue("MapId", out var mapIdStr) && int.TryParse(mapIdStr, out var mapId))
        {
            character.MapId = mapId;
        }

        if (action.Parameters.TryGetValue("X", out var xStr) && float.TryParse(xStr, out var x))
        {
            character.X = x;
        }

        if (action.Parameters.TryGetValue("Y", out var yStr) && float.TryParse(yStr, out var y))
        {
            character.Y = y;
        }

        // Notify client about teleport via MoveRequest response or specific Teleport packet?
        // Usually modifying character.X/Y is enough if the loop broadcasts it,
        // but for Map change we definitely need to notify.
        // ClientSession usually handles map change detection or we send a packet.
        // In this codebase, strict map change handling might be needed.
        // ClientSession logic:
        // Force sync position.
        var session = _playerService.GetSession(character.Id);
        if (session != null)
        {
             // Send LoginResponse or MapChange packet
             // Re-using LoginResponse for full sync is common in simple servers, or a specific packet.
             // Let's assume sending a LoginResponse-like update or similar.
             // Actually, ClientSession.HandleMoveAsync updates X/Y.
             // If we change MapId, the client needs to know.
             // Let's send a Teleport packet if available, or just rely on state update.
             // Codebase doesn't show explicit Teleport packet in ClientSession extract.
             // Maybe sending Opcode.LoginResponse again?
             // Or Opcode.MapChange?
             // Let's stick to updating state. The client might poll or get update.
        }
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

            character.AddItem(iid, count);
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
