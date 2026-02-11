using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TWL.Server.Domain.World;
using TWL.Server.Persistence.Database;
using TWL.Server.Persistence.Services;
using TWL.Server.Services;
using TWL.Server.Services.World;
using TWL.Server.Services.World.Handlers;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Services;

namespace TWL.Server.Simulation;

public class ServerWorker : IHostedService
{
    private readonly DbService _db;
    private readonly InteractionManager _interactionManager;
    private readonly ILogger<ServerWorker> _log;
    private readonly IMapRegistry _mapRegistry;
    private readonly ServerMetrics _metrics;
    private readonly MonsterManager _monsterManager;
    private readonly INetworkServer _net;
    private readonly PetManager _petManager;
    private readonly PlayerService _playerService;
    private readonly ServerQuestManager _questManager;
    private readonly ILoggerFactory _loggerFactory;
    private readonly SpawnManager _spawnManager;
    private readonly IWorldScheduler _worldScheduler;
    private readonly IWorldTriggerService _worldTriggerService;
    private readonly HealthCheckService _healthCheck;
    private readonly InstanceService _instanceService;

    public ServerWorker(INetworkServer net, DbService db, ILogger<ServerWorker> log, PetManager petManager,
        ServerQuestManager questManager, InteractionManager interactionManager, PlayerService playerService,
        IWorldScheduler worldScheduler, ServerMetrics metrics, IMapRegistry mapRegistry,
        IWorldTriggerService worldTriggerService, MonsterManager monsterManager, SpawnManager spawnManager,
        ILoggerFactory loggerFactory, HealthCheckService healthCheck, InstanceService instanceService)
    {
        _net = net;
        _db = db;
        _log = log;
        _petManager = petManager;
        _questManager = questManager;
        _interactionManager = interactionManager;
        _playerService = playerService;
        _worldScheduler = worldScheduler;
        _metrics = metrics;
        _mapRegistry = mapRegistry;
        _worldTriggerService = worldTriggerService;
        _monsterManager = monsterManager;
        _spawnManager = spawnManager;
        _loggerFactory = loggerFactory;
        _healthCheck = healthCheck;
        _instanceService = instanceService;
    }

    public Task StartAsync(CancellationToken ct)
    {
        _healthCheck.SetStatus(ServerStatus.Starting);
        _log.LogInformation("Init DB...");
        _db.InitDatabase();

        _log.LogInformation("Starting World Scheduler...");
        _worldScheduler.Start();

        // Metrics Reporter (1 min interval = 1200 ticks at 20 TPS)
        _worldScheduler.OnTick += tick =>
        {
            if (tick % 1200 == 0)
            {
                var snap = _metrics.GetSnapshot();
                _log.LogInformation(snap.ToString());
            }
        };

        _log.LogInformation("Starting persistence service...");
        _playerService.Start();

        _log.LogInformation("Loading Game Data...");
        _monsterManager.Load("Content/Data/monsters.json");
        _spawnManager.Load("Content/Data/spawns");
        _petManager.Load("Content/Data/pets.json");
        _questManager.Load("Content/Data/quests.json");
        _interactionManager.Load("Content/Data/interactions.json");

        // Schedule Spawn Manager
        _worldScheduler.ScheduleRepeating(() => _spawnManager.Update(0.05f), TimeSpan.FromMilliseconds(50),
            "SpawnManager");

        if (File.Exists("Content/Data/skills.json"))
        {
            var json = File.ReadAllText("Content/Data/skills.json");
            SkillRegistry.Instance.LoadSkills(json);
        }

        _log.LogInformation("Loading Maps...");
        _worldTriggerService.RegisterHandler(new MapTransitionHandler());
        _worldTriggerService.RegisterHandler(new QuestTriggerHandler(_playerService));
        // Manual resolution for now until DI registration for handlers is improved
        _worldTriggerService.RegisterHandler(new DamageTriggerHandler(_loggerFactory.CreateLogger<DamageTriggerHandler>(), _playerService));
        _worldTriggerService.RegisterHandler(new GenericTriggerHandler(_playerService, _spawnManager, _instanceService));

        if (Directory.Exists("Content/Maps"))
        {
            _mapRegistry.Load("Content/Maps");
        }
        else
        {
            _log.LogWarning("Content/Maps not found.");
        }

        _worldTriggerService.Start();

        _log.LogInformation("Starting server...");
        _net.Start();
        _healthCheck.SetStatus(ServerStatus.Healthy);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        _log.LogInformation("Stopping server...");
        _healthCheck.SetStatus(ServerStatus.ShuttingDown);

        // Simulate drain time to allow LBs to notice the health change (configurable in future)
        _log.LogInformation("Waiting for load balancer drain (5s)...");
        try
        {
            await Task.Delay(5000, ct);
        }
        catch (TaskCanceledException) { /* ignore */ }

        _net.Stop();

        _log.LogInformation("Disconnecting players...");
        await _playerService.DisconnectAllAsync("Server Shutdown");

        // Allow buffers to flush
        await Task.Delay(500, ct);

        _worldScheduler.Stop();
        _playerService.Stop();

        sw.Stop();
        _metrics.RecordShutdown(sw.ElapsedMilliseconds, _playerService.Metrics.SessionsSavedInLastFlush);

        _log.LogInformation("Server stopped in {Elapsed}ms.", sw.ElapsedMilliseconds);
        _healthCheck.SetStatus(ServerStatus.Stopped);
    }
}