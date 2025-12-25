using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HoNfigurator.Dashboard.Services;

/// <summary>
/// Service for communicating with HoNfigurator API
/// </summary>
public class ApiService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    
    public event EventHandler<string>? OnLog;
    public event EventHandler<string>? OnError;

    public ApiService(string baseUrl = "http://localhost:8080")
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public void SetBaseUrl(string baseUrl)
    {
        _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/'));
    }

    #region Health & Status

    public async Task<bool> CheckConnectionAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<ApiStatus?> GetStatusAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/status");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiStatus>();
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, $"Failed to get status: {ex.Message}");
        }
        return null;
    }

    #endregion

    #region Server Management

    public async Task<List<ServerInfo>?> GetServersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/servers");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ServersResponse>();
                return result?.Servers;
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, $"Failed to get servers: {ex.Message}");
        }
        return null;
    }

    public async Task<bool> StartServerAsync(int serverId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/servers/{serverId}/start", null);
            if (response.IsSuccessStatusCode)
            {
                OnLog?.Invoke(this, $"Server {serverId} start command sent");
                return true;
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, $"Failed to start server {serverId}: {ex.Message}");
        }
        return false;
    }

    public async Task<bool> StopServerAsync(int serverId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/servers/{serverId}/stop", null);
            if (response.IsSuccessStatusCode)
            {
                OnLog?.Invoke(this, $"Server {serverId} stop command sent");
                return true;
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, $"Failed to stop server {serverId}: {ex.Message}");
        }
        return false;
    }

    public async Task<bool> RestartServerAsync(int serverId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/servers/{serverId}/restart", null);
            if (response.IsSuccessStatusCode)
            {
                OnLog?.Invoke(this, $"Server {serverId} restart command sent");
                return true;
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, $"Failed to restart server {serverId}: {ex.Message}");
        }
        return false;
    }

    public async Task<bool> StartAllServersAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("/api/servers/start-all", null);
            if (response.IsSuccessStatusCode)
            {
                OnLog?.Invoke(this, "Start all servers command sent");
                return true;
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, $"Failed to start all servers: {ex.Message}");
        }
        return false;
    }

    public async Task<bool> StopAllServersAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("/api/servers/stop-all", null);
            if (response.IsSuccessStatusCode)
            {
                OnLog?.Invoke(this, "Stop all servers command sent");
                return true;
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, $"Failed to stop all servers: {ex.Message}");
        }
        return false;
    }

    public async Task<bool> RestartAllServersAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("/api/servers/restart-all", null);
            if (response.IsSuccessStatusCode)
            {
                OnLog?.Invoke(this, "Restart all servers command sent");
                return true;
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, $"Failed to restart all servers: {ex.Message}");
        }
        return false;
    }

    #endregion

    #region Configuration

    public async Task<ApiConfiguration?> GetConfigurationAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/config");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiConfiguration>();
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, $"Failed to get configuration: {ex.Message}");
        }
        return null;
    }

    public async Task<bool> SaveConfigurationAsync(ApiConfiguration config)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/config", config);
            if (response.IsSuccessStatusCode)
            {
                OnLog?.Invoke(this, "Configuration saved");
                return true;
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, $"Failed to save configuration: {ex.Message}");
        }
        return false;
    }

    #endregion

    #region Logs

    public async Task<List<string>?> GetLogsAsync(int serverId, int lines = 100)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/logs/{serverId}?lines={lines}");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LogsResponse>();
                return result?.Lines;
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, $"Failed to get logs: {ex.Message}");
        }
        return null;
    }

    #endregion

    #region Metrics

    public async Task<ApiMetrics?> GetMetricsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/metrics");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiMetrics>();
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, $"Failed to get metrics: {ex.Message}");
        }
        return null;
    }

    #endregion

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

#region API Models

public class ApiStatus
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";
    
    [JsonPropertyName("uptime")]
    public string Uptime { get; set; } = "";
    
    [JsonPropertyName("serverCount")]
    public int ServerCount { get; set; }
    
    [JsonPropertyName("runningServers")]
    public int RunningServers { get; set; }
    
    [JsonPropertyName("totalPlayers")]
    public int TotalPlayers { get; set; }
    
    [JsonPropertyName("chatServerConnected")]
    public bool ChatServerConnected { get; set; }
    
    [JsonPropertyName("masterServerConnected")]
    public bool MasterServerConnected { get; set; }
}

public class ServersResponse
{
    [JsonPropertyName("servers")]
    public List<ServerInfo> Servers { get; set; } = new();
    
    [JsonPropertyName("total")]
    public int Total { get; set; }
}

public class ServerInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";
    
    [JsonPropertyName("gamePort")]
    public int GamePort { get; set; }
    
    [JsonPropertyName("voicePort")]
    public int VoicePort { get; set; }
    
    [JsonPropertyName("players")]
    public int Players { get; set; }
    
    [JsonPropertyName("maxPlayers")]
    public int MaxPlayers { get; set; }
    
    [JsonPropertyName("map")]
    public string Map { get; set; } = "";
    
    [JsonPropertyName("uptime")]
    public string Uptime { get; set; } = "";
    
    [JsonPropertyName("matchId")]
    public long MatchId { get; set; }
}

public class ApiConfiguration
{
    [JsonPropertyName("honInstallDirectory")]
    public string HonInstallDirectory { get; set; } = "";
    
    [JsonPropertyName("honHomeDirectory")]
    public string HonHomeDirectory { get; set; } = "";
    
    [JsonPropertyName("serverName")]
    public string ServerName { get; set; } = "";
    
    [JsonPropertyName("login")]
    public string Login { get; set; } = "";
    
    [JsonPropertyName("totalServers")]
    public int TotalServers { get; set; }
    
    [JsonPropertyName("startingGamePort")]
    public int StartingGamePort { get; set; }
    
    [JsonPropertyName("startingVoicePort")]
    public int StartingVoicePort { get; set; }
    
    [JsonPropertyName("masterServer")]
    public string MasterServer { get; set; } = "";
    
    [JsonPropertyName("apiPort")]
    public int ApiPort { get; set; }
}

public class LogsResponse
{
    [JsonPropertyName("serverId")]
    public int ServerId { get; set; }
    
    [JsonPropertyName("lines")]
    public List<string> Lines { get; set; } = new();
}

public class ApiMetrics
{
    [JsonPropertyName("cpuUsage")]
    public double CpuUsage { get; set; }
    
    [JsonPropertyName("memoryUsageMb")]
    public double MemoryUsageMb { get; set; }
    
    [JsonPropertyName("totalPlayers")]
    public int TotalPlayers { get; set; }
    
    [JsonPropertyName("activeGames")]
    public int ActiveGames { get; set; }
    
    [JsonPropertyName("gamesToday")]
    public int GamesToday { get; set; }
}

#endregion
