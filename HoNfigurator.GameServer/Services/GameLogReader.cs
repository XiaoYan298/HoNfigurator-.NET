using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using HoNfigurator.Core.Models;

namespace HoNfigurator.GameServer.Services;

/// <summary>
/// Service for reading game log files to extract player slot information.
/// HoN game servers write log files (*.clog) with player connection info including slot assignments.
/// Slot 0-4 = Legion (Team 1), Slot 5-9 = Hellbourne (Team 2), others = Spectators
/// </summary>
public interface IGameLogReader
{
    /// <summary>
    /// Get player slot assignments from the game log for a specific server.
    /// </summary>
    Dictionary<string, int> GetPlayerSlots(int serverId);
    
    /// <summary>
    /// Populate PlayersByTeam based on current Players list and log file data.
    /// </summary>
    void PopulateTeams(GameServerInstance instance);
    
    /// <summary>
    /// Get match info from game log (mode, map name, etc.)
    /// </summary>
    MatchInfo? GetMatchInfo(int serverId);
}

/// <summary>
/// Match information extracted from game log
/// </summary>
public class MatchInfo
{
    public string? Mode { get; set; }
    public string? Map { get; set; }
    public string? Name { get; set; }
    public bool IsBotMatch => Mode?.Equals("botmatch", StringComparison.OrdinalIgnoreCase) == true;
}

public class GameLogReader : IGameLogReader
{
    private readonly ILogger<GameLogReader> _logger;
    private readonly HoNConfiguration _config;
    
    // Regex patterns for parsing log files
    // PLAYER_CONNECT player:X name:"PlayerName" id:Y psr:Z
    private static readonly Regex PlayerConnectPattern = new(
        @"PLAYER_CONNECT player:(\d+) name:""(.*?)""(?: id:(\d+))?(?: psr:(\d+(?:\.\d+)?))?",
        RegexOptions.Compiled);
    
    // PLAYER_TEAM_CHANGE player:X team:Y (if available)
    private static readonly Regex TeamChangePattern = new(
        @"PLAYER_TEAM_CHANGE player:(\d+) team:(\d+)",
        RegexOptions.Compiled);
    
    // Match info patterns from log files
    // INFO_MATCH name:"MatchName"
    private static readonly Regex MatchNamePattern = new(
        @"INFO_MATCH name:""([^""]+)""",
        RegexOptions.Compiled);
    
    // INFO_MAP name:"MapName"
    private static readonly Regex MapNamePattern = new(
        @"INFO_MAP name:""([^""]+)""",
        RegexOptions.Compiled);
    
    // INFO_SETTINGS mode:"Mode_Normal" or mode:"Mode_BotMatch"
    private static readonly Regex ModePattern = new(
        @"INFO_SETTINGS mode:""([^""]+)""",
        RegexOptions.Compiled);
    
    public GameLogReader(ILogger<GameLogReader> logger, HoNConfiguration config)
    {
        _logger = logger;
        _config = config;
    }
    
    public Dictionary<string, int> GetPlayerSlots(int serverId)
    {
        var playerSlots = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        
        try
        {
            var logsDir = _config.HonData.HonLogsDirectory;
            if (string.IsNullOrEmpty(logsDir) || !Directory.Exists(logsDir))
            {
                _logger.LogDebug("Logs directory not configured or doesn't exist: {Dir}", logsDir);
                return playerSlots;
            }
            
            // Find the latest log file for this server: Slave{id}_*.clog
            var pattern = $"Slave{serverId}_*.clog";
            var files = Directory.GetFiles(logsDir, pattern);
            
            if (files.Length == 0)
            {
                _logger.LogDebug("No log files found for server #{Id} in {Dir}", serverId, logsDir);
                return playerSlots;
            }
            
            // Get the most recent file
            var latestFile = files
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTime)
                .First()
                .FullName;
            
            _logger.LogDebug("Reading log file: {File}", latestFile);
            
            // Read the file with UTF-16-LE encoding (HoN uses this)
            var lines = File.ReadAllLines(latestFile, Encoding.Unicode);
            
            // Track team changes
            var playerTeams = new Dictionary<int, int>(); // slot -> team
            
            foreach (var line in lines)
            {
                // Check for PLAYER_CONNECT
                var connectMatch = PlayerConnectPattern.Match(line);
                if (connectMatch.Success)
                {
                    var slot = int.Parse(connectMatch.Groups[1].Value);
                    var playerName = connectMatch.Groups[2].Value;
                    
                    playerSlots[playerName] = slot;
                    
                    // Default team based on slot
                    if (slot <= 4)
                        playerTeams[slot] = 1; // Legion
                    else if (slot <= 9)
                        playerTeams[slot] = 2; // Hellbourne
                    else
                        playerTeams[slot] = 0; // Spectator
                }
                
                // Check for team changes
                var teamMatch = TeamChangePattern.Match(line);
                if (teamMatch.Success)
                {
                    var slot = int.Parse(teamMatch.Groups[1].Value);
                    var team = int.Parse(teamMatch.Groups[2].Value);
                    playerTeams[slot] = team;
                }
            }
            
            _logger.LogDebug("Found {Count} player slots in log file", playerSlots.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading game log for server #{Id}", serverId);
        }
        
        return playerSlots;
    }
    
    public void PopulateTeams(GameServerInstance instance)
    {
        // Clear existing teams
        instance.PlayersByTeam.Legion.Clear();
        instance.PlayersByTeam.Hellbourne.Clear();
        instance.PlayersByTeam.Spectators.Clear();
        
        if (instance.Players.Count == 0)
            return;
        
        // Get slot assignments from log file
        var playerSlots = GetPlayerSlots(instance.Id);
        
        foreach (var player in instance.Players)
        {
            // Try to find slot from log file
            if (playerSlots.TryGetValue(player.Name, out var slot))
            {
                player.Slot = slot;
                
                // Assign to team based on slot
                if (slot >= 0 && slot <= 4)
                {
                    instance.PlayersByTeam.Legion.Add(player);
                }
                else if (slot >= 5 && slot <= 9)
                {
                    instance.PlayersByTeam.Hellbourne.Add(player);
                }
                else
                {
                    instance.PlayersByTeam.Spectators.Add(player);
                }
            }
            else
            {
                // No slot info - put in spectators
                _logger.LogDebug("No slot info for player {Name}, adding to spectators", player.Name);
                instance.PlayersByTeam.Spectators.Add(player);
            }
        }
        
        // Sort by slot
        instance.PlayersByTeam.Legion = instance.PlayersByTeam.Legion.OrderBy(p => p.Slot).ToList();
        instance.PlayersByTeam.Hellbourne = instance.PlayersByTeam.Hellbourne.OrderBy(p => p.Slot).ToList();
        
        _logger.LogDebug("Server #{Id} teams: Legion={Legion}, Hellbourne={Hellbourne}, Spectators={Spec}",
            instance.Id,
            instance.PlayersByTeam.Legion.Count,
            instance.PlayersByTeam.Hellbourne.Count,
            instance.PlayersByTeam.Spectators.Count);
    }
    
    public MatchInfo? GetMatchInfo(int serverId)
    {
        try
        {
            var logsDir = _config.HonData.HonLogsDirectory;
            if (string.IsNullOrEmpty(logsDir) || !Directory.Exists(logsDir))
            {
                _logger.LogDebug("Logs directory not configured or doesn't exist: {Dir}", logsDir);
                return null;
            }
            
            // Find match log files: M*.log (match logs)
            var matchFiles = Directory.GetFiles(logsDir, "M*.log");
            
            if (matchFiles.Length == 0)
            {
                _logger.LogDebug("No match log files found in {Dir}", logsDir);
                return null;
            }
            
            // Get the most recent match log file
            var latestFile = matchFiles
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTime)
                .First()
                .FullName;
            
            _logger.LogDebug("Reading match log file: {File}", latestFile);
            
            // Read the file with UTF-16-LE encoding (HoN uses this)
            var content = File.ReadAllText(latestFile, Encoding.Unicode);
            
            var matchInfo = new MatchInfo();
            
            // Extract match name
            var matchNameMatch = MatchNamePattern.Match(content);
            if (matchNameMatch.Success)
            {
                matchInfo.Map = matchNameMatch.Groups[1].Value.ToLower();
            }
            
            // Extract map name
            var mapNameMatch = MapNamePattern.Match(content);
            if (mapNameMatch.Success)
            {
                matchInfo.Name = mapNameMatch.Groups[1].Value.ToLower();
            }
            
            // Extract mode
            var modeMatch = ModePattern.Match(content);
            if (modeMatch.Success)
            {
                // Remove "Mode_" prefix and convert to lowercase
                matchInfo.Mode = modeMatch.Groups[1].Value
                    .Replace("Mode_", "")
                    .ToLower();
            }
            
            _logger.LogDebug("Match info - Mode: {Mode}, Map: {Map}, Name: {Name}", 
                matchInfo.Mode, matchInfo.Map, matchInfo.Name);
            
            return matchInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading match log for server #{Id}", serverId);
            return null;
        }
    }
}
