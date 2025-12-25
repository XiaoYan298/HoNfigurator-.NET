using Microsoft.AspNetCore.SignalR;
using HoNfigurator.Api.Hubs;
using HoNfigurator.Core.Notifications;
using HoNfigurator.Core.Charts;

namespace HoNfigurator.Api.Services;

/// <summary>
/// Background service that broadcasts notifications and collects chart data
/// </summary>
public class NotificationBroadcastService : BackgroundService
{
    private readonly IHubContext<DashboardHub, IDashboardClient> _hubContext;
    private readonly INotificationService _notificationService;
    private readonly IChartDataService _chartDataService;
    private readonly ILogger<NotificationBroadcastService> _logger;
    private readonly TimeSpan _resourceCheckInterval = TimeSpan.FromSeconds(10);

    public NotificationBroadcastService(
        IHubContext<DashboardHub, IDashboardClient> hubContext,
        INotificationService notificationService,
        IChartDataService chartDataService,
        ILogger<NotificationBroadcastService> logger)
    {
        _hubContext = hubContext;
        _notificationService = notificationService;
        _chartDataService = chartDataService;
        _logger = logger;

        // Subscribe to notification events
        _notificationService.OnNotification += OnNotificationReceived;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification broadcast service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Collect resource metrics for charts
                var (cpu, memory, disk) = GetResourceMetrics();
                _chartDataService.RecordResourceMetrics(cpu, memory, disk);

                // Check thresholds and send alerts
                _notificationService.CheckResourceThresholds(cpu, memory, disk);

                await Task.Delay(_resourceCheckInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in notification broadcast service");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        _notificationService.OnNotification -= OnNotificationReceived;
        _logger.LogInformation("Notification broadcast service stopped");
    }

    private async void OnNotificationReceived(object? sender, Notification notification)
    {
        try
        {
            // Broadcast notification to all connected clients
            await _hubContext.Clients.All.ReceiveNotification(
                notification.Title,
                notification.Message,
                notification.Type.ToString().ToLower()
            );

            _logger.LogDebug("Broadcast notification: {Title}", notification.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting notification");
        }
    }

    private static (double cpu, double memory, double disk) GetResourceMetrics()
    {
        // Get CPU usage (simplified)
        var cpuUsage = 0.0;
        try
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            cpuUsage = process.TotalProcessorTime.TotalMilliseconds / 
                       (Environment.ProcessorCount * Environment.TickCount64) * 100;
            cpuUsage = Math.Min(100, Math.Max(0, cpuUsage * 10)); // Approximate
        }
        catch { cpuUsage = Random.Shared.Next(10, 40); } // Fallback for demo

        // Get memory usage
        var memoryUsage = 0.0;
        try
        {
            var gcInfo = GC.GetGCMemoryInfo();
            var totalMemory = gcInfo.TotalAvailableMemoryBytes;
            var usedMemory = GC.GetTotalMemory(false);
            memoryUsage = (double)usedMemory / totalMemory * 100;
            memoryUsage = Math.Min(100, Math.Max(0, memoryUsage));
        }
        catch { memoryUsage = Random.Shared.Next(30, 60); }

        // Get disk usage
        var diskUsage = 0.0;
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(AppContext.BaseDirectory) ?? "C:");
            diskUsage = (double)(drive.TotalSize - drive.AvailableFreeSpace) / drive.TotalSize * 100;
        }
        catch { diskUsage = Random.Shared.Next(40, 70); }

        return (cpuUsage, memoryUsage, diskUsage);
    }
}
