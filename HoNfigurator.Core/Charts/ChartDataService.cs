using System.Collections.Concurrent;

namespace HoNfigurator.Core.Charts;

/// <summary>
/// Time-series data point for charts
/// </summary>
public record ChartDataPoint
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public double Value { get; init; }
    public string? Label { get; init; }
}

/// <summary>
/// Server uptime record
/// </summary>
public record UptimeRecord
{
    public int ServerId { get; init; }
    public string ServerName { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public DateTime? EndTime { get; init; }
    public TimeSpan Duration => (EndTime ?? DateTime.UtcNow) - StartTime;
    public string EndReason { get; init; } = "Running"; // Running, Stopped, Crashed, Restarted
}

/// <summary>
/// Match statistics
/// </summary>
public record MatchStats
{
    public int MatchId { get; init; }
    public int ServerId { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime? EndTime { get; init; }
    public TimeSpan Duration => EndTime.HasValue ? EndTime.Value - StartTime : TimeSpan.Zero;
    public int PlayerCount { get; init; }
    public string GameMode { get; init; } = "Normal";
    public string Map { get; init; } = "caldavar";
    public string? Winner { get; init; } // "Legion", "Hellbourne", "Draw"
}

/// <summary>
/// Player count snapshot
/// </summary>
public record PlayerCountSnapshot
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public int TotalPlayers { get; init; }
    public int ActiveGames { get; init; }
    public Dictionary<int, int> PlayersByServer { get; init; } = new();
}

/// <summary>
/// Service for managing chart data
/// </summary>
public interface IChartDataService
{
    // Uptime tracking
    void RecordServerStart(int serverId, string serverName);
    void RecordServerStop(int serverId, string reason);
    IReadOnlyList<UptimeRecord> GetUptimeHistory(int? serverId = null, int days = 7);
    double GetUptimePercentage(int serverId, int hours = 24);
    Dictionary<int, double> GetAllServersUptime(int hours = 24);
    
    // Player count tracking
    void RecordPlayerCount(int serverId, int playerCount);
    IReadOnlyList<PlayerCountSnapshot> GetPlayerCountHistory(int hours = 24);
    IReadOnlyList<ChartDataPoint> GetPlayerCountSeries(int hours = 24);
    
    // Match statistics
    void RecordMatchStart(int matchId, int serverId, int playerCount, string gameMode, string map);
    void RecordMatchEnd(int matchId, string? winner);
    IReadOnlyList<MatchStats> GetMatchHistory(int? serverId = null, int days = 7);
    MatchStatsSummary GetMatchStatsSummary(int days = 7);
    
    // Resource metrics series (for charts)
    void RecordResourceMetrics(double cpuPercent, double memoryPercent, double diskPercent);
    IReadOnlyList<ChartDataPoint> GetCpuSeries(int hours = 1);
    IReadOnlyList<ChartDataPoint> GetMemorySeries(int hours = 1);
    IReadOnlyList<ChartDataPoint> GetDiskSeries(int hours = 1);
}

public record MatchStatsSummary
{
    public int TotalMatches { get; init; }
    public int TotalPlayers { get; init; }
    public TimeSpan AverageMatchDuration { get; init; }
    public TimeSpan TotalPlayTime { get; init; }
    public int LegionWins { get; init; }
    public int HellbourneWins { get; init; }
    public int Draws { get; init; }
    public Dictionary<string, int> MatchesByGameMode { get; init; } = new();
    public Dictionary<int, int> MatchesByServer { get; init; } = new();
    public List<ChartDataPoint> MatchesPerDay { get; init; } = new();
}

public class ChartDataService : IChartDataService
{
    private readonly ConcurrentQueue<UptimeRecord> _uptimeRecords = new();
    private readonly ConcurrentDictionary<int, UptimeRecord> _currentUptime = new();
    private readonly ConcurrentQueue<PlayerCountSnapshot> _playerCounts = new();
    private readonly ConcurrentQueue<MatchStats> _matchHistory = new();
    private readonly ConcurrentDictionary<int, MatchStats> _activeMatches = new();
    private readonly ConcurrentQueue<(DateTime, double, double, double)> _resourceMetrics = new();
    
    private const int MaxRecords = 10000;
    private const int MaxMetricPoints = 3600; // 1 hour of per-second data

    #region Uptime Tracking

    public void RecordServerStart(int serverId, string serverName)
    {
        var record = new UptimeRecord
        {
            ServerId = serverId,
            ServerName = serverName,
            StartTime = DateTime.UtcNow,
            EndReason = "Running"
        };
        
        _currentUptime[serverId] = record;
    }

    public void RecordServerStop(int serverId, string reason)
    {
        if (_currentUptime.TryRemove(serverId, out var current))
        {
            var completed = current with
            {
                EndTime = DateTime.UtcNow,
                EndReason = reason
            };
            
            _uptimeRecords.Enqueue(completed);
            TrimQueue(_uptimeRecords, MaxRecords);
        }
    }

    public IReadOnlyList<UptimeRecord> GetUptimeHistory(int? serverId = null, int days = 7)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);
        var query = _uptimeRecords.Where(r => r.StartTime >= cutoff);
        
        if (serverId.HasValue)
        {
            query = query.Where(r => r.ServerId == serverId.Value);
        }
        
        // Include current running servers
        var current = _currentUptime.Values
            .Where(r => !serverId.HasValue || r.ServerId == serverId.Value);
        
        return query.Concat(current).OrderByDescending(r => r.StartTime).ToList();
    }

    public double GetUptimePercentage(int serverId, int hours = 24)
    {
        var cutoff = DateTime.UtcNow.AddHours(-hours);
        var totalSeconds = hours * 3600.0;
        
        var records = _uptimeRecords
            .Where(r => r.ServerId == serverId && r.EndTime >= cutoff)
            .ToList();
        
        // Add current uptime if server is running
        if (_currentUptime.TryGetValue(serverId, out var current))
        {
            records.Add(current);
        }
        
        var uptimeSeconds = records.Sum(r =>
        {
            var start = r.StartTime < cutoff ? cutoff : r.StartTime;
            var end = r.EndTime ?? DateTime.UtcNow;
            return (end - start).TotalSeconds;
        });
        
        return Math.Min(100, (uptimeSeconds / totalSeconds) * 100);
    }

    public Dictionary<int, double> GetAllServersUptime(int hours = 24)
    {
        var serverIds = _uptimeRecords.Select(r => r.ServerId)
            .Concat(_currentUptime.Keys)
            .Distinct();
        
        return serverIds.ToDictionary(
            id => id,
            id => GetUptimePercentage(id, hours)
        );
    }

    #endregion

    #region Player Count Tracking

    public void RecordPlayerCount(int serverId, int playerCount)
    {
        var lastSnapshot = _playerCounts.LastOrDefault();
        var playersByServer = lastSnapshot?.PlayersByServer.ToDictionary(k => k.Key, v => v.Value) 
            ?? new Dictionary<int, int>();
        
        playersByServer[serverId] = playerCount;
        
        var snapshot = new PlayerCountSnapshot
        {
            Timestamp = DateTime.UtcNow,
            TotalPlayers = playersByServer.Values.Sum(),
            ActiveGames = playersByServer.Count(p => p.Value > 0),
            PlayersByServer = playersByServer
        };
        
        _playerCounts.Enqueue(snapshot);
        TrimQueue(_playerCounts, MaxRecords);
    }

    public IReadOnlyList<PlayerCountSnapshot> GetPlayerCountHistory(int hours = 24)
    {
        var cutoff = DateTime.UtcNow.AddHours(-hours);
        return _playerCounts.Where(p => p.Timestamp >= cutoff).ToList();
    }

    public IReadOnlyList<ChartDataPoint> GetPlayerCountSeries(int hours = 24)
    {
        var cutoff = DateTime.UtcNow.AddHours(-hours);
        return _playerCounts
            .Where(p => p.Timestamp >= cutoff)
            .Select(p => new ChartDataPoint
            {
                Timestamp = p.Timestamp,
                Value = p.TotalPlayers
            })
            .ToList();
    }

    #endregion

    #region Match Statistics

    public void RecordMatchStart(int matchId, int serverId, int playerCount, string gameMode, string map)
    {
        var match = new MatchStats
        {
            MatchId = matchId,
            ServerId = serverId,
            StartTime = DateTime.UtcNow,
            PlayerCount = playerCount,
            GameMode = gameMode,
            Map = map
        };
        
        _activeMatches[matchId] = match;
    }

    public void RecordMatchEnd(int matchId, string? winner)
    {
        if (_activeMatches.TryRemove(matchId, out var match))
        {
            var completed = match with
            {
                EndTime = DateTime.UtcNow,
                Winner = winner
            };
            
            _matchHistory.Enqueue(completed);
            TrimQueue(_matchHistory, MaxRecords);
        }
    }

    public IReadOnlyList<MatchStats> GetMatchHistory(int? serverId = null, int days = 7)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);
        var query = _matchHistory.Where(m => m.StartTime >= cutoff);
        
        if (serverId.HasValue)
        {
            query = query.Where(m => m.ServerId == serverId.Value);
        }
        
        return query.OrderByDescending(m => m.StartTime).ToList();
    }

    public MatchStatsSummary GetMatchStatsSummary(int days = 7)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);
        var matches = _matchHistory.Where(m => m.StartTime >= cutoff).ToList();
        
        var totalDuration = TimeSpan.FromSeconds(matches.Sum(m => m.Duration.TotalSeconds));
        var avgDuration = matches.Count > 0 
            ? TimeSpan.FromSeconds(matches.Average(m => m.Duration.TotalSeconds))
            : TimeSpan.Zero;
        
        // Matches per day for chart
        var matchesPerDay = matches
            .GroupBy(m => m.StartTime.Date)
            .Select(g => new ChartDataPoint
            {
                Timestamp = g.Key,
                Value = g.Count(),
                Label = g.Key.ToString("MM/dd")
            })
            .OrderBy(p => p.Timestamp)
            .ToList();
        
        return new MatchStatsSummary
        {
            TotalMatches = matches.Count,
            TotalPlayers = matches.Sum(m => m.PlayerCount),
            AverageMatchDuration = avgDuration,
            TotalPlayTime = totalDuration,
            LegionWins = matches.Count(m => m.Winner == "Legion"),
            HellbourneWins = matches.Count(m => m.Winner == "Hellbourne"),
            Draws = matches.Count(m => m.Winner == "Draw"),
            MatchesByGameMode = matches.GroupBy(m => m.GameMode)
                .ToDictionary(g => g.Key, g => g.Count()),
            MatchesByServer = matches.GroupBy(m => m.ServerId)
                .ToDictionary(g => g.Key, g => g.Count()),
            MatchesPerDay = matchesPerDay
        };
    }

    #endregion

    #region Resource Metrics

    public void RecordResourceMetrics(double cpuPercent, double memoryPercent, double diskPercent)
    {
        _resourceMetrics.Enqueue((DateTime.UtcNow, cpuPercent, memoryPercent, diskPercent));
        TrimQueue(_resourceMetrics, MaxMetricPoints);
    }

    public IReadOnlyList<ChartDataPoint> GetCpuSeries(int hours = 1)
    {
        var cutoff = DateTime.UtcNow.AddHours(-hours);
        return _resourceMetrics
            .Where(m => m.Item1 >= cutoff)
            .Select(m => new ChartDataPoint { Timestamp = m.Item1, Value = m.Item2 })
            .ToList();
    }

    public IReadOnlyList<ChartDataPoint> GetMemorySeries(int hours = 1)
    {
        var cutoff = DateTime.UtcNow.AddHours(-hours);
        return _resourceMetrics
            .Where(m => m.Item1 >= cutoff)
            .Select(m => new ChartDataPoint { Timestamp = m.Item1, Value = m.Item3 })
            .ToList();
    }

    public IReadOnlyList<ChartDataPoint> GetDiskSeries(int hours = 1)
    {
        var cutoff = DateTime.UtcNow.AddHours(-hours);
        return _resourceMetrics
            .Where(m => m.Item1 >= cutoff)
            .Select(m => new ChartDataPoint { Timestamp = m.Item1, Value = m.Item4 })
            .ToList();
    }

    #endregion

    private static void TrimQueue<T>(ConcurrentQueue<T> queue, int maxSize)
    {
        while (queue.Count > maxSize && queue.TryDequeue(out _)) { }
    }
}
