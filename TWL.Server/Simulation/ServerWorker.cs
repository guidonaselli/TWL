using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TWL.Server.Persistence.Database;
using TWL.Server.Simulation.Networking;

namespace TWL.Server.Simulation;

public class ServerWorker : IHostedService
{
    private readonly DbService _db;
    private readonly ILogger<ServerWorker> _log;
    private readonly NetworkServer _net;

    public ServerWorker(NetworkServer net, DbService db, ILogger<ServerWorker> log)
    {
        _net = net;
        _db = db;
        _log = log;
    }

    public Task StartAsync(CancellationToken ct)
    {
        _log.LogInformation("Init DB...");
        _db.InitDatabase();
        _log.LogInformation("Starting server...");
        _net.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct)
    {
        _log.LogInformation("Stopping server...");
        _net.Stop();
        _log.LogInformation("Server stopped.");
        return Task.CompletedTask;
    }
}