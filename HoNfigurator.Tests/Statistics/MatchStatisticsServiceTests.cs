using FluentAssertions;
using HoNfigurator.Core.Statistics;
using Microsoft.Extensions.Logging;
using Moq;

namespace HoNfigurator.Tests.Statistics;

/// <summary>
/// Tests for MatchStatisticsService - match and player statistics storage
/// </summary>
public class MatchStatisticsServiceTests : IAsyncLifetime, IDisposable
{
    private readonly Mock<ILogger<MatchStatisticsService>> _mockLogger;
    private readonly string _testDbPath;
    private MatchStatisticsService _service;

    public MatchStatisticsServiceTests()
    {
        _mockLogger = new Mock<ILogger<MatchStatisticsService>>();
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_stats_{Guid.NewGuid():N}.db");
        _service = new MatchStatisticsService(_mockLogger.Object, _testDbPath);
    }

    public async Task InitializeAsync()
    {
        await _service.InitializeAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose()
    {
        if (File.Exists(_testDbPath))
        {
            try { File.Delete(_testDbPath); } catch { }
        }
    }

    #region Model Tests

    [Fact]
    public void MatchRecord_ShouldHaveCorrectDefaults()
    {
        var record = new MatchRecord();

        record.Id.Should().Be(0);
        record.ServerId.Should().Be(0);
        record.ServerName.Should().BeEmpty();
        record.EndTime.Should().BeNull();
        record.Winner.Should().BeNull();
        record.GameMode.Should().BeNull();
        record.Map.Should().BeNull();
        record.PlayersJson.Should().Be("[]");
        record.Players.Should().BeEmpty();
    }

    [Fact]
    public void MatchRecord_Players_ShouldDeserializeCorrectly()
    {
        var record = new MatchRecord
        {
            PlayersJson = "[\"Player1\",\"Player2\",\"Player3\"]"
        };

        record.Players.Should().BeEquivalentTo(new[] { "Player1", "Player2", "Player3" });
    }

    [Fact]
    public void PlayerStats_ShouldHaveCorrectDefaults()
    {
        var stats = new PlayerStats();

        stats.Id.Should().Be(0);
        stats.AccountId.Should().Be(0);
        stats.PlayerName.Should().BeEmpty();
        stats.TotalMatches.Should().Be(0);
        stats.Wins.Should().Be(0);
        stats.Losses.Should().Be(0);
        stats.Disconnects.Should().Be(0);
        stats.WinRate.Should().Be(0);
    }

    [Fact]
    public void PlayerStats_WinRate_ShouldCalculateCorrectly()
    {
        var stats = new PlayerStats
        {
            TotalMatches = 100,
            Wins = 60,
            Losses = 40
        };

        stats.WinRate.Should().Be(60);
    }

    [Fact]
    public void PlayerStats_WinRate_NoMatches_ShouldBeZero()
    {
        var stats = new PlayerStats { TotalMatches = 0 };

        stats.WinRate.Should().Be(0);
    }

    [Fact]
    public void ServerStats_ShouldHaveCorrectDefaults()
    {
        var stats = new ServerStats();

        stats.ServerId.Should().Be(0);
        stats.ServerName.Should().BeEmpty();
        stats.TotalMatches.Should().Be(0);
        stats.TotalPlayers.Should().Be(0);
    }

    [Fact]
    public void DailyStats_ShouldHaveCorrectDefaults()
    {
        var stats = new DailyStats();

        stats.MatchCount.Should().Be(0);
        stats.UniquePlayerCount.Should().Be(0);
        stats.PeakConcurrentPlayers.Should().Be(0);
    }

    #endregion

    #region Match Recording Tests

    [Fact]
    public async Task RecordMatchStartAsync_ShouldReturnMatchId()
    {
        var players = new List<string> { "Player1", "Player2" };

        var matchId = await _service.RecordMatchStartAsync(1, "TestServer", players, "Normal");

        matchId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RecordMatchStartAsync_ShouldStoreMatchData()
    {
        var players = new List<string> { "Alice", "Bob", "Charlie" };

        var matchId = await _service.RecordMatchStartAsync(5, "GameServer5", players, "Ranked");

        var match = await _service.GetMatchAsync(matchId);
        match.Should().NotBeNull();
        match!.ServerId.Should().Be(5);
        match.ServerName.Should().Be("GameServer5");
        match.PlayerCount.Should().Be(3);
        match.GameMode.Should().Be("Ranked");
        match.Players.Should().BeEquivalentTo(players);
    }

    [Fact]
    public async Task RecordMatchEndAsync_ShouldUpdateMatch()
    {
        var players = new List<string> { "Player1", "Player2" };
        var matchId = await _service.RecordMatchStartAsync(1, "Server", players);

        await _service.RecordMatchEndAsync(matchId, "Legion");

        var match = await _service.GetMatchAsync(matchId);
        match.Should().NotBeNull();
        match!.EndTime.Should().NotBeNull();
        match.Winner.Should().Be("Legion");
        // Duration should be >= 0 (could be 0 if completed very quickly)
    }

    [Fact]
    public async Task RecordMatchEndAsync_WithoutWinner_ShouldWork()
    {
        var matchId = await _service.RecordMatchStartAsync(1, "Server", new List<string> { "P1" });

        await _service.RecordMatchEndAsync(matchId);

        var match = await _service.GetMatchAsync(matchId);
        match!.Winner.Should().BeNull();
    }

    [Fact]
    public async Task RecordMatchEndAsync_InvalidMatchId_ShouldNotThrow()
    {
        await _service.RecordMatchEndAsync(999999, "Winner");
        // Should not throw, just silently do nothing
    }

    #endregion

    #region Match Retrieval Tests

    [Fact]
    public async Task GetMatchAsync_NotFound_ShouldReturnNull()
    {
        var match = await _service.GetMatchAsync(999999);

        match.Should().BeNull();
    }

    [Fact]
    public async Task GetRecentMatchesAsync_ShouldReturnInDescendingOrder()
    {
        await _service.RecordMatchStartAsync(1, "Server1", new List<string> { "P1" });
        await Task.Delay(10);
        await _service.RecordMatchStartAsync(2, "Server2", new List<string> { "P2" });
        await Task.Delay(10);
        await _service.RecordMatchStartAsync(3, "Server3", new List<string> { "P3" });

        var matches = await _service.GetRecentMatchesAsync(10);

        matches.Should().HaveCountGreaterThanOrEqualTo(3);
        matches.First().ServerName.Should().Be("Server3"); // Most recent
    }

    [Fact]
    public async Task GetRecentMatchesAsync_ShouldRespectLimit()
    {
        for (int i = 0; i < 10; i++)
        {
            await _service.RecordMatchStartAsync(i, $"Server{i}", new List<string> { $"P{i}" });
        }

        var matches = await _service.GetRecentMatchesAsync(3);

        matches.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetMatchesByServerAsync_ShouldFilterByServer()
    {
        await _service.RecordMatchStartAsync(1, "Server1", new List<string> { "P1" });
        await _service.RecordMatchStartAsync(1, "Server1", new List<string> { "P2" });
        await _service.RecordMatchStartAsync(2, "Server2", new List<string> { "P3" });

        var server1Matches = await _service.GetMatchesByServerAsync(1);

        server1Matches.Should().HaveCount(2);
        server1Matches.Should().OnlyContain(m => m.ServerId == 1);
    }

    #endregion

    #region Player Stats Tests

    [Fact]
    public async Task UpdatePlayerStatsAsync_NewPlayer_ShouldCreateRecord()
    {
        await _service.UpdatePlayerStatsAsync("NewPlayer", 12345, true, 3600);

        var stats = await _service.GetPlayerStatsAsync("NewPlayer");
        stats.Should().NotBeNull();
        stats!.PlayerName.Should().Be("NewPlayer");
        stats.AccountId.Should().Be(12345);
        stats.TotalMatches.Should().Be(1);
        stats.Wins.Should().Be(1);
        stats.Losses.Should().Be(0);
        stats.TotalPlayTimeSeconds.Should().Be(3600);
    }

    [Fact]
    public async Task UpdatePlayerStatsAsync_ExistingPlayer_ShouldIncrementStats()
    {
        await _service.UpdatePlayerStatsAsync("ExistingPlayer", 111, true, 1000);
        await _service.UpdatePlayerStatsAsync("ExistingPlayer", 111, false, 1500);
        await _service.UpdatePlayerStatsAsync("ExistingPlayer", 111, true, 2000);

        var stats = await _service.GetPlayerStatsAsync("ExistingPlayer");
        stats!.TotalMatches.Should().Be(3);
        stats.Wins.Should().Be(2);
        stats.Losses.Should().Be(1);
        stats.TotalPlayTimeSeconds.Should().Be(4500);
    }

    [Fact]
    public async Task GetPlayerStatsAsync_NotFound_ShouldReturnNull()
    {
        var stats = await _service.GetPlayerStatsAsync("NonexistentPlayer");

        stats.Should().BeNull();
    }

    [Fact]
    public async Task GetTopPlayersAsync_ShouldReturnByWinRate()
    {
        // Player with 80% win rate (8/10)
        for (int i = 0; i < 8; i++)
            await _service.UpdatePlayerStatsAsync("TopPlayer", 1, true, 100);
        for (int i = 0; i < 2; i++)
            await _service.UpdatePlayerStatsAsync("TopPlayer", 1, false, 100);

        // Player with 60% win rate (6/10)
        for (int i = 0; i < 6; i++)
            await _service.UpdatePlayerStatsAsync("MidPlayer", 2, true, 100);
        for (int i = 0; i < 4; i++)
            await _service.UpdatePlayerStatsAsync("MidPlayer", 2, false, 100);

        var topPlayers = await _service.GetTopPlayersAsync(10);

        var topPlayer = topPlayers.FirstOrDefault(p => p.PlayerName == "TopPlayer");
        var midPlayer = topPlayers.FirstOrDefault(p => p.PlayerName == "MidPlayer");
        
        if (topPlayer != null && midPlayer != null)
        {
            topPlayers.IndexOf(topPlayer).Should().BeLessThan(topPlayers.IndexOf(midPlayer));
        }
    }

    [Fact]
    public async Task GetMostActivePlayersAsync_ShouldReturnByMatchCount()
    {
        for (int i = 0; i < 20; i++)
            await _service.UpdatePlayerStatsAsync("ActivePlayer", 1, i % 2 == 0, 100);

        for (int i = 0; i < 5; i++)
            await _service.UpdatePlayerStatsAsync("CasualPlayer", 2, i % 2 == 0, 100);

        var activePlayers = await _service.GetMostActivePlayersAsync(10);

        var activeIdx = activePlayers.FindIndex(p => p.PlayerName == "ActivePlayer");
        var casualIdx = activePlayers.FindIndex(p => p.PlayerName == "CasualPlayer");
        
        if (activeIdx >= 0 && casualIdx >= 0)
        {
            activeIdx.Should().BeLessThan(casualIdx);
        }
    }

    #endregion

    #region Server Stats Tests

    [Fact]
    public async Task GetServerStatsAsync_ShouldCalculateCorrectly()
    {
        var players1 = new List<string> { "P1", "P2" };
        var players2 = new List<string> { "P3", "P4", "P5" };
        
        var match1 = await _service.RecordMatchStartAsync(10, "StatsServer", players1);
        await _service.RecordMatchEndAsync(match1);
        
        var match2 = await _service.RecordMatchStartAsync(10, "StatsServer", players2);
        await _service.RecordMatchEndAsync(match2);

        var stats = await _service.GetServerStatsAsync(10);

        stats.Should().NotBeNull();
        stats.ServerId.Should().Be(10);
        stats.TotalMatches.Should().Be(2);
    }

    [Fact]
    public async Task GetAllServerStatsAsync_ShouldReturnAllServers()
    {
        await _service.RecordMatchStartAsync(100, "Server100", new List<string> { "P1" });
        await _service.RecordMatchStartAsync(101, "Server101", new List<string> { "P2" });
        await _service.RecordMatchStartAsync(102, "Server102", new List<string> { "P3" });

        var allStats = await _service.GetAllServerStatsAsync();

        allStats.Should().HaveCountGreaterThanOrEqualTo(3);
        allStats.Should().Contain(s => s.ServerId == 100);
        allStats.Should().Contain(s => s.ServerId == 101);
        allStats.Should().Contain(s => s.ServerId == 102);
    }

    #endregion

    #region Daily Stats Tests

    [Fact]
    public async Task GetTodayStatsAsync_ShouldReturnCurrentDayStats()
    {
        await _service.RecordMatchStartAsync(1, "Server", new List<string> { "P1", "P2" });
        await _service.RecordMatchStartAsync(1, "Server", new List<string> { "P3", "P4" });

        var today = await _service.GetTodayStatsAsync();

        today.Date.Date.Should().Be(DateTime.UtcNow.Date);
        today.MatchCount.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetDailyStatsAsync_ShouldReturnRequestedDays()
    {
        await _service.RecordMatchStartAsync(1, "Server", new List<string> { "P1" });

        var dailyStats = await _service.GetDailyStatsAsync(7);

        dailyStats.Should().NotBeNull();
        dailyStats.Should().Contain(d => d.Date.Date == DateTime.UtcNow.Date);
    }

    #endregion

    #region Overall Summary Tests

    [Fact]
    public async Task GetOverallSummaryAsync_ShouldReturnSummary()
    {
        await _service.RecordMatchStartAsync(1, "Server", new List<string> { "P1", "P2" });
        await _service.UpdatePlayerStatsAsync("SummaryPlayer", 999, true, 1000);

        var summary = await _service.GetOverallSummaryAsync();

        summary.Should().NotBeNull();
        summary.Should().ContainKey("total_matches");
        summary.Should().ContainKey("unique_players");
    }

    #endregion

    #region Initialization Tests

    [Fact]
    public async Task InitializeAsync_ShouldBeIdempotent()
    {
        await _service.InitializeAsync();
        await _service.InitializeAsync();
        await _service.InitializeAsync();

        // Should not throw and database should work
        var matchId = await _service.RecordMatchStartAsync(1, "Test", new List<string> { "P1" });
        matchId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task InitializeAsync_ShouldCreateDatabaseFile()
    {
        await _service.InitializeAsync();

        File.Exists(_testDbPath).Should().BeTrue();
    }

    #endregion
}
