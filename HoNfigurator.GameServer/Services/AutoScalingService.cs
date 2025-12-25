using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HoNfigurator.Core.Models;

namespace HoNfigurator.GameServer.Services;

/// <summary>
/// Service that automatically scales game servers based on load
/// </summary>
public class AutoScalingService : BackgroundService
{
    private readonly ILogger<AutoScalingService> _logger;
    private readonly IGameServerManager _serverManager;
    private readonly HoNConfiguration _config;
    private readonly AutoScalingSettings _settings;
    private DateTime _lastScaleOperation = DateTime.MinValue;
    private int _consecutiveScaleUpChecks;
    private int _consecutiveScaleDownChecks;

    public bool IsEnabled => _settings.Enabled;
    public DateTime LastScaleTime => _lastScaleOperation;
    public string LastScaleAction { get; private set; } = "None";

    public AutoScalingService(
        ILogger<AutoScalingService> logger,
        IGameServerManager serverManager,
        HoNConfiguration config)
    {
        _logger = logger;
        _serverManager = serverManager;
        _config = config;
        
        // Load settings from config or use defaults
        _settings = config.ApplicationData?.AutoScaling ?? new AutoScalingSettings();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Auto-scaling is disabled");
            return;
        }

        _logger.LogInformation("Auto-scaling service started (Min={Min}, Max={Max}, Interval={Interval}s)",
            _settings.MinServers, _settings.MaxServers, _settings.CheckIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndScaleAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during auto-scaling check");
            }

            await Task.Delay(TimeSpan.FromSeconds(_settings.CheckIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("Auto-scaling service stopped");
    }

    private async Task CheckAndScaleAsync()
    {
        var instances = _serverManager.Instances.ToList();
        var totalServers = instances.Count;
        var occupiedServers = instances.Count(s => s.Status == ServerStatus.Occupied);
        var readyServers = instances.Count(s => s.Status == ServerStatus.Ready);
        var startingServers = instances.Count(s => s.Status == ServerStatus.Starting);
        
        if (totalServers == 0)
        {
            _logger.LogDebug("No servers to scale");
            return;
        }

        var occupancyRate = (double)occupiedServers / totalServers * 100;
        
        _logger.LogDebug("Auto-scaling check: Total={Total}, Occupied={Occupied}, Ready={Ready}, Starting={Starting}, Rate={Rate:F1}%",
            totalServers, occupiedServers, readyServers, startingServers, occupancyRate);

        // Check if we're in cooldown
        if (DateTime.UtcNow - _lastScaleOperation < TimeSpan.FromSeconds(_settings.CooldownSeconds))
        {
            _logger.LogDebug("In cooldown period, skipping scale check");
            return;
        }

        // Don't scale while servers are starting
        if (startingServers > 0)
        {
            _logger.LogDebug("Servers are starting, skipping scale check");
            return;
        }

        // Scale up check
        if (ShouldScaleUp(totalServers, occupancyRate, readyServers))
        {
            _consecutiveScaleUpChecks++;
            _consecutiveScaleDownChecks = 0;
            
            // Require 3 consecutive checks before scaling up
            if (_consecutiveScaleUpChecks >= 3)
            {
                await ScaleUpAsync();
                _consecutiveScaleUpChecks = 0;
            }
            else
            {
                _logger.LogDebug("Scale up check {Count}/3", _consecutiveScaleUpChecks);
            }
        }
        // Scale down check
        else if (ShouldScaleDown(totalServers, occupancyRate, readyServers))
        {
            _consecutiveScaleDownChecks++;
            _consecutiveScaleUpChecks = 0;
            
            // Require 5 consecutive checks before scaling down (more conservative)
            if (_consecutiveScaleDownChecks >= 5)
            {
                await ScaleDownAsync(instances, readyServers);
                _consecutiveScaleDownChecks = 0;
            }
            else
            {
                _logger.LogDebug("Scale down check {Count}/5", _consecutiveScaleDownChecks);
            }
        }
        else
        {
            _consecutiveScaleUpChecks = 0;
            _consecutiveScaleDownChecks = 0;
        }
    }

    private bool ShouldScaleUp(int totalServers, double occupancyRate, int readyServers)
    {
        // Don't scale up if at max
        if (totalServers >= _settings.MaxServers)
            return false;

        // Scale up if occupancy rate is high
        if (occupancyRate >= _settings.ScaleUpThreshold)
            return true;

        // Scale up if not enough ready servers
        if (readyServers < _settings.MinReadyServers)
            return true;

        return false;
    }

    private bool ShouldScaleDown(int totalServers, double occupancyRate, int readyServers)
    {
        // Don't scale down below minimum
        if (totalServers <= _settings.MinServers)
            return false;

        // Don't scale down if we'd go below minimum ready servers
        if (readyServers <= _settings.MinReadyServers)
            return false;

        // Scale down if occupancy rate is low
        if (occupancyRate <= _settings.ScaleDownThreshold)
            return true;

        return false;
    }

    private async Task ScaleUpAsync()
    {
        try
        {
            // Add a new server
            var newId = _serverManager.AddNewServer();
            
            if (newId > 0)
            {
                _logger.LogInformation("Auto-scaling: Adding server #{Id}", newId);
                
                // Start the new server
                await _serverManager.StartServerAsync(newId);
                
                _lastScaleOperation = DateTime.UtcNow;
                LastScaleAction = $"Scale Up: Added server #{newId}";
                
                _logger.LogInformation("Auto-scaling: Server #{Id} started", newId);
            }
            else
            {
                _logger.LogWarning("Auto-scaling: Failed to add server");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto-scaling: Error scaling up");
        }
    }

    private async Task ScaleDownAsync(List<GameServerInstance> instances, int readyServers)
    {
        try
        {
            // Find a ready server to remove (prefer the one with highest ID)
            var serverToRemove = instances
                .Where(s => s.Status == ServerStatus.Ready)
                .OrderByDescending(s => s.Id)
                .FirstOrDefault();

            if (serverToRemove == null)
            {
                _logger.LogDebug("Auto-scaling: No ready servers to remove");
                return;
            }

            // Don't remove if this would leave us with no ready servers
            if (readyServers <= _settings.MinReadyServers)
            {
                _logger.LogDebug("Auto-scaling: Cannot remove server, would leave no ready servers");
                return;
            }

            _logger.LogInformation("Auto-scaling: Removing server #{Id}", serverToRemove.Id);
            
            // Stop and remove the server
            await _serverManager.StopServerAsync(serverToRemove.Id, graceful: true);
            _serverManager.RemoveServer(serverToRemove.Id);
            
            _lastScaleOperation = DateTime.UtcNow;
            LastScaleAction = $"Scale Down: Removed server #{serverToRemove.Id}";
            
            _logger.LogInformation("Auto-scaling: Server #{Id} removed", serverToRemove.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto-scaling: Error scaling down");
        }
    }

    /// <summary>
    /// Get current auto-scaling status
    /// </summary>
    public AutoScalingStatus GetStatus()
    {
        var instances = _serverManager.Instances.ToList();
        var totalServers = instances.Count;
        var occupiedServers = instances.Count(s => s.Status == ServerStatus.Occupied);
        var readyServers = instances.Count(s => s.Status == ServerStatus.Ready);
        
        return new AutoScalingStatus
        {
            Enabled = _settings.Enabled,
            TotalServers = totalServers,
            OccupiedServers = occupiedServers,
            ReadyServers = readyServers,
            OccupancyRate = totalServers > 0 ? (double)occupiedServers / totalServers * 100 : 0,
            MinServers = _settings.MinServers,
            MaxServers = _settings.MaxServers,
            ScaleUpThreshold = _settings.ScaleUpThreshold,
            ScaleDownThreshold = _settings.ScaleDownThreshold,
            LastScaleTime = _lastScaleOperation,
            LastScaleAction = LastScaleAction,
            InCooldown = DateTime.UtcNow - _lastScaleOperation < TimeSpan.FromSeconds(_settings.CooldownSeconds),
            CooldownRemainingSeconds = Math.Max(0, _settings.CooldownSeconds - (int)(DateTime.UtcNow - _lastScaleOperation).TotalSeconds)
        };
    }

    /// <summary>
    /// Manually trigger scale up
    /// </summary>
    public async Task<bool> ManualScaleUpAsync()
    {
        if (_serverManager.Instances.Count() >= _settings.MaxServers)
        {
            _logger.LogWarning("Cannot scale up: already at max servers");
            return false;
        }
        
        await ScaleUpAsync();
        return true;
    }

    /// <summary>
    /// Manually trigger scale down
    /// </summary>
    public async Task<bool> ManualScaleDownAsync()
    {
        var instances = _serverManager.Instances.ToList();
        if (instances.Count <= _settings.MinServers)
        {
            _logger.LogWarning("Cannot scale down: already at min servers");
            return false;
        }
        
        var readyServers = instances.Count(s => s.Status == ServerStatus.Ready);
        await ScaleDownAsync(instances, readyServers);
        return true;
    }
}

/// <summary>
/// Auto-scaling status information
/// </summary>
public class AutoScalingStatus
{
    public bool Enabled { get; set; }
    public int TotalServers { get; set; }
    public int OccupiedServers { get; set; }
    public int ReadyServers { get; set; }
    public double OccupancyRate { get; set; }
    public int MinServers { get; set; }
    public int MaxServers { get; set; }
    public int ScaleUpThreshold { get; set; }
    public int ScaleDownThreshold { get; set; }
    public DateTime LastScaleTime { get; set; }
    public string LastScaleAction { get; set; } = "";
    public bool InCooldown { get; set; }
    public int CooldownRemainingSeconds { get; set; }
}
