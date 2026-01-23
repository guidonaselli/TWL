using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TWL.Server.Persistence.Database;
using TWL.Server.Persistence.Services;
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

    public ServerWorker(NetworkServer net, DbService db, ILogger<ServerWorker> log, PetManager petManager, ServerQuestManager questManager, InteractionManager interactionManager, PlayerService playerService)
    {
        _net = net;
        _db = db;
        _log = log;
        _petManager = petManager;
        _questManager = questManager;
        _interactionManager = interactionManager;
        _playerService = playerService;
    }

    public Task StartAsync(CancellationToken ct)
    {
        _log.LogInformation("Init DB...");
        _db.InitDatabase();

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

        _log.LogInformation("Starting server...");
        _net.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct)
    {
        _log.LogInformation("Stopping server...");
        _net.Stop();
        _playerService.Stop();
        _log.LogInformation("Server stopped.");
        return Task.CompletedTask;
    }
}