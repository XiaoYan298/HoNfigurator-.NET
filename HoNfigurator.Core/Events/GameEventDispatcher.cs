using Microsoft.Extensions.Logging;

namespace HoNfigurator.Core.Events;

/// <summary>
/// Central event dispatcher for game server events with history tracking
/// </summary>
public class GameEventDispatcher
{
    private readonly ILogger<GameEventDispatcher> _logger;
    private readonly List<IGameEventHandler> _handlers = new();
    private readonly Queue<GameEvent> _eventHistory = new();
    private readonly object _historyLock = new();
    private const int MaxHistorySize = 500;

    public GameEventDispatcher(ILogger<GameEventDispatcher> logger)
    {
        _logger = logger;
    }

    public void RegisterHandler(IGameEventHandler handler)
    {
        _handlers.Add(handler);
        _logger.LogDebug("Registered event handler: {HandlerType}", handler.GetType().Name);
    }

    public void UnregisterHandler(IGameEventHandler handler)
    {
        _handlers.Remove(handler);
    }

    public async Task DispatchAsync(GameEvent gameEvent)
    {
        _logger.LogDebug("Dispatching event: {EventType} for server {ServerId}", gameEvent.EventType, gameEvent.ServerId);
        
        // Add to history
        lock (_historyLock)
        {
            _eventHistory.Enqueue(gameEvent);
            while (_eventHistory.Count > MaxHistorySize)
                _eventHistory.Dequeue();
        }

        foreach (var handler in _handlers)
        {
            try
            {
                if (handler.CanHandle(gameEvent.EventType))
                {
                    await handler.HandleAsync(gameEvent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in event handler {Handler} for event {EventType}",
                    handler.GetType().Name, gameEvent.EventType);
            }
        }
    }

    public void Dispatch(GameEvent gameEvent) => _ = DispatchAsync(gameEvent);

    public IReadOnlyList<GameEvent> GetRecentEvents(int count = 100)
    {
        lock (_historyLock)
        {
            return _eventHistory.TakeLast(count).Reverse().ToList();
        }
    }

    public IReadOnlyList<GameEvent> GetEventsByType(GameEventType type, int count = 50)
    {
        lock (_historyLock)
        {
            return _eventHistory.Where(e => e.EventType == type).TakeLast(count).Reverse().ToList();
        }
    }

    public IReadOnlyList<GameEvent> GetEventsByServer(int serverId, int count = 50)
    {
        lock (_historyLock)
        {
            return _eventHistory.Where(e => e.ServerId == serverId).TakeLast(count).Reverse().ToList();
        }
    }

    public EventStats GetStats()
    {
        lock (_historyLock)
        {
            var events = _eventHistory.ToList();
            return new EventStats
            {
                TotalEvents = events.Count,
                EventsByType = events.GroupBy(e => e.EventType).ToDictionary(g => g.Key.ToString(), g => g.Count()),
                EventsByServer = events.GroupBy(e => e.ServerId).ToDictionary(g => g.Key, g => g.Count()),
                OldestEvent = events.FirstOrDefault()?.Timestamp,
                NewestEvent = events.LastOrDefault()?.Timestamp
            };
        }
    }
}

public interface IGameEventHandler
{
    bool CanHandle(GameEventType eventType);
    Task HandleAsync(GameEvent gameEvent);
}

public class GameEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public GameEventType EventType { get; set; }
    public int ServerId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Data { get; set; } = new();

    public T? GetData<T>(string key)
    {
        if (Data.TryGetValue(key, out var value))
        {
            if (value is T typedValue) return typedValue;
            try { return (T)Convert.ChangeType(value, typeof(T)); }
            catch { return default; }
        }
        return default;
    }
}

public class EventStats
{
    public int TotalEvents { get; set; }
    public Dictionary<string, int> EventsByType { get; set; } = new();
    public Dictionary<int, int> EventsByServer { get; set; } = new();
    public DateTime? OldestEvent { get; set; }
    public DateTime? NewestEvent { get; set; }
}

public enum GameEventType
{
    ServerStarted, ServerStopped, ServerCrashed, ServerRestarted,
    MatchStarted, MatchEnded, MatchAborted,
    PlayerConnected, PlayerDisconnected, PlayerKicked, PlayerBanned,
    HeroSelected, FirstBlood, TowerDestroyed, BarracksDestroyed, KongorKilled, GamePaused, GameResumed,
    ChatMessage, TeamMessage, AllChat,
    AdminCommand, ConfigChanged,
    HealthCheckFailed, ResourceWarning, MaintenanceStarted, MaintenanceEnded
}
