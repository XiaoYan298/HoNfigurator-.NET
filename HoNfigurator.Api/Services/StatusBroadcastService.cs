using Microsoft.AspNetCore.SignalR;
using HoNfigurator.Api.Hubs;
using HoNfigurator.GameServer.Services;

namespace HoNfigurator.Api.Services;

public class StatusBroadcastService : BackgroundService
{
    private readonly IHubContext<DashboardHub, IDashboardClient> _hubContext;
    private readonly IGameServerManager _serverManager;
    private readonly ILogger<StatusBroadcastService> _logger;
    private readonly TimeSpan _broadcastInterval = TimeSpan.FromSeconds(5);

    public StatusBroadcastService(
        IHubContext<DashboardHub, IDashboardClient> hubContext,
        IGameServerManager serverManager,
        ILogger<StatusBroadcastService> logger)
    {
        _hubContext = hubContext;
        _serverManager = serverManager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Status broadcast service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var status = _serverManager.GetStatus();
                await _hubContext.Clients.All.ReceiveStatus(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting status");
            }

            await Task.Delay(_broadcastInterval, stoppingToken);
        }
    }
}
