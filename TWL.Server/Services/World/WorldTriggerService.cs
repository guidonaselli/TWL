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
    private readonly IMapRegistry _mapRegistry;
    private readonly ServerMetrics _metrics;
    private readonly PlayerService _playerService;
    private readonly IWorldScheduler _scheduler;
    private readonly Dictionary<(int, string), long> _triggerCooldowns = new();
    private bool _started;

    public WorldTriggerService(ILogger<WorldTriggerService> logger, ServerMetrics metrics, PlayerService playerService,
        IWorldScheduler scheduler, IMapRegistry mapRegistry)
    {
        _logger = logger;
        _metrics = metrics;
        _playerService = playerService;
        _scheduler = scheduler;
        _mapRegistry = mapRegistry;
    }

    public void Start()
    {
        if (_started) return;
        _started = true;

        foreach (var map in _mapRegistry.GetAllMaps())
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

    public void RegisterHandler(ITriggerHandler handler) => _handlers.Add(handler);

    public void OnFlagChanged(ServerCharacter character, string flag)
    {
        var map = _mapRegistry.GetMap(character.MapId);
        if (map == null)
        {
            return;
        }

        foreach (var trigger in map.Triggers)
        {
            if (trigger.ActivationType == TriggerActivationType.Flag)
            {
                // Check if this trigger is activated by the specific flag
                if (trigger.Properties.TryGetValue("ReqFlag", out var reqFlag))
                {
                    if (reqFlag == flag)
                    {
                        if (IsOnCooldown(map.Id, trigger.Id)) continue;

                        if (!CheckConditions(character, trigger)) continue;

                        var handler = _handlers.FirstOrDefault(h => h.CanHandle(trigger.Type));
                        if (handler != null)
                        {
                            ApplyCooldown(map.Id, trigger);
                            _logger.LogDebug("Character {CharId} activated flag trigger {TriggerId} ({Type})",
                                character.Id, trigger.Id, trigger.Type);
                            handler.ExecuteEnter(character, trigger, this);
                        }
                    }
                }
            }
        }
    }

    public void OnEnterTrigger(ServerCharacter character, int mapId, string triggerId)
    {
        var map = _mapRegistry.GetMap(mapId);
        if (map == null)
        {
            return;
        }

        var trigger = map.Triggers.FirstOrDefault(t => t.Id == triggerId);
        if (trigger == null)
        {
            return;
        }

        if (IsOnCooldown(mapId, triggerId))
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

            ApplyCooldown(mapId, trigger);

            _logger.LogDebug("Character {CharId} entered trigger {TriggerId} ({Type})", character.Id, triggerId,
                trigger.Type);
            _metrics.RecordTriggerExecuted(trigger.Type);
            handler.ExecuteEnter(character, trigger, this);
        }
    }

    public void OnInteractTrigger(ServerCharacter character, int mapId, string triggerId)
    {
        var map = _mapRegistry.GetMap(mapId);
        if (map == null)
        {
            return;
        }

        var trigger = map.Triggers.FirstOrDefault(t => t.Id == triggerId);
        if (trigger == null)
        {
            return;
        }

        if (IsOnCooldown(mapId, triggerId))
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

            ApplyCooldown(mapId, trigger);

            _logger.LogDebug("Character {CharId} interacted with trigger {TriggerId} ({Type})", character.Id, triggerId,
                trigger.Type);
            _metrics.RecordTriggerExecuted(trigger.Type);
            handler.ExecuteInteract(character, trigger, this);
        }
    }

    private bool IsOnCooldown(int mapId, string triggerId)
    {
        if (_triggerCooldowns.TryGetValue((mapId, triggerId), out var nextTick))
        {
            if (_scheduler.CurrentTick < nextTick)
            {
                return true;
            }
            // Expired, can cleanup if we want, but overwriting is cheaper
        }
        return false;
    }

    private void ApplyCooldown(int mapId, ServerTrigger trigger)
    {
        if (trigger.CooldownMs > 0)
        {
            var ticks = trigger.CooldownMs / 50; // 50ms per tick
            if (ticks < 1) ticks = 1;
            _triggerCooldowns[(mapId, trigger.Id)] = _scheduler.CurrentTick + ticks;
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
        var map = _mapRegistry.GetMap(character.MapId);
        if (map == null)
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
        var map = _mapRegistry.GetMap(mapId);
        if (map == null)
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