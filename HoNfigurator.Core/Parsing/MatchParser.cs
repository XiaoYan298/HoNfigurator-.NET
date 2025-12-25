using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace HoNfigurator.Core.Parsing;

/// <summary>
/// Match result data parsed from game logs
/// </summary>
public record MatchResult
{
    public int MatchId { get; init; }
    public string Map { get; init; } = string.Empty;
    public string GameMode { get; init; } = string.Empty;
    public string GameName { get; init; } = string.Empty;
    public TimeSpan Duration { get; init; }
    public int WinningTeam { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public List<PlayerMatchResult> Players { get; init; } = new();
}

/// <summary>
/// Individual player result from a match
/// </summary>
public record PlayerMatchResult
{
    public int AccountId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Team { get; init; }
    public int Slot { get; init; }
    public string Hero { get; init; } = string.Empty;
    public int Kills { get; init; }
    public int Deaths { get; init; }
    public int Assists { get; init; }
    public int Level { get; init; }
    public int CreepKills { get; init; }
    public int CreepDenies { get; init; }
    public int NeutralKills { get; init; }
    public int GoldEarned { get; init; }
    public int GoldSpent { get; init; }
    public int HeroDamage { get; init; }
    public int TowerDamage { get; init; }
    public bool IsWinner { get; init; }
    public bool Disconnected { get; init; }
    public TimeSpan PlayTime { get; init; }
}

/// <summary>
/// Log entry parsed from server logs
/// </summary>
public record LogEntry
{
    public DateTime Timestamp { get; init; }
    public string Level { get; init; } = "INFO";
    public string Category { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Parses HoN game server log files for match results and statistics
/// </summary>
public class MatchParser
{
    private readonly ILogger<MatchParser> _logger;
    
    // Regex patterns for log parsing
    private static readonly Regex TimestampPattern = new(@"\[(\w+\s+\d+\s+\d+:\d+:\d+)\]", RegexOptions.Compiled);
    private static readonly Regex MatchStartPattern = new(@"Match\s+(\d+)\s+started", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex MatchEndPattern = new(@"Match\s+(\d+)\s+ended.*winner.*team\s+(\d)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex PlayerJoinPattern = new(@"Player\s+'(.+?)'\s+\((\d+)\)\s+joined.*team\s+(\d)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex PlayerLeavePattern = new(@"Player\s+'(.+?)'\s+\((\d+)\)\s+left", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex HeroSelectPattern = new(@"Player\s+'(.+?)'\s+selected\s+hero\s+'(.+?)'", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex StatsPattern = new(@"STATS:\s*(\{.*\})", RegexOptions.Compiled);
    private static readonly Regex MapLoadPattern = new(@"Loading\s+map\s+'(.+?)'", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex GameModePattern = new(@"Game\s+mode:\s+'?(.+?)'?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public MatchParser(ILogger<MatchParser> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parse a log file and extract all match results
    /// </summary>
    public async Task<List<MatchResult>> ParseLogFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var results = new List<MatchResult>();
        
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Log file not found: {FilePath}", filePath);
            return results;
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
            results = ParseLogLines(lines);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing log file: {FilePath}", filePath);
        }

        return results;
    }

    /// <summary>
    /// Parse log lines and extract match results
    /// </summary>
    public List<MatchResult> ParseLogLines(IEnumerable<string> lines)
    {
        var results = new List<MatchResult>();
        var currentMatch = new MatchBuilder();
        var inMatch = false;

        foreach (var line in lines)
        {
            // Check for match start
            var matchStart = MatchStartPattern.Match(line);
            if (matchStart.Success)
            {
                if (inMatch && currentMatch.MatchId > 0)
                {
                    // Previous match didn't end properly
                    results.Add(currentMatch.Build());
                }
                
                currentMatch = new MatchBuilder
                {
                    MatchId = int.Parse(matchStart.Groups[1].Value),
                    StartTime = ParseTimestamp(line)
                };
                inMatch = true;
                continue;
            }

            if (!inMatch) continue;

            // Check for map load
            var mapLoad = MapLoadPattern.Match(line);
            if (mapLoad.Success)
            {
                currentMatch.Map = mapLoad.Groups[1].Value;
                continue;
            }

            // Check for game mode
            var gameMode = GameModePattern.Match(line);
            if (gameMode.Success)
            {
                currentMatch.GameMode = gameMode.Groups[1].Value;
                continue;
            }

            // Check for player join
            var playerJoin = PlayerJoinPattern.Match(line);
            if (playerJoin.Success)
            {
                var player = new PlayerMatchResultBuilder
                {
                    Name = playerJoin.Groups[1].Value,
                    AccountId = int.Parse(playerJoin.Groups[2].Value),
                    Team = int.Parse(playerJoin.Groups[3].Value)
                };
                currentMatch.Players[player.AccountId] = player;
                continue;
            }

            // Check for hero selection
            var heroSelect = HeroSelectPattern.Match(line);
            if (heroSelect.Success)
            {
                var playerName = heroSelect.Groups[1].Value;
                var heroName = heroSelect.Groups[2].Value;
                var player = currentMatch.Players.Values.FirstOrDefault(p => p.Name == playerName);
                if (player != null)
                {
                    player.Hero = heroName;
                }
                continue;
            }

            // Check for player leave
            var playerLeave = PlayerLeavePattern.Match(line);
            if (playerLeave.Success)
            {
                var accountId = int.Parse(playerLeave.Groups[2].Value);
                if (currentMatch.Players.TryGetValue(accountId, out var player))
                {
                    player.Disconnected = true;
                    player.PlayTime = ParseTimestamp(line) - currentMatch.StartTime;
                }
                continue;
            }

            // Check for match end
            var matchEnd = MatchEndPattern.Match(line);
            if (matchEnd.Success)
            {
                currentMatch.WinningTeam = int.Parse(matchEnd.Groups[2].Value);
                currentMatch.EndTime = ParseTimestamp(line);
                
                // Mark winners
                foreach (var player in currentMatch.Players.Values)
                {
                    player.IsWinner = player.Team == currentMatch.WinningTeam;
                    if (!player.Disconnected)
                    {
                        player.PlayTime = currentMatch.EndTime - currentMatch.StartTime;
                    }
                }
                
                results.Add(currentMatch.Build());
                inMatch = false;
                currentMatch = new MatchBuilder();
            }
        }

        return results;
    }

    /// <summary>
    /// Parse log entries from lines
    /// </summary>
    public List<LogEntry> ParseLogEntries(IEnumerable<string> lines)
    {
        var entries = new List<LogEntry>();
        
        foreach (var line in lines)
        {
            var timestamp = ParseTimestamp(line);
            var level = "INFO";
            var message = line;

            if (line.Contains("Error:", StringComparison.OrdinalIgnoreCase) || 
                line.Contains("[ERROR]", StringComparison.OrdinalIgnoreCase))
            {
                level = "ERROR";
            }
            else if (line.Contains("Warning:", StringComparison.OrdinalIgnoreCase) ||
                     line.Contains("[WARN]", StringComparison.OrdinalIgnoreCase))
            {
                level = "WARN";
            }
            else if (line.Contains("[DEBUG]", StringComparison.OrdinalIgnoreCase))
            {
                level = "DEBUG";
            }

            entries.Add(new LogEntry
            {
                Timestamp = timestamp,
                Level = level,
                Message = message
            });
        }

        return entries;
    }

    private static DateTime ParseTimestamp(string line)
    {
        var match = TimestampPattern.Match(line);
        if (match.Success)
        {
            if (DateTime.TryParse(match.Groups[1].Value, out var result))
            {
                return result;
            }
        }
        return DateTime.Now;
    }

    private class MatchBuilder
    {
        public int MatchId { get; set; }
        public string Map { get; set; } = "";
        public string GameMode { get; set; } = "";
        public string GameName { get; set; } = "";
        public int WinningTeam { get; set; }
        public DateTime StartTime { get; set; } = DateTime.Now;
        public DateTime EndTime { get; set; } = DateTime.Now;
        public Dictionary<int, PlayerMatchResultBuilder> Players { get; } = new();

        public MatchResult Build() => new()
        {
            MatchId = MatchId,
            Map = Map,
            GameMode = GameMode,
            GameName = GameName,
            Duration = EndTime - StartTime,
            WinningTeam = WinningTeam,
            StartTime = StartTime,
            EndTime = EndTime,
            Players = Players.Values.Select(p => p.Build()).ToList()
        };
    }

    private class PlayerMatchResultBuilder
    {
        public int AccountId { get; set; }
        public string Name { get; set; } = "";
        public int Team { get; set; }
        public string Hero { get; set; } = "";
        public bool IsWinner { get; set; }
        public bool Disconnected { get; set; }
        public TimeSpan PlayTime { get; set; }
        
        public PlayerMatchResult Build() => new()
        {
            AccountId = AccountId,
            Name = Name,
            Team = Team,
            Hero = Hero,
            IsWinner = IsWinner,
            Disconnected = Disconnected,
            PlayTime = PlayTime
        };
    }
}
