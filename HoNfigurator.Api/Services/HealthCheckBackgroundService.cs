using HoNfigurator.Core.Health;

namespace HoNfigurator.Api.Services;

/// <summary>
/// Background service that runs periodic health checks
/// </summary>
public class HealthCheckBackgroundService : BackgroundService
{
    private readonly ILogger<HealthCheckBackgroundService> _logger;
    private readonly HealthCheckManager _healthCheckManager;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

    public HealthCheckBackgroundService(
        ILogger<HealthCheckBackgroundService> logger,
        HealthCheckManager healthCheckManager)
    {
        _logger = logger;
        _healthCheckManager = healthCheckManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Health check background service started");

        // Run initial health check
        await RunHealthChecksAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
                await RunHealthChecksAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during health check");
            }
        }

        _logger.LogInformation("Health check background service stopped");
    }

    private async Task RunHealthChecksAsync(CancellationToken cancellationToken)
    {
        try
        {
            var results = await _healthCheckManager.RunAllChecksAsync(cancellationToken);
            
            var unhealthy = results.Values.Where(r => !r.IsHealthy).ToList();
            if (unhealthy.Count > 0)
            {
                foreach (var result in unhealthy)
                {
                    _logger.LogWarning("Health check failed: {Name} - {Status}: {Message}",
                        result.Name, result.Status, result.Message);
                }
            }
            else
            {
                _logger.LogDebug("All health checks passed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run health checks");
        }
    }
}
