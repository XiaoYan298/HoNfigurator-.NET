using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HoNfigurator.Core.Services;

/// <summary>
/// Manages scheduled tasks for server maintenance and automated operations
/// </summary>
public class ScheduledTasksService : IHostedService, IDisposable
{
    private readonly ILogger<ScheduledTasksService> _logger;
    private readonly List<ScheduledTask> _tasks = new();
    private readonly Dictionary<string, Timer> _timers = new();
    private bool _isRunning;

    public IReadOnlyList<ScheduledTask> Tasks => _tasks.AsReadOnly();
    public bool IsRunning => _isRunning;

    public ScheduledTasksService(ILogger<ScheduledTasksService> logger)
    {
        _logger = logger;
        // Add default tasks
        foreach (var task in GetDefaultTasks())
        {
            _tasks.Add(task);
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Stop();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Add a scheduled task
    /// </summary>
    public void AddTask(ScheduledTask task)
    {
        _tasks.Add(task);
        _logger.LogInformation("Added scheduled task: {TaskName} - {Schedule}", task.Name, task.Schedule);

        if (_isRunning)
        {
            StartTask(task);
        }
    }

    /// <summary>
    /// Remove a scheduled task
    /// </summary>
    public bool RemoveTask(string taskId)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == taskId);
        if (task == null) return false;

        StopTask(task);
        _tasks.Remove(task);
        _logger.LogInformation("Removed scheduled task: {TaskName}", task.Name);
        return true;
    }

    /// <summary>
    /// Get all tasks with their status
    /// </summary>
    public List<ScheduledTaskInfo> GetTasks()
    {
        return _tasks.Select(t => new ScheduledTaskInfo
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            Schedule = t.Schedule,
            IsEnabled = t.IsEnabled,
            LastRun = t.LastRun,
            NextRun = CalculateNextRun(t),
            RunCount = t.RunCount,
            TaskType = t.TaskType.ToString()
        }).ToList();
    }

    /// <summary>
    /// Start all scheduled tasks
    /// </summary>
    public void Start()
    {
        if (_isRunning) return;

        _isRunning = true;
        foreach (var task in _tasks)
        {
            StartTask(task);
        }
        _logger.LogInformation("Scheduled tasks service started with {Count} tasks", _tasks.Count);
    }

    /// <summary>
    /// Stop all scheduled tasks
    /// </summary>
    public void Stop()
    {
        if (!_isRunning) return;

        _isRunning = false;
        foreach (var timer in _timers.Values)
        {
            timer.Dispose();
        }
        _timers.Clear();
        _logger.LogInformation("Scheduled tasks service stopped");
    }

    /// <summary>
    /// Enable or disable a task
    /// </summary>
    public void SetTaskEnabled(string taskId, bool enabled)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == taskId || t.Name == taskId);
        if (task == null) return;

        task.IsEnabled = enabled;
        if (enabled && _isRunning)
        {
            StartTask(task);
        }
        else
        {
            StopTask(task);
        }
        _logger.LogInformation("Task {TaskName} {State}", task.Name, enabled ? "enabled" : "disabled");
    }

    /// <summary>
    /// Run a task immediately
    /// </summary>
    public async Task<bool> RunTaskNowAsync(string taskId)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == taskId || t.Name == taskId);
        if (task == null)
        {
            _logger.LogWarning("Task not found: {TaskId}", taskId);
            return false;
        }

        await ExecuteTaskAsync(task);
        return true;
    }

    /// <summary>
    /// Get default maintenance tasks
    /// </summary>
    public static List<ScheduledTask> GetDefaultTasks()
    {
        return new List<ScheduledTask>
        {
            new ScheduledTask
            {
                Id = "cleanup-logs",
                Name = "Cleanup Old Logs",
                Description = "Remove log files older than 7 days",
                Schedule = "0 3 * * *",
                TaskType = ScheduledTaskType.CleanupLogs,
                IsEnabled = true
            },
            new ScheduledTask
            {
                Id = "cleanup-replays",
                Name = "Cleanup Old Replays", 
                Description = "Remove replay files older than 30 days",
                Schedule = "0 4 * * 0",
                TaskType = ScheduledTaskType.CleanupReplays,
                IsEnabled = true
            },
            new ScheduledTask
            {
                Id = "health-check",
                Name = "Health Check",
                Description = "Run system health checks",
                Schedule = "*/5 * * * *",
                TaskType = ScheduledTaskType.HealthCheck,
                IsEnabled = true
            },
            new ScheduledTask
            {
                Id = "restart-idle",
                Name = "Restart Idle Servers",
                Description = "Restart servers that have been idle for too long",
                Schedule = "0 */6 * * *",
                TaskType = ScheduledTaskType.RestartIdle,
                IsEnabled = false
            }
        };
    }

    private DateTime CalculateNextRun(ScheduledTask task)
    {
        if (!task.IsEnabled) return DateTime.MinValue;
        var interval = ParseScheduleToInterval(task.Schedule);
        var lastRun = task.LastRun ?? DateTime.UtcNow;
        return lastRun.Add(interval);
    }

    private void StartTask(ScheduledTask task)
    {
        if (!task.IsEnabled) return;
        if (_timers.ContainsKey(task.Id)) return;

        var interval = ParseScheduleToInterval(task.Schedule);
        var timer = new Timer(async _ => await ExecuteTaskAsync(task), null, interval, interval);
        _timers[task.Id] = timer;

        _logger.LogDebug("Started timer for task: {TaskName}", task.Name);
    }

    private void StopTask(ScheduledTask task)
    {
        if (_timers.TryGetValue(task.Id, out var timer))
        {
            timer.Dispose();
            _timers.Remove(task.Id);
            _logger.LogDebug("Stopped timer for task: {TaskName}", task.Name);
        }
    }

    private async Task ExecuteTaskAsync(ScheduledTask task)
    {
        try
        {
            task.LastRun = DateTime.UtcNow;
            task.RunCount++;

            _logger.LogInformation("Executing scheduled task: {TaskName}", task.Name);

            switch (task.TaskType)
            {
                case ScheduledTaskType.CleanupLogs:
                    await CleanupLogsAsync(task.Parameters);
                    break;
                case ScheduledTaskType.CleanupReplays:
                    await CleanupReplaysAsync(task.Parameters);
                    break;
                case ScheduledTaskType.HealthCheck:
                    await RunHealthCheckAsync();
                    break;
                case ScheduledTaskType.RestartIdle:
                    await RestartIdleServersAsync();
                    break;
                case ScheduledTaskType.Custom:
                    await ExecuteCustomTaskAsync(task);
                    break;
            }

            _logger.LogInformation("Completed scheduled task: {TaskName}", task.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing scheduled task: {TaskName}", task.Name);
        }
    }

    private TimeSpan ParseScheduleToInterval(string schedule)
    {
        var parts = schedule.Split(' ');
        if (parts.Length < 5) return TimeSpan.FromHours(1);

        if (parts[0].StartsWith("*/"))
        {
            if (int.TryParse(parts[0][2..], out var minutes))
                return TimeSpan.FromMinutes(minutes);
        }
        
        if (parts[1].StartsWith("*/"))
        {
            if (int.TryParse(parts[1][2..], out var hours))
                return TimeSpan.FromHours(hours);
        }

        if (parts[0] == "0" && parts[1] != "*")
        {
            if (int.TryParse(parts[1], out var hour))
            {
                if (parts[4] != "*")
                    return TimeSpan.FromDays(7);
                return TimeSpan.FromDays(1);
            }
        }

        return TimeSpan.FromHours(1);
    }

    private Task CleanupLogsAsync(Dictionary<string, object>? parameters)
    {
        var logsDir = "logs";
        var daysToKeep = 7;
        if (parameters?.TryGetValue("daysToKeep", out var days) == true)
            daysToKeep = Convert.ToInt32(days);

        if (!Directory.Exists(logsDir)) return Task.CompletedTask;

        var cutoff = DateTime.UtcNow.AddDays(-daysToKeep);
        foreach (var file in Directory.GetFiles(logsDir, "*.log"))
        {
            if (File.GetLastWriteTimeUtc(file) < cutoff)
            {
                try { File.Delete(file); }
                catch { /* ignore */ }
            }
        }
        return Task.CompletedTask;
    }

    private Task CleanupReplaysAsync(Dictionary<string, object>? parameters)
    {
        var replaysDir = "replays";
        var daysToKeep = 30;
        if (parameters?.TryGetValue("daysToKeep", out var days) == true)
            daysToKeep = Convert.ToInt32(days);

        if (!Directory.Exists(replaysDir)) return Task.CompletedTask;

        var cutoff = DateTime.UtcNow.AddDays(-daysToKeep);
        foreach (var file in Directory.GetFiles(replaysDir, "*.honreplay"))
        {
            if (File.GetLastWriteTimeUtc(file) < cutoff)
            {
                try { File.Delete(file); }
                catch { /* ignore */ }
            }
        }
        return Task.CompletedTask;
    }

    private Task RunHealthCheckAsync()
    {
        _logger.LogDebug("Running scheduled health check");
        return Task.CompletedTask;
    }

    private Task RestartIdleServersAsync()
    {
        _logger.LogDebug("Checking for idle servers to restart");
        return Task.CompletedTask;
    }

    private Task ExecuteCustomTaskAsync(ScheduledTask task)
    {
        _logger.LogDebug("Executing custom task: {TaskName}", task.Name);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}

public class ScheduledTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Schedule { get; set; } = "0 * * * *";
    public ScheduledTaskType TaskType { get; set; } = ScheduledTaskType.Custom;
    public bool IsEnabled { get; set; } = true;
    public DateTime? LastRun { get; set; }
    public int RunCount { get; set; }
    public Dictionary<string, object>? Parameters { get; set; }
}

public class ScheduledTaskInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Schedule { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTime? LastRun { get; set; }
    public DateTime NextRun { get; set; }
    public int RunCount { get; set; }
    public string TaskType { get; set; } = string.Empty;
}

public enum ScheduledTaskType
{
    Custom,
    CleanupLogs,
    CleanupReplays,
    HealthCheck,
    RestartIdle
}
