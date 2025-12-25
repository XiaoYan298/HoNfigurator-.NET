using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HoNfigurator.Core.Statistics;

/// <summary>
/// Match statistics models
/// </summary>
public class MatchRecord
{
    public long Id { get; set; }
    public int ServerId { get; set; }
    public string ServerName { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int DurationSeconds { get; set; }
    public int PlayerCount { get; set; }
    public string? Winner { get; set; }
    public string? GameMode { get; set; }
    public string? Map { get; set; }
    public string PlayersJson { get; set; } = "[]";
    
    public List<string> Players => JsonSerializer.Deserialize<List<string>>(PlayersJson) ?? new();
}

public class PlayerStats
{
    public long Id { get; set; }
    public int AccountId { get; set; }
    public string PlayerName { get; set; } = "";
    public int TotalMatches { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Disconnects { get; set; }
    public double WinRate => TotalMatches > 0 ? (double)Wins / TotalMatches * 100 : 0;
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public int TotalPlayTimeSeconds { get; set; }
}

public class ServerStats
{
    public int ServerId { get; set; }
    public string ServerName { get; set; } = "";
    public int TotalMatches { get; set; }
    public int TotalPlayers { get; set; }
    public int TotalPlayTimeSeconds { get; set; }
    public double AverageMatchDuration { get; set; }
    public double AveragePlayersPerMatch { get; set; }
}

public class DailyStats
{
    public DateTime Date { get; set; }
    public int MatchCount { get; set; }
    public int UniquePlayerCount { get; set; }
    public int TotalPlayTimeSeconds { get; set; }
    public int PeakConcurrentPlayers { get; set; }
}

/// <summary>
/// Service for storing and retrieving match statistics using SQLite
/// </summary>
public interface IMatchStatisticsService
{
    Task InitializeAsync();
    
    // Match operations
    Task<long> RecordMatchStartAsync(int serverId, string serverName, List<string> players, string? gameMode = null);
    Task RecordMatchEndAsync(long matchId, string? winner = null);
    Task<MatchRecord?> GetMatchAsync(long matchId);
    Task<List<MatchRecord>> GetRecentMatchesAsync(int count = 20);
    Task<List<MatchRecord>> GetMatchesByServerAsync(int serverId, int count = 20);
    
    // Player stats
    Task UpdatePlayerStatsAsync(string playerName, int accountId, bool won, int playTimeSeconds);
    Task<PlayerStats?> GetPlayerStatsAsync(string playerName);
    Task<List<PlayerStats>> GetTopPlayersAsync(int count = 10);
    Task<List<PlayerStats>> GetMostActivePlayersAsync(int count = 10);
    
    // Server stats
    Task<ServerStats> GetServerStatsAsync(int serverId);
    Task<List<ServerStats>> GetAllServerStatsAsync();
    
    // Daily stats
    Task<DailyStats> GetTodayStatsAsync();
    Task<List<DailyStats>> GetDailyStatsAsync(int days = 7);
    
    // Summary
    Task<Dictionary<string, object>> GetOverallSummaryAsync();
}

public class MatchStatisticsService : IMatchStatisticsService
{
    private readonly ILogger<MatchStatisticsService> _logger;
    private readonly string _connectionString;
    private bool _initialized;

    public MatchStatisticsService(ILogger<MatchStatisticsService> logger, string? databasePath = null)
    {
        _logger = logger;
        var dbPath = databasePath ?? Path.Combine(AppContext.BaseDirectory, "data", "statistics.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        _connectionString = $"Data Source={dbPath}";
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // Create tables
        var createTablesCmd = connection.CreateCommand();
        createTablesCmd.CommandText = """
            CREATE TABLE IF NOT EXISTS matches (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                server_id INTEGER NOT NULL,
                server_name TEXT NOT NULL,
                start_time TEXT NOT NULL,
                end_time TEXT,
                duration_seconds INTEGER DEFAULT 0,
                player_count INTEGER DEFAULT 0,
                winner TEXT,
                game_mode TEXT,
                map TEXT,
                players_json TEXT DEFAULT '[]'
            );

            CREATE TABLE IF NOT EXISTS player_stats (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                account_id INTEGER DEFAULT 0,
                player_name TEXT NOT NULL UNIQUE,
                total_matches INTEGER DEFAULT 0,
                wins INTEGER DEFAULT 0,
                losses INTEGER DEFAULT 0,
                disconnects INTEGER DEFAULT 0,
                first_seen TEXT NOT NULL,
                last_seen TEXT NOT NULL,
                total_play_time_seconds INTEGER DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS daily_stats (
                date TEXT PRIMARY KEY,
                match_count INTEGER DEFAULT 0,
                unique_player_count INTEGER DEFAULT 0,
                total_play_time_seconds INTEGER DEFAULT 0,
                peak_concurrent_players INTEGER DEFAULT 0
            );

            CREATE INDEX IF NOT EXISTS idx_matches_server_id ON matches(server_id);
            CREATE INDEX IF NOT EXISTS idx_matches_start_time ON matches(start_time);
            CREATE INDEX IF NOT EXISTS idx_player_stats_name ON player_stats(player_name);
            CREATE INDEX IF NOT EXISTS idx_daily_stats_date ON daily_stats(date);
            """;
        await createTablesCmd.ExecuteNonQueryAsync();

        _initialized = true;
        _logger.LogInformation("Match statistics database initialized");
    }

    public async Task<long> RecordMatchStartAsync(int serverId, string serverName, List<string> players, string? gameMode = null)
    {
        await InitializeAsync();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO matches (server_id, server_name, start_time, player_count, game_mode, players_json)
            VALUES ($serverId, $serverName, $startTime, $playerCount, $gameMode, $playersJson);
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("$serverId", serverId);
        cmd.Parameters.AddWithValue("$serverName", serverName);
        cmd.Parameters.AddWithValue("$startTime", DateTime.UtcNow.ToString("o"));
        cmd.Parameters.AddWithValue("$playerCount", players.Count);
        cmd.Parameters.AddWithValue("$gameMode", gameMode ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$playersJson", JsonSerializer.Serialize(players));

        var matchId = (long)(await cmd.ExecuteScalarAsync())!;
        
        _logger.LogInformation("Recorded match start: ID={MatchId}, Server={Server}, Players={Count}", 
            matchId, serverName, players.Count);
        
        // Update daily stats
        await UpdateDailyStatsAsync(connection, players.Count);
        
        return matchId;
    }

    public async Task RecordMatchEndAsync(long matchId, string? winner = null)
    {
        await InitializeAsync();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // Get match start time to calculate duration
        var getCmd = connection.CreateCommand();
        getCmd.CommandText = "SELECT start_time FROM matches WHERE id = $id";
        getCmd.Parameters.AddWithValue("$id", matchId);
        var startTimeStr = await getCmd.ExecuteScalarAsync() as string;
        
        if (startTimeStr == null) return;

        var startTime = DateTime.Parse(startTimeStr);
        var duration = (int)(DateTime.UtcNow - startTime).TotalSeconds;

        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            UPDATE matches 
            SET end_time = $endTime, duration_seconds = $duration, winner = $winner
            WHERE id = $id
            """;
        cmd.Parameters.AddWithValue("$id", matchId);
        cmd.Parameters.AddWithValue("$endTime", DateTime.UtcNow.ToString("o"));
        cmd.Parameters.AddWithValue("$duration", duration);
        cmd.Parameters.AddWithValue("$winner", winner ?? (object)DBNull.Value);

        await cmd.ExecuteNonQueryAsync();
        
        _logger.LogInformation("Recorded match end: ID={MatchId}, Duration={Duration}s, Winner={Winner}", 
            matchId, duration, winner ?? "N/A");
    }

    public async Task<MatchRecord?> GetMatchAsync(long matchId)
    {
        await InitializeAsync();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM matches WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", matchId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return ReadMatchRecord(reader);
        }
        return null;
    }

    public async Task<List<MatchRecord>> GetRecentMatchesAsync(int count = 20)
    {
        await InitializeAsync();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM matches ORDER BY start_time DESC LIMIT $count";
        cmd.Parameters.AddWithValue("$count", count);

        var matches = new List<MatchRecord>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            matches.Add(ReadMatchRecord(reader));
        }
        return matches;
    }

    public async Task<List<MatchRecord>> GetMatchesByServerAsync(int serverId, int count = 20)
    {
        await InitializeAsync();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM matches WHERE server_id = $serverId ORDER BY start_time DESC LIMIT $count";
        cmd.Parameters.AddWithValue("$serverId", serverId);
        cmd.Parameters.AddWithValue("$count", count);

        var matches = new List<MatchRecord>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            matches.Add(ReadMatchRecord(reader));
        }
        return matches;
    }

    public async Task UpdatePlayerStatsAsync(string playerName, int accountId, bool won, int playTimeSeconds)
    {
        await InitializeAsync();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO player_stats (account_id, player_name, total_matches, wins, losses, first_seen, last_seen, total_play_time_seconds)
            VALUES ($accountId, $playerName, 1, $wins, $losses, $now, $now, $playTime)
            ON CONFLICT(player_name) DO UPDATE SET
                account_id = CASE WHEN $accountId > 0 THEN $accountId ELSE account_id END,
                total_matches = total_matches + 1,
                wins = wins + $wins,
                losses = losses + $losses,
                last_seen = $now,
                total_play_time_seconds = total_play_time_seconds + $playTime
            """;
        cmd.Parameters.AddWithValue("$accountId", accountId);
        cmd.Parameters.AddWithValue("$playerName", playerName);
        cmd.Parameters.AddWithValue("$wins", won ? 1 : 0);
        cmd.Parameters.AddWithValue("$losses", won ? 0 : 1);
        cmd.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("o"));
        cmd.Parameters.AddWithValue("$playTime", playTimeSeconds);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<PlayerStats?> GetPlayerStatsAsync(string playerName)
    {
        await InitializeAsync();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM player_stats WHERE player_name = $playerName";
        cmd.Parameters.AddWithValue("$playerName", playerName);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return ReadPlayerStats(reader);
        }
        return null;
    }

    public async Task<List<PlayerStats>> GetTopPlayersAsync(int count = 10)
    {
        await InitializeAsync();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT * FROM player_stats 
            WHERE total_matches >= 5
            ORDER BY (CAST(wins AS REAL) / total_matches) DESC 
            LIMIT $count
            """;
        cmd.Parameters.AddWithValue("$count", count);

        var players = new List<PlayerStats>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            players.Add(ReadPlayerStats(reader));
        }
        return players;
    }

    public async Task<List<PlayerStats>> GetMostActivePlayersAsync(int count = 10)
    {
        await InitializeAsync();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM player_stats ORDER BY total_matches DESC LIMIT $count";
        cmd.Parameters.AddWithValue("$count", count);

        var players = new List<PlayerStats>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            players.Add(ReadPlayerStats(reader));
        }
        return players;
    }

    public async Task<ServerStats> GetServerStatsAsync(int serverId)
    {
        await InitializeAsync();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT 
                server_id,
                server_name,
                COUNT(*) as total_matches,
                SUM(player_count) as total_players,
                SUM(duration_seconds) as total_play_time,
                AVG(duration_seconds) as avg_duration,
                AVG(player_count) as avg_players
            FROM matches 
            WHERE server_id = $serverId
            GROUP BY server_id
            """;
        cmd.Parameters.AddWithValue("$serverId", serverId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new ServerStats
            {
                ServerId = reader.GetInt32(0),
                ServerName = reader.GetString(1),
                TotalMatches = reader.GetInt32(2),
                TotalPlayers = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                TotalPlayTimeSeconds = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                AverageMatchDuration = reader.IsDBNull(5) ? 0 : reader.GetDouble(5),
                AveragePlayersPerMatch = reader.IsDBNull(6) ? 0 : reader.GetDouble(6)
            };
        }
        return new ServerStats { ServerId = serverId };
    }

    public async Task<List<ServerStats>> GetAllServerStatsAsync()
    {
        await InitializeAsync();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT 
                server_id,
                server_name,
                COUNT(*) as total_matches,
                SUM(player_count) as total_players,
                SUM(duration_seconds) as total_play_time,
                AVG(duration_seconds) as avg_duration,
                AVG(player_count) as avg_players
            FROM matches 
            GROUP BY server_id
            ORDER BY total_matches DESC
            """;

        var stats = new List<ServerStats>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            stats.Add(new ServerStats
            {
                ServerId = reader.GetInt32(0),
                ServerName = reader.GetString(1),
                TotalMatches = reader.GetInt32(2),
                TotalPlayers = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                TotalPlayTimeSeconds = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                AverageMatchDuration = reader.IsDBNull(5) ? 0 : reader.GetDouble(5),
                AveragePlayersPerMatch = reader.IsDBNull(6) ? 0 : reader.GetDouble(6)
            });
        }
        return stats;
    }

    public async Task<DailyStats> GetTodayStatsAsync()
    {
        await InitializeAsync();
        var today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM daily_stats WHERE date = $date";
        cmd.Parameters.AddWithValue("$date", today);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new DailyStats
            {
                Date = DateTime.Parse(reader.GetString(0)),
                MatchCount = reader.GetInt32(1),
                UniquePlayerCount = reader.GetInt32(2),
                TotalPlayTimeSeconds = reader.GetInt32(3),
                PeakConcurrentPlayers = reader.GetInt32(4)
            };
        }
        return new DailyStats { Date = DateTime.UtcNow.Date };
    }

    public async Task<List<DailyStats>> GetDailyStatsAsync(int days = 7)
    {
        await InitializeAsync();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM daily_stats ORDER BY date DESC LIMIT $days";
        cmd.Parameters.AddWithValue("$days", days);

        var stats = new List<DailyStats>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            stats.Add(new DailyStats
            {
                Date = DateTime.Parse(reader.GetString(0)),
                MatchCount = reader.GetInt32(1),
                UniquePlayerCount = reader.GetInt32(2),
                TotalPlayTimeSeconds = reader.GetInt32(3),
                PeakConcurrentPlayers = reader.GetInt32(4)
            });
        }
        return stats;
    }

    public async Task<Dictionary<string, object>> GetOverallSummaryAsync()
    {
        await InitializeAsync();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // Get match stats
        var matchCmd = connection.CreateCommand();
        matchCmd.CommandText = """
            SELECT 
                COUNT(*) as total_matches,
                SUM(duration_seconds) as total_play_time,
                AVG(duration_seconds) as avg_duration,
                AVG(player_count) as avg_players
            FROM matches
            """;

        var summary = new Dictionary<string, object>();
        await using (var reader = await matchCmd.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                summary["total_matches"] = reader.GetInt32(0);
                summary["total_play_time_seconds"] = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                summary["average_match_duration"] = reader.IsDBNull(2) ? 0 : reader.GetDouble(2);
                summary["average_players_per_match"] = reader.IsDBNull(3) ? 0 : reader.GetDouble(3);
            }
        }

        // Get player stats
        var playerCmd = connection.CreateCommand();
        playerCmd.CommandText = "SELECT COUNT(*) FROM player_stats";
        summary["unique_players"] = (long)(await playerCmd.ExecuteScalarAsync())!;

        // Get today's stats
        var todayStats = await GetTodayStatsAsync();
        summary["today_matches"] = todayStats.MatchCount;
        summary["today_unique_players"] = todayStats.UniquePlayerCount;

        return summary;
    }

    private async Task UpdateDailyStatsAsync(SqliteConnection connection, int playerCount)
    {
        var today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
        
        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO daily_stats (date, match_count, unique_player_count, peak_concurrent_players)
            VALUES ($date, 1, $playerCount, $playerCount)
            ON CONFLICT(date) DO UPDATE SET
                match_count = match_count + 1,
                unique_player_count = unique_player_count + $playerCount,
                peak_concurrent_players = MAX(peak_concurrent_players, $playerCount)
            """;
        cmd.Parameters.AddWithValue("$date", today);
        cmd.Parameters.AddWithValue("$playerCount", playerCount);

        await cmd.ExecuteNonQueryAsync();
    }

    private static MatchRecord ReadMatchRecord(SqliteDataReader reader)
    {
        return new MatchRecord
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            ServerId = reader.GetInt32(reader.GetOrdinal("server_id")),
            ServerName = reader.GetString(reader.GetOrdinal("server_name")),
            StartTime = DateTime.Parse(reader.GetString(reader.GetOrdinal("start_time"))),
            EndTime = reader.IsDBNull(reader.GetOrdinal("end_time")) 
                ? null 
                : DateTime.Parse(reader.GetString(reader.GetOrdinal("end_time"))),
            DurationSeconds = reader.GetInt32(reader.GetOrdinal("duration_seconds")),
            PlayerCount = reader.GetInt32(reader.GetOrdinal("player_count")),
            Winner = reader.IsDBNull(reader.GetOrdinal("winner")) ? null : reader.GetString(reader.GetOrdinal("winner")),
            GameMode = reader.IsDBNull(reader.GetOrdinal("game_mode")) ? null : reader.GetString(reader.GetOrdinal("game_mode")),
            Map = reader.IsDBNull(reader.GetOrdinal("map")) ? null : reader.GetString(reader.GetOrdinal("map")),
            PlayersJson = reader.GetString(reader.GetOrdinal("players_json"))
        };
    }

    private static PlayerStats ReadPlayerStats(SqliteDataReader reader)
    {
        return new PlayerStats
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            AccountId = reader.GetInt32(reader.GetOrdinal("account_id")),
            PlayerName = reader.GetString(reader.GetOrdinal("player_name")),
            TotalMatches = reader.GetInt32(reader.GetOrdinal("total_matches")),
            Wins = reader.GetInt32(reader.GetOrdinal("wins")),
            Losses = reader.GetInt32(reader.GetOrdinal("losses")),
            Disconnects = reader.GetInt32(reader.GetOrdinal("disconnects")),
            FirstSeen = DateTime.Parse(reader.GetString(reader.GetOrdinal("first_seen"))),
            LastSeen = DateTime.Parse(reader.GetString(reader.GetOrdinal("last_seen"))),
            TotalPlayTimeSeconds = reader.GetInt32(reader.GetOrdinal("total_play_time_seconds"))
        };
    }
}
