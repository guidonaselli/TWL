using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TWL.Server.Simulation.Managers;

/// <summary>
/// Background service that periodically triggers market listing expiration checks.
/// </summary>
public class MarketListingScheduler : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MarketListingScheduler> _logger;

    public MarketListingScheduler(IServiceProvider serviceProvider, ILogger<MarketListingScheduler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Market Listing Scheduler is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // We use a scope although IMarketService is a singleton to follow best practices 
                // and allow for future scoped dependencies if needed.
                using (var scope = _serviceProvider.CreateScope())
                {
                    var marketService = scope.ServiceProvider.GetRequiredService<IMarketService>();
                    _logger.LogDebug("Running market listing expiration check...");
                    await marketService.ExpireListingsAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while expiring market listings.");
            }

            // Wait for 60 seconds before next execution
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }

        _logger.LogInformation("Market Listing Scheduler is stopping.");
    }
}
