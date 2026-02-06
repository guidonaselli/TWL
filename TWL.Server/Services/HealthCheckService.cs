using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TWL.Server.Persistence.Database;
using TWL.Server.Persistence.Services;

namespace TWL.Server.Services;

using TWL.Server.Simulation.Managers;

public class HealthStatus
{
    public string AppVersion { get; set; }
    public string Status { get; set; } = "Unknown";
    public TimeSpan Uptime { get; set; }
    public bool Database { get; set; }
    public int ActivePlayers { get; set; }
    public int DirtySessions { get; set; }
    public long LastPersistenceFlushMs { get; set; }
    public int LastPersistenceErrors { get; set; }
    public DateTime LastCheck { get; set; }
    public long WorldLoopDriftMs { get; set; }
    public long WorldLoopSkippedTicks { get; set; }
}

public class HealthCheckService : BackgroundService
{
    private readonly DbService _db;
    private readonly PlayerService _playerService;
    private readonly ServerMetrics _serverMetrics;
    private readonly ILogger<HealthCheckService> _logger;

    public HealthCheckService(DbService db, PlayerService playerService, ServerMetrics serverMetrics, ILogger<HealthCheckService> logger)
    {
        _db = db;
        _playerService = playerService;
        _serverMetrics = serverMetrics;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HealthCheckService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var dbHealthy = await _db.CheckHealthAsync();
                var pMetrics = _playerService.Metrics;
                var sMetrics = _serverMetrics.GetSnapshot();

                var status = new HealthStatus
                {
                    AppVersion = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "Unknown",
                    Status = dbHealthy ? "Healthy" : "Unhealthy",
                    Uptime = DateTime.UtcNow - _serverMetrics.StartTime,
                    Database = dbHealthy,
                    ActivePlayers = _playerService.ActiveSessionCount,
                    DirtySessions = _playerService.DirtySessionCount,
                    LastPersistenceFlushMs = pMetrics.LastFlushDurationMs,
                    LastPersistenceErrors = pMetrics.TotalSaveErrors,
                    LastCheck = DateTime.UtcNow,
                    WorldLoopDriftMs = sMetrics.WorldLoopDriftMs,
                    WorldLoopSkippedTicks = sMetrics.WorldLoopSkippedTicks
                };

                if (!dbHealthy)
                {
                    _logger.LogWarning("Health Check Failed: Database unreachable.");
                }

                var json = JsonSerializer.Serialize(status, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync("health.json", json, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "HealthCheckService error");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
