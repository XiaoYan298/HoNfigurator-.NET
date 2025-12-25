using Microsoft.Extensions.Logging;

namespace HoNfigurator.Core.Events;

/// <summary>
/// Handles logging of game events
/// </summary>
public class LoggingEventHandler : IGameEventHandler
{
    private readonly ILogger<LoggingEventHandler> _logger;

    public LoggingEventHandler(ILogger<LoggingEventHandler> logger)
    {
        _logger = logger;
    }

    public bool CanHandle(GameEventType eventType) => true;

    public Task HandleAsync(GameEvent gameEvent)
    {
        var logLevel = GetLogLevel(gameEvent.EventType);
        _logger.Log(logLevel, "[Server {ServerId}] {EventType}: {Data}",
            gameEvent.ServerId, gameEvent.EventType,
            string.Join(", ", gameEvent.Data.Select(kv => $"{kv.Key}={kv.Value}")));
        return Task.CompletedTask;
    }

    private LogLevel GetLogLevel(GameEventType eventType)
    {
        return eventType switch
        {
            GameEventType.ServerCrashed => LogLevel.Error,
            GameEventType.HealthCheckFailed => LogLevel.Warning,
            GameEventType.ResourceWarning => LogLevel.Warning,
            GameEventType.PlayerBanned => LogLevel.Warning,
            GameEventType.PlayerKicked => LogLevel.Warning,
            _ => LogLevel.Information
        };
    }
}

/// <summary>
/// Handles match statistics collection
/// </summary>
public class MatchStatsHandler : IGameEventHandler
{
    private readonly ILogger<MatchStatsHandler> _logger;
    private readonly List<MatchStats> _recentMatches = new();
    private readonly object _lock = new();

    public MatchStatsHandler(ILogger<MatchStatsHandler> logger)
    {
        _logger = logger;
    }

    public bool CanHandle(GameEventType eventType) =>
        eventType is GameEventType.MatchStarted or GameEventType.MatchEnded or GameEventType.MatchAborted;

    public Task HandleAsync(GameEvent gameEvent)
    {
        lock (_lock)
        {
            switch (gameEvent.EventType)
            {
                case GameEventType.MatchStarted:
                    var matchId = gameEvent.GetData<long>("matchId");
                    _recentMatches.Add(new MatchStats
                    {
                        MatchId = matchId,
                        ServerId = gameEvent.ServerId,
                        StartTime = gameEvent.Timestamp,
                        GameMode = gameEvent.GetData<string>("gameMode") ?? "Unknown"
                    });
                    break;

                case GameEventType.MatchEnded:
                    var endedMatchId = gameEvent.GetData<long>("matchId");
                    var match = _recentMatches.FirstOrDefault(m => m.MatchId == endedMatchId);
                    if (match != null)
                    {
                        match.EndTime = gameEvent.Timestamp;
                        match.Duration = match.EndTime.Value - match.StartTime;
                        match.Winner = gameEvent.GetData<string>("winner");
                        _logger.LogInformation("Match {MatchId} ended. Duration: {Duration}", endedMatchId, match.Duration);
                    }
                    break;

                case GameEventType.MatchAborted:
                    var abortedMatchId = gameEvent.GetData<long>("matchId");
                    var abortedMatch = _recentMatches.FirstOrDefault(m => m.MatchId == abortedMatchId);
                    if (abortedMatch != null)
                    {
                        abortedMatch.WasAborted = true;
                        abortedMatch.EndTime = gameEvent.Timestamp;
                    }
                    break;
            }

            while (_recentMatches.Count > 100)
                _recentMatches.RemoveAt(0);
        }
        return Task.CompletedTask;
    }

    public List<MatchStats> GetRecentMatches()
    {
        lock (_lock) { return _recentMatches.ToList(); }
    }
}

public class MatchStats
{
    public long MatchId { get; set; }
    public int ServerId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan? Duration { get; set; }
    public string GameMode { get; set; } = string.Empty;
    public string? Winner { get; set; }
    public bool WasAborted { get; set; }
}
