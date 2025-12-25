using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HoNfigurator.Core.Services;

/// <summary>
/// Manages player bans and kicks
/// </summary>
public class BanManager
{
    private readonly ILogger<BanManager> _logger;
    private readonly string _bansFilePath;
    private readonly List<BanRecord> _bans = new();
    private readonly object _lock = new();

    public BanManager(ILogger<BanManager> logger, string bansFilePath = "config/bans.json")
    {
        _logger = logger;
        _bansFilePath = bansFilePath;
        LoadBans();
    }

    public void LoadBans()
    {
        lock (_lock)
        {
            _bans.Clear();
            if (File.Exists(_bansFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_bansFilePath);
                    var loaded = JsonSerializer.Deserialize<List<BanRecord>>(json);
                    if (loaded != null)
                        _bans.AddRange(loaded);
                    _logger.LogInformation("Loaded {Count} ban records", _bans.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load bans file");
                }
            }
        }
    }

    public void SaveBans()
    {
        lock (_lock)
        {
            try
            {
                var dir = Path.GetDirectoryName(_bansFilePath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                    
                var json = JsonSerializer.Serialize(_bans, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_bansFilePath, json);
                _logger.LogDebug("Saved {Count} ban records", _bans.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save bans file");
            }
        }
    }

    public BanRecord BanPlayer(int accountId, string playerName, string reason, string bannedBy, 
        BanType type = BanType.Permanent, TimeSpan? duration = null)
    {
        lock (_lock)
        {
            // Remove existing ban if any
            _bans.RemoveAll(b => b.AccountId == accountId);

            var ban = new BanRecord
            {
                AccountId = accountId,
                PlayerName = playerName,
                Reason = reason,
                BannedBy = bannedBy,
                BannedAt = DateTime.UtcNow,
                Type = type,
                ExpiresAt = type == BanType.Temporary && duration.HasValue 
                    ? DateTime.UtcNow.Add(duration.Value) 
                    : null
            };

            _bans.Add(ban);
            SaveBans();

            _logger.LogWarning("Banned player {PlayerName} (ID: {AccountId}). Reason: {Reason}", 
                playerName, accountId, reason);

            return ban;
        }
    }

    public bool UnbanPlayer(int accountId)
    {
        lock (_lock)
        {
            var removed = _bans.RemoveAll(b => b.AccountId == accountId);
            if (removed > 0)
            {
                SaveBans();
                _logger.LogInformation("Unbanned player with ID: {AccountId}", accountId);
                return true;
            }
            return false;
        }
    }

    public BanRecord? GetBan(int accountId)
    {
        lock (_lock)
        {
            var ban = _bans.FirstOrDefault(b => b.AccountId == accountId);
            if (ban != null && ban.Type == BanType.Temporary && ban.ExpiresAt.HasValue)
            {
                if (DateTime.UtcNow > ban.ExpiresAt.Value)
                {
                    _bans.Remove(ban);
                    SaveBans();
                    return null;
                }
            }
            return ban;
        }
    }

    public bool IsBanned(int accountId)
    {
        return GetBan(accountId) != null;
    }

    public List<BanRecord> GetAllBans(bool includeExpired = false)
    {
        lock (_lock)
        {
            if (!includeExpired)
            {
                // Remove expired bans
                var expired = _bans.Where(b => 
                    b.Type == BanType.Temporary && 
                    b.ExpiresAt.HasValue && 
                    DateTime.UtcNow > b.ExpiresAt.Value).ToList();
                
                if (expired.Any())
                {
                    foreach (var ban in expired)
                        _bans.Remove(ban);
                    SaveBans();
                }
            }
            return _bans.ToList();
        }
    }

    public List<BanRecord> SearchBans(string query)
    {
        lock (_lock)
        {
            return _bans.Where(b => 
                b.PlayerName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                b.Reason.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                b.AccountId.ToString() == query).ToList();
        }
    }

    public void AddKickRecord(int accountId, string playerName, string reason, string kickedBy, int serverId)
    {
        _logger.LogWarning("Kicked player {PlayerName} (ID: {AccountId}) from server {ServerId}. Reason: {Reason}",
            playerName, accountId, serverId, reason);
    }
}

public class BanRecord
{
    public int AccountId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string BannedBy { get; set; } = string.Empty;
    public DateTime BannedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public BanType Type { get; set; }

    public bool IsExpired => Type == BanType.Temporary && ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
    public TimeSpan? RemainingTime => ExpiresAt.HasValue ? ExpiresAt.Value - DateTime.UtcNow : null;
}

public enum BanType
{
    Permanent,
    Temporary
}
