using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TWL.Server.Domain.World;
using TWL.Server.Persistence.Database;
using TWL.Server.Persistence.Services;
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
    private readonly MapLoader _mapLoader;
    private readonly ServerMetrics _metrics;
    private readonly MonsterManager _monsterManager;
    private readonly NetworkServer _net;
    private readonly PetManager _petManager;
    private readonly PlayerService _playerService;
    private readonly ServerQuestManager _questManager;
    private readonly SpawnManager _spawnManager;
    private readonly IWorldScheduler _worldScheduler;
    private readonly IWorldTriggerService _worldTriggerService;

    public ServerWorker(NetworkServer net, DbService db, ILogger<ServerWorker> log, PetManager petManager,
        ServerQuestManager questManager, InteractionManager interactionManager, PlayerService playerService,
        IWorldScheduler worldScheduler, ServerMetrics metrics, MapLoader mapLoader,
        IWorldTriggerService worldTriggerService, MonsterManager monsterManager, SpawnManager spawnManager)
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
        _mapLoader = mapLoader;
        _worldTriggerService = worldTriggerService;
        _monsterManager = monsterManager;
        _spawnManager = spawnManager;
    }

    public Task StartAsync(CancellationToken ct)
    {
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

        if (Directory.Exists("Content/Maps"))
        {
            var mapFiles = Directory.GetFiles("Content/Maps", "*.tmx", SearchOption.AllDirectories);
            var loadedMaps = new List<ServerMap>();
            foreach (var file in mapFiles)
            {
                try
                {
                    var map = _mapLoader.LoadMap(file);
                    loadedMaps.Add(map);
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Failed to load map {Path}", file);
                }
            }

            _worldTriggerService.LoadMaps(loadedMaps);
            _log.LogInformation("Loaded {Count} maps.", loadedMaps.Count);
        }
        else
        {
            _log.LogWarning("Content/Maps not found.");
        }

        _log.LogInformation("Starting server...");
        _net.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct)
    {
        _log.LogInformation("Stopping server...");
        _net.Stop();
        _worldScheduler.Stop();
        _playerService.Stop();
        _log.LogInformation("Server stopped.");
        return Task.CompletedTask;
    }
}