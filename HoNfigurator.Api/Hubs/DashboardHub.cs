using Microsoft.AspNetCore.SignalR;
using HoNfigurator.Core.Models;
using HoNfigurator.Core.Notifications;
using HoNfigurator.Core.Charts;
using HoNfigurator.GameServer.Services;

namespace HoNfigurator.Api.Hubs;

public interface IDashboardClient
{
    Task ReceiveStatus(ServerStatusResponse status);
    Task ReceiveServerUpdate(GameServerInstance instance);
    Task ReceiveLog(string message, string level);
    Task ReceiveNotification(string title, string message, string type);
    Task ReceiveCommandResult(CommandResult result);
    Task ReceiveLogUpdate(string[] lines);
    Task ReceiveAlert(Notification notification);
    Task ReceiveChartUpdate(string chartType, object data);
}

public record CommandResult
{
    public bool Success { get; init; }
    public string[] Output { get; init; } = Array.Empty<string>();
}

public class DashboardHub : Hub<IDashboardClient>
{
    private readonly IGameServerManager _serverManager;
    private readonly ILogger<DashboardHub> _logger;

    public DashboardHub(IGameServerManager serverManager, ILogger<DashboardHub> logger)
    {
        _serverManager = serverManager;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        var status = _serverManager.GetStatus();
        await Clients.Caller.ReceiveStatus(status);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task RequestStatus()
    {
        var status = _serverManager.GetStatus();
        await Clients.Caller.ReceiveStatus(status);
    }

    public async Task StartServer(int id)
    {
        _logger.LogInformation("Client requested to start server {Id}", id);
        var instance = await _serverManager.StartServerAsync(id);
        if (instance != null)
        {
            await Clients.All.ReceiveServerUpdate(instance);
            await Clients.Caller.ReceiveNotification("Server Started", string.Format("Server #{0} is starting...", id), "success");
        }
    }

    public async Task StopServer(int id)
    {
        _logger.LogInformation("Client requested to stop server {Id}", id);
        var success = await _serverManager.StopServerAsync(id);
        if (success)
        {
            var status = _serverManager.GetStatus();
            var instance = status.Instances.FirstOrDefault(i => i.Id == id);
            if (instance != null)
            {
                await Clients.All.ReceiveServerUpdate(instance);
            }
            await Clients.Caller.ReceiveNotification("Server Stopped", string.Format("Server #{0} has been stopped", id), "info");
        }
    }

    public async Task RestartServer(int id)
    {
        _logger.LogInformation("Client requested to restart server {Id}", id);
        await Clients.Caller.ReceiveNotification("Restarting", string.Format("Server #{0} is restarting...", id), "warning");
        await _serverManager.RestartServerAsync(id);
        var status = _serverManager.GetStatus();
        var instance = status.Instances.FirstOrDefault(i => i.Id == id);
        if (instance != null)
        {
            await Clients.All.ReceiveServerUpdate(instance);
            await Clients.Caller.ReceiveNotification("Server Restarted", string.Format("Server #{0} has been restarted", id), "success");
        }
    }

    public async Task StartAllServers()
    {
        _logger.LogInformation("Client requested to start all servers");
        await Clients.Caller.ReceiveNotification("Starting All", "Starting all servers...", "info");
        await _serverManager.StartAllServersAsync();
        var status = _serverManager.GetStatus();
        await Clients.All.ReceiveStatus(status);
        await Clients.Caller.ReceiveNotification("All Started", "All servers have been started", "success");
    }

    public async Task StopAllServers()
    {
        _logger.LogInformation("Client requested to stop all servers");
        await Clients.Caller.ReceiveNotification("Stopping All", "Stopping all servers...", "warning");
        await _serverManager.StopAllServersAsync();
        var status = _serverManager.GetStatus();
        await Clients.All.ReceiveStatus(status);
        await Clients.Caller.ReceiveNotification("All Stopped", "All servers have been stopped", "info");
    }

    public async Task RestartAllServers()
    {
        _logger.LogInformation("Client requested to restart all servers");
        await Clients.Caller.ReceiveNotification("Restarting All", "Restarting all servers...", "warning");
        await _serverManager.StopAllServersAsync();
        await Task.Delay(1000);
        await _serverManager.StartAllServersAsync();
        var status = _serverManager.GetStatus();
        await Clients.All.ReceiveStatus(status);
        await Clients.Caller.ReceiveNotification("All Restarted", "All servers have been restarted", "success");
    }

    public async Task AddServer()
    {
        _logger.LogInformation("Client requested to add a server");
        // In a real implementation, this would add a new server instance
        await Clients.Caller.ReceiveNotification("Add Server", "Server addition requires configuration update", "info");
        var result = new CommandResult
        {
            Success = true,
            Output = new[] { "Server addition is configured through config/config.json", "Increase total_servers and restart the application" }
        };
        await Clients.Caller.ReceiveCommandResult(result);
    }

    public async Task ExecuteCommand(string command)
    {
        _logger.LogInformation("Client executing command: {Command}", command);
        var output = new List<string>();
        var cmd = command?.ToLower().Trim() ?? "";

        if (cmd == "help")
        {
            output.Add("Available commands:");
            output.Add("  status      - Show server status");
            output.Add("  list        - List all server instances");
            output.Add("  startup N   - Start server N (or 'all')");
            output.Add("  shutdown N  - Stop server N (or 'all')");
            output.Add("  restart N   - Restart server N (or 'all')");
            output.Add("  add         - Add a new server instance");
            output.Add("  config      - Show configuration path");
            output.Add("  version     - Show version info");
            output.Add("  help        - Show this help");
        }
        else if (cmd == "status")
        {
            var status = _serverManager.GetStatus();
            output.Add(string.Format("Server Name: {0}", status.ServerName));
            output.Add(string.Format("Version: {0}", status.Version));
            output.Add(string.Format("Total Servers: {0}", status.TotalServers));
            output.Add(string.Format("Online Servers: {0}", status.OnlineServers));
            output.Add(string.Format("Total Players: {0}", status.TotalPlayers));
            output.Add(string.Format("Master Server: {0}", status.MasterServerConnected ? "Connected" : "Disconnected"));
            output.Add(string.Format("Chat Server: {0}", status.ChatServerConnected ? "Connected" : "Disconnected"));
            output.Add(string.Format("CPU: {0}%", status.SystemStats?.CpuUsagePercent ?? 0));
            output.Add(string.Format("Memory: {0} MB", status.SystemStats?.UsedMemoryMb ?? 0));
        }
        else if (cmd == "list")
        {
            var status = _serverManager.GetStatus();
            output.Add("Server Instances:");
            output.Add("-------------------");
            foreach (var instance in status.Instances)
            {
                output.Add(string.Format("  #{0} | {1} | Port {2} | {3}/{4} players", 
                    instance.Id, instance.StatusString, instance.Port, instance.NumClients, instance.MaxClients));
            }
        }
        else if (cmd == "startup all")
        {
            await StartAllServers();
            output.Add("Starting all servers...");
        }
        else if (cmd.StartsWith("startup ") && int.TryParse(cmd.Substring(8).Trim(), out int startId))
        {
            await StartServer(startId);
            output.Add(string.Format("Starting server #{0}...", startId));
        }
        else if (cmd == "shutdown all")
        {
            await StopAllServers();
            output.Add("Stopping all servers...");
        }
        else if (cmd.StartsWith("shutdown ") && int.TryParse(cmd.Substring(9).Trim(), out int stopId))
        {
            await StopServer(stopId);
            output.Add(string.Format("Stopping server #{0}...", stopId));
        }
        else if (cmd == "restart all")
        {
            await RestartAllServers();
            output.Add("Restarting all servers...");
        }
        else if (cmd.StartsWith("restart ") && int.TryParse(cmd.Substring(8).Trim(), out int restartId))
        {
            await RestartServer(restartId);
            output.Add(string.Format("Restarting server #{0}...", restartId));
        }
        else if (cmd == "config")
        {
            output.Add("Configuration: config/config.json");
            output.Add("Use the Config tab in the dashboard to edit.");
        }
        else if (cmd == "version")
        {
            output.Add("HoNfigurator .NET 10");
            output.Add("Version: 1.0.0-dotnet");
            output.Add("Runtime: .NET 10.0");
        }
        else if (cmd == "add")
        {
            await AddServer();
            output.Add("See notification for details.");
        }
        else if (!string.IsNullOrWhiteSpace(cmd))
        {
            output.Add(string.Format("Unknown command: {0}", cmd));
            output.Add("Type 'help' for available commands.");
        }

        var result = new CommandResult { Success = true, Output = output.ToArray() };
        await Clients.Caller.ReceiveCommandResult(result);
    }

    public async Task SendMessage(int serverId, string message)
    {
        _logger.LogInformation("Sending message to server {Id}: {Message}", serverId, message);
        // In real implementation, this would send to the game server
        await Clients.Caller.ReceiveNotification("Message Sent", 
            string.Format("Message sent to server #{0}: {1}", serverId, message), "success");
    }
}
