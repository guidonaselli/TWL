using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TWL.Server.Persistence.Database;
using TWL.Server.Persistence.Services;
using TWL.Server.Services.World;
using TWL.Server.Domain.World;
using TWL.Server.Simulation.Networking;

namespace TWL.Server.Simulation;

using TWL.Server.Simulation.Managers;

public class ServerWorker : IHostedService
{
    private readonly DbService _db;
    private readonly ILogger<ServerWorker> _log;
    private readonly NetworkServer _net;
    private readonly PetManager _petManager;
    private readonly ServerQuestManager _questManager;
    private readonly InteractionManager _interactionManager;
    private readonly PlayerService _playerService;
    private readonly TWL.Shared.Services.IWorldScheduler _worldScheduler;
    private readonly ServerMetrics _metrics;
    private readonly MapLoader _mapLoader;
    private readonly IWorldTriggerService _worldTriggerService;

    public ServerWorker(NetworkServer net, DbService db, ILogger<ServerWorker> log, PetManager petManager, ServerQuestManager questManager, InteractionManager interactionManager, PlayerService playerService, TWL.Shared.Services.IWorldScheduler worldScheduler, ServerMetrics metrics, MapLoader mapLoader, IWorldTriggerService worldTriggerService)
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
    }

    public Task StartAsync(CancellationToken ct)
    {
        _log.LogInformation("Init DB...");
        _db.InitDatabase();

        _log.LogInformation("Starting World Scheduler...");
        _worldScheduler.Start();

        // Metrics Reporter (1 min interval = 1200 ticks at 20 TPS)
        _worldScheduler.OnTick += (tick) =>
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
        _petManager.Load("Content/Data/pets.json");
        _questManager.Load("Content/Data/quests.json");
        _interactionManager.Load("Content/Data/interactions.json");

        if (System.IO.File.Exists("Content/Data/skills.json"))
        {
            var json = System.IO.File.ReadAllText("Content/Data/skills.json");
            TWL.Shared.Domain.Skills.SkillRegistry.Instance.LoadSkills(json);
        }

        _log.LogInformation("Loading Maps...");
        _worldTriggerService.RegisterHandler(new TWL.Server.Services.World.Handlers.MapTransitionHandler());

        if (System.IO.Directory.Exists("Content/Maps"))
        {
            var mapFiles = System.IO.Directory.GetFiles("Content/Maps", "*.tmx", System.IO.SearchOption.AllDirectories);
            var loadedMaps = new List<ServerMap>();
            foreach (var file in mapFiles)
            {
                try
                {
                    var map = _mapLoader.LoadMap(file);
                    loadedMaps.Add(map);
                }
                catch (System.Exception ex)
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