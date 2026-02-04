using Microsoft.Extensions.Logging;
using TWL.Server.Domain.World;
using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Services;

namespace TWL.Server.Services.World;

public class WorldTriggerService : IWorldTriggerService
{
    private readonly List<ITriggerHandler> _handlers = new();
    private readonly ILogger<WorldTriggerService> _logger;
    private readonly Dictionary<int, ServerMap> _maps = new();
    private readonly ServerMetrics _metrics;
    private readonly PlayerService _playerService;
    private readonly IWorldScheduler _scheduler;
    private bool _started;

    public WorldTriggerService(ILogger<WorldTriggerService> logger, ServerMetrics metrics, PlayerService playerService, IWorldScheduler scheduler)
    {
        _logger = logger;
        _metrics = metrics;
        _playerService = playerService;
        _scheduler = scheduler;
    }

    public void Start()
    {
        if (_started) return;
        _started = true;

        foreach (var map in _maps.Values)
        {
            foreach (var trigger in map.Triggers)
            {
                if (trigger.ActivationType == TriggerActivationType.Timer && trigger.IntervalMs > 0)
                {
                    _scheduler.ScheduleRepeating(() =>
                    {
                        var handler = _handlers.FirstOrDefault(h => h.CanHandle(trigger.Type));
                        if (handler != null)
                        {
                            try
                            {
                                handler.ExecuteTick(trigger, map.Id, this);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error executing timer trigger {TriggerId} in map {MapId}", trigger.Id, map.Id);
                            }
                        }
                    }, TimeSpan.FromMilliseconds(trigger.IntervalMs), $"Trigger-{map.Id}-{trigger.Id}");
                }
            }
        }
    }

    public void LoadMaps(IEnumerable<ServerMap> maps)
    {
        foreach (var map in maps)
        {
            _maps[map.Id] = map;
        }

        _logger.LogInformation("Loaded {Count} maps into WorldTriggerService.", _maps.Count);
    }

    public void RegisterHandler(ITriggerHandler handler) => _handlers.Add(handler);

    public void OnEnterTrigger(ServerCharacter character, int mapId, string triggerId)
    {
        if (!_maps.TryGetValue(mapId, out var map))
        {
            return;
        }

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
            if (!CheckConditions(character, trigger))
            {
                return;
            }

            _logger.LogDebug("Character {CharId} entered trigger {TriggerId} ({Type})", character.Id, triggerId,
                trigger.Type);
            _metrics.RecordTriggerExecuted(trigger.Type);
            handler.ExecuteEnter(character, trigger, this);
        }
    }

    public void OnInteractTrigger(ServerCharacter character, int mapId, string triggerId)
    {
        if (!_maps.TryGetValue(mapId, out var map))
        {
            return;
        }

        var trigger = map.Triggers.FirstOrDefault(t => t.Id == triggerId);
        if (trigger == null)
        {
            return;
        }

        var handler = _handlers.FirstOrDefault(h => h.CanHandle(trigger.Type));
        if (handler != null)
        {
            if (!CheckConditions(character, trigger))
            {
                return;
            }

            _logger.LogDebug("Character {CharId} interacted with trigger {TriggerId} ({Type})", character.Id, triggerId,
                trigger.Type);
            _metrics.RecordTriggerExecuted(trigger.Type);
            handler.ExecuteInteract(character, trigger, this);
        }
    }

    private bool CheckConditions(ServerCharacter character, ServerTrigger trigger)
    {
        foreach (var condition in trigger.Conditions)
        {
            if (!condition.IsMet(character, _playerService))
            {
                // Can send message to player here if needed (e.g. "Locked")
                return false;
            }
        }
        return true;
    }

    public void CheckTriggers(ServerCharacter character)
    {
        if (!_maps.TryGetValue(character.MapId, out var map))
        {
            return;
        }

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
        if (!_maps.TryGetValue(mapId, out var map))
        {
            return null;
        }

        return map.Spawns.FirstOrDefault(s => s.Id == spawnId);
    }

    public IEnumerable<ServerCharacter> GetPlayersInTrigger(ServerTrigger trigger, int mapId)
    {
        var sessions = new List<ClientSession>();
        _playerService.GetSessions(sessions, s => s.Character != null && s.Character.MapId == mapId);

        return sessions.Select(s => s.Character!)
            .Where(c => c.X >= trigger.X && c.X < trigger.X + trigger.Width &&
                        c.Y >= trigger.Y && c.Y < trigger.Y + trigger.Height);
    }
}