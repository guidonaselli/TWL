using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TWL.Server.Persistence.Database;
using TWL.Server.Persistence.Services;

namespace TWL.Server.Services;

public class HealthStatus
{
    public string Status { get; set; } = "Unknown";
    public bool Database { get; set; }
    public long LastPersistenceFlushMs { get; set; }
    public int LastPersistenceErrors { get; set; }
    public DateTime LastCheck { get; set; }
}

public class HealthCheckService : BackgroundService
{
    private readonly DbService _db;
    private readonly PlayerService _playerService;
    private readonly ILogger<HealthCheckService> _logger;

    public HealthCheckService(DbService db, PlayerService playerService, ILogger<HealthCheckService> logger)
    {
        _db = db;
        _playerService = playerService;
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
                var metrics = _playerService.Metrics;

                var status = new HealthStatus
                {
                    Status = dbHealthy ? "Healthy" : "Unhealthy",
                    Database = dbHealthy,
                    LastPersistenceFlushMs = metrics.LastFlushDurationMs,
                    LastPersistenceErrors = metrics.TotalSaveErrors,
                    LastCheck = DateTime.UtcNow
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
