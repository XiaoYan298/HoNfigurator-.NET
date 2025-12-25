namespace HoNfigurator.Core.Metrics;

public class AdvancedMetricsService
{
    private readonly Dictionary<int, ServerMetricsHistory> _serverMetrics = new();
    private readonly SystemMetricsHistory _systemMetrics = new();
    private readonly object _lock = new();
    private const int MaxHistoryPoints = 1000;

    public void RecordServerMetrics(int serverId, ServerMetricsSnapshot snapshot)
    {
        lock (_lock)
        {
            if (!_serverMetrics.TryGetValue(serverId, out var history))
            {
                history = new ServerMetricsHistory { ServerId = serverId };
                _serverMetrics[serverId] = history;
            }
            history.AddSnapshot(snapshot);
        }
    }

    public void RecordSystemMetrics(SystemMetricsSnapshot snapshot)
    {
        lock (_lock) { _systemMetrics.AddSnapshot(snapshot); }
    }

    public ServerMetricsHistory? GetServerMetrics(int serverId, int points = 100)
    {
        lock (_lock)
        {
            if (!_serverMetrics.TryGetValue(serverId, out var history)) return null;
            return history.GetRecent(points);
        }
    }

    public SystemMetricsHistory GetSystemMetrics(int points = 100)
    {
        lock (_lock) { return _systemMetrics.GetRecent(points); }
    }

    public Dictionary<int, ServerMetricsSummary> GetAllServersSummary()
    {
        lock (_lock)
        {
            return _serverMetrics.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.GetSummary()
            );
        }
    }

    public MetricsComparison CompareServers(int[] serverIds, TimeSpan period)
    {
        lock (_lock)
        {
            var since = DateTime.UtcNow - period;
            var comparison = new MetricsComparison { Period = period, GeneratedAt = DateTime.UtcNow };
            foreach (var serverId in serverIds)
            {
                if (_serverMetrics.TryGetValue(serverId, out var history))
                {
                    var filtered = history.GetSince(since);
                    if (filtered.Snapshots.Count > 0)
                    {
                        comparison.Servers[serverId] = new ServerComparisonData
                        {
                            AverageCpu = filtered.Snapshots.Average(s => s.CpuPercent),
                            AverageMemory = filtered.Snapshots.Average(s => s.MemoryMb),
                            MaxCpu = filtered.Snapshots.Max(s => s.CpuPercent),
                            MaxMemory = filtered.Snapshots.Max(s => s.MemoryMb),
                            TotalMatches = filtered.Snapshots.Count(s => s.MatchId.HasValue),
                            AverageUptime = filtered.Snapshots.Average(s => s.UptimeSeconds)
                        };
                    }
                }
            }
            return comparison;
        }
    }
}

public class ServerMetricsHistory
{
    public int ServerId { get; set; }
    public List<ServerMetricsSnapshot> Snapshots { get; set; } = new();
    private const int MaxPoints = 1000;

    public void AddSnapshot(ServerMetricsSnapshot snapshot)
    {
        Snapshots.Add(snapshot);
        if (Snapshots.Count > MaxPoints) Snapshots.RemoveAt(0);
    }

    public ServerMetricsHistory GetRecent(int count)
    {
        return new ServerMetricsHistory { ServerId = ServerId, Snapshots = Snapshots.TakeLast(count).ToList() };
    }

    public ServerMetricsHistory GetSince(DateTime since)
    {
        return new ServerMetricsHistory { ServerId = ServerId, Snapshots = Snapshots.Where(s => s.Timestamp >= since).ToList() };
    }

    public ServerMetricsSummary GetSummary()
    {
        if (Snapshots.Count == 0) return new ServerMetricsSummary { ServerId = ServerId };
        var recent = Snapshots.TakeLast(60).ToList();
        return new ServerMetricsSummary
        {
            ServerId = ServerId,
            CurrentCpu = recent.LastOrDefault()?.CpuPercent ?? 0,
            CurrentMemory = recent.LastOrDefault()?.MemoryMb ?? 0,
            AverageCpu = recent.Average(s => s.CpuPercent),
            AverageMemory = recent.Average(s => s.MemoryMb),
            PeakCpu = recent.Max(s => s.CpuPercent),
            PeakMemory = recent.Max(s => s.MemoryMb),
            DataPoints = Snapshots.Count,
            LastUpdated = recent.LastOrDefault()?.Timestamp ?? DateTime.MinValue
        };
    }
}

public class SystemMetricsHistory
{
    public List<SystemMetricsSnapshot> Snapshots { get; set; } = new();
    private const int MaxPoints = 1000;

    public void AddSnapshot(SystemMetricsSnapshot snapshot)
    {
        Snapshots.Add(snapshot);
        if (Snapshots.Count > MaxPoints) Snapshots.RemoveAt(0);
    }

    public SystemMetricsHistory GetRecent(int count)
    {
        return new SystemMetricsHistory { Snapshots = Snapshots.TakeLast(count).ToList() };
    }
}

public class ServerMetricsSnapshot
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public double CpuPercent { get; set; }
    public double MemoryMb { get; set; }
    public int PlayerCount { get; set; }
    public string Status { get; set; } = "";
    public int? MatchId { get; set; }
    public long UptimeSeconds { get; set; }
    public int NetworkBytesSent { get; set; }
    public int NetworkBytesRecv { get; set; }
}

public class SystemMetricsSnapshot
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public double CpuPercent { get; set; }
    public double MemoryUsedMb { get; set; }
    public double MemoryTotalMb { get; set; }
    public double DiskUsedGb { get; set; }
    public double DiskTotalGb { get; set; }
    public int ActiveServers { get; set; }
    public int TotalPlayers { get; set; }
    public int NetworkBytesSent { get; set; }
    public int NetworkBytesRecv { get; set; }
}

public class ServerMetricsSummary
{
    public int ServerId { get; set; }
    public double CurrentCpu { get; set; }
    public double CurrentMemory { get; set; }
    public double AverageCpu { get; set; }
    public double AverageMemory { get; set; }
    public double PeakCpu { get; set; }
    public double PeakMemory { get; set; }
    public int DataPoints { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class MetricsComparison
{
    public TimeSpan Period { get; set; }
    public DateTime GeneratedAt { get; set; }
    public Dictionary<int, ServerComparisonData> Servers { get; set; } = new();
}

public class ServerComparisonData
{
    public double AverageCpu { get; set; }
    public double AverageMemory { get; set; }
    public double MaxCpu { get; set; }
    public double MaxMemory { get; set; }
    public int TotalMatches { get; set; }
    public double AverageUptime { get; set; }
}
