using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using TWL.Server.Domain.World;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;

namespace TWL.Server.Services.World;

public class WorldTriggerService : IWorldTriggerService
{
    private readonly ILogger<WorldTriggerService> _logger;
    private readonly ServerMetrics _metrics;
    private readonly Dictionary<int, ServerMap> _maps = new();
    private readonly List<ITriggerHandler> _handlers = new();

    public WorldTriggerService(ILogger<WorldTriggerService> logger, ServerMetrics metrics)
    {
        _logger = logger;
        _metrics = metrics;
    }

    public void LoadMaps(IEnumerable<ServerMap> maps)
    {
        foreach (var map in maps)
        {
            _maps[map.Id] = map;
        }
        _logger.LogInformation("Loaded {Count} maps into WorldTriggerService.", _maps.Count);
    }

    public void RegisterHandler(ITriggerHandler handler)
    {
        _handlers.Add(handler);
    }

    public void OnEnterTrigger(ServerCharacter character, int mapId, string triggerId)
    {
        if (!_maps.TryGetValue(mapId, out var map)) return;
        var trigger = map.Triggers.FirstOrDefault(t => t.Id == triggerId);
        if (trigger == null)
        {
            // Try by TMX ID if logical ID not found?
            // The map loader sets logical Id if present, else TMX Id.
            // So triggerId passed here should match what is in map.Triggers.
            return;
        }

        var handler = _handlers.FirstOrDefault(h => h.CanHandle(trigger.Type));
        if (handler != null)
        {
            _metrics.RecordTriggersExecuted();
            _logger.LogDebug("Character {CharId} entered trigger {TriggerId} ({Type})", character.Id, triggerId, trigger.Type);
            handler.ExecuteEnter(character, trigger, this);
        }
    }

    public void OnInteractTrigger(ServerCharacter character, int mapId, string triggerId)
    {
        if (!_maps.TryGetValue(mapId, out var map)) return;
        var trigger = map.Triggers.FirstOrDefault(t => t.Id == triggerId);
        if (trigger == null) return;

        var handler = _handlers.FirstOrDefault(h => h.CanHandle(trigger.Type));
        if (handler != null)
        {
            _metrics.RecordTriggersExecuted();
            _logger.LogDebug("Character {CharId} interacted with trigger {TriggerId} ({Type})", character.Id, triggerId, trigger.Type);
            handler.ExecuteInteract(character, trigger, this);
        }
    }

    public void CheckTriggers(ServerCharacter character)
    {
        if (!_maps.TryGetValue(character.MapId, out var map)) return;

        foreach (var trigger in map.Triggers)
        {
            // Simple AABB check
            if (character.X >= trigger.X && character.X < trigger.X + trigger.Width &&
                character.Y >= trigger.Y && character.Y < trigger.Y + trigger.Height)
            {
                OnEnterTrigger(character, character.MapId, trigger.Id);
            }
        }
    }

    public ServerSpawn? GetSpawn(int mapId, string spawnId)
    {
         if (!_maps.TryGetValue(mapId, out var map)) return null;
         return map.Spawns.FirstOrDefault(s => s.Id == spawnId);
    }
}
