using HoNfigurator.Core.Services;
using HoNfigurator.Core.Models;
using HoNfigurator.GameServer.Services;

namespace HoNfigurator.Api.Services;

/// <summary>
/// Adapter that wraps IGameServerManager to implement ICliGameServerManager interface
/// for use with CliCommandService
/// </summary>
public class CliGameServerManagerAdapter : ICliGameServerManager
{
    private readonly IGameServerManager _serverManager;

    public CliGameServerManagerAdapter(IGameServerManager serverManager)
    {
        _serverManager = serverManager;
    }

    public IEnumerable<ServerInfo> GetAllServers()
    {
        return _serverManager.Instances.Select(i => new ServerInfo
        {
            Port = i.Port,
            IsRunning = i.Status == ServerStatus.Ready || 
                       i.Status == ServerStatus.Occupied ||
                       i.Status == ServerStatus.Idle,
            PlayerCount = i.NumClients,
            ProcessId = i.ProcessId > 0 ? i.ProcessId : null,
            StartTime = i.StartTime
        });
    }

    public async Task<ServerStartResult> StartServerAsync(int? port = null)
    {
        try
        {
            // Find server by port or get next available
            var server = port.HasValue 
                ? _serverManager.Instances.FirstOrDefault(i => i.Port == port.Value)
                : _serverManager.Instances.FirstOrDefault(i => 
                    i.Status == ServerStatus.Offline || 
                    i.Status == ServerStatus.Unknown);

            if (server == null)
            {
                // Add a new server if none available
                var newId = _serverManager.AddNewServer();
                server = _serverManager.Instances.FirstOrDefault(i => i.Id == newId);
                
                if (server == null)
                {
                    return new ServerStartResult
                    {
                        Success = false,
                        Port = port ?? 0,
                        Error = "Failed to create new server"
                    };
                }
            }

            var result = await _serverManager.StartServerAsync(server.Id);
            return new ServerStartResult
            {
                Success = result != null,
                Port = result?.Port ?? server.Port,
                Error = result == null ? "Failed to start server" : null
            };
        }
        catch (Exception ex)
        {
            return new ServerStartResult
            {
                Success = false,
                Port = port ?? 0,
                Error = ex.Message
            };
        }
    }

    public async Task<bool> StopServerAsync(int port)
    {
        var server = _serverManager.Instances.FirstOrDefault(i => i.Port == port);
        if (server == null) return false;
        
        return await _serverManager.StopServerAsync(server.Id);
    }

    public async Task<ScaleResult> ScaleServersAsync(int targetCount)
    {
        var currentCount = _serverManager.Instances.Count(i => 
            i.Status == ServerStatus.Ready || 
            i.Status == ServerStatus.Occupied ||
            i.Status == ServerStatus.Idle);

        var result = new ScaleResult
        {
            PreviousCount = currentCount,
            Started = 0,
            Stopped = 0
        };

        if (targetCount > currentCount)
        {
            // Start more servers
            var toStart = targetCount - currentCount;
            for (int i = 0; i < toStart; i++)
            {
                var startResult = await StartServerAsync();
                if (startResult.Success) result.Started++;
            }
        }
        else if (targetCount < currentCount)
        {
            // Stop some servers (prefer empty ones)
            var toStop = currentCount - targetCount;
            var stoppableServers = _serverManager.Instances
                .Where(s => s.Status == ServerStatus.Ready || 
                           s.Status == ServerStatus.Idle)
                .OrderBy(s => s.NumClients)
                .Take(toStop)
                .ToList();

            foreach (var server in stoppableServers)
            {
                if (await _serverManager.StopServerAsync(server.Id))
                    result.Stopped++;
            }
        }

        result.CurrentCount = _serverManager.Instances.Count(i => 
            i.Status == ServerStatus.Ready || 
            i.Status == ServerStatus.Occupied ||
            i.Status == ServerStatus.Idle);

        return result;
    }
}
