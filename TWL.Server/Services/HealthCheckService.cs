using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TWL.Server.Persistence.Database;
using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Managers;

namespace TWL.Server.Services;

public enum ServerStatus
{
    Starting,
    Healthy,
    Unhealthy,
    ShuttingDown,
    Stopped
}

public class HealthStatus
{
    public string AppVersion { get; set; }
    public string Status { get; set; } = "Unknown";
    public ServerStatus ServerStatus { get; set; }
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
    private readonly SemaphoreSlim _signal = new(0);

    public virtual ServerStatus CurrentStatus { get; protected set; } = ServerStatus.Starting;

    // Default constructor for mocking/testing if needed, though we usually mock the class with arguments
    protected HealthCheckService() { }

    public HealthCheckService(DbService db, PlayerService playerService, ServerMetrics serverMetrics, ILogger<HealthCheckService> logger)
    {
        _db = db;
        _playerService = playerService;
        _serverMetrics = serverMetrics;
        _logger = logger;
    }

    public virtual void SetStatus(ServerStatus status)
    {
        if (CurrentStatus == status) return;

        CurrentStatus = status;
        _logger.LogInformation("Server Status changed to {Status}", status);

        // Trigger immediate check if waiting
        if (_signal.CurrentCount == 0)
        {
            _signal.Release();
        }
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

                // Determine overall status string based on CurrentStatus and DB health
                string statusString = CurrentStatus.ToString();
                if (CurrentStatus == ServerStatus.Healthy && !dbHealthy)
                {
                    statusString = "Unhealthy (DB)";
                }

                var status = new HealthStatus
                {
                    AppVersion = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "Unknown",
                    Status = statusString,
                    ServerStatus = CurrentStatus,
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

            // Wait for signal OR timeout (30s)
            try
            {
                await _signal.WaitAsync(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
