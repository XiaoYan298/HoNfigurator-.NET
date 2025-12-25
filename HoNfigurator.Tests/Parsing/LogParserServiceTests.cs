using FluentAssertions;
using HoNfigurator.Core.Models;
using HoNfigurator.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HoNfigurator.Tests.Parsing;

/// <summary>
/// Tests for LogParserService - game server log parsing functionality
/// </summary>
public class LogParserServiceTests : IDisposable
{
    private readonly Mock<ILogger<LogParserService>> _mockLogger;
    private readonly HoNConfiguration _config;
    private readonly LogParserService _service;
    private readonly string _testDir;

    public LogParserServiceTests()
    {
        _mockLogger = new Mock<ILogger<LogParserService>>();
        _config = new HoNConfiguration();
        _service = new LogParserService(_mockLogger.Object, _config);
        _testDir = Path.Combine(Path.GetTempPath(), $"LogParserTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            try { Directory.Delete(_testDir, true); } catch { }
        }
    }

    private string CreateTestLog(string content)
    {
        var path = Path.Combine(_testDir, $"test_{Guid.NewGuid():N}.log");
        File.WriteAllText(path, content);
        return path;
    }

    #region ParseLogFileAsync Tests

    [Fact]
    public async Task ParseLogFileAsync_FileNotFound_ShouldReturnFailure()
    {
        var result = await _service.ParseLogFileAsync("/nonexistent/path.log");

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task ParseLogFileAsync_EmptyFile_ShouldReturnSuccess()
    {
        var logPath = CreateTestLog("");

        var result = await _service.ParseLogFileAsync(logPath);

        result.Success.Should().BeTrue();
        result.TotalLines.Should().Be(0);
    }

    [Fact]
    public async Task ParseLogFileAsync_WithMatchId_ShouldExtract()
    {
        var content = "[2024-01-15 10:30:00] Server started\nMatch ID: 123456\n[2024-01-15 10:31:00] Game starting";
        var logPath = CreateTestLog(content);

        var result = await _service.ParseLogFileAsync(logPath);

        result.Success.Should().BeTrue();
        result.MatchId.Should().Be(123456);
    }

    [Fact]
    public async Task ParseLogFileAsync_WithPlayerConnect_ShouldExtract()
    {
        var content = "Player connected: TestPlayer (12345) from 192.168.1.100:27015";
        var logPath = CreateTestLog(content);

        var result = await _service.ParseLogFileAsync(logPath);

        result.Success.Should().BeTrue();
        result.PlayerConnections.Should().HaveCount(1);
        result.PlayerConnections[0].PlayerName.Should().Be("TestPlayer");
        result.PlayerConnections[0].PlayerId.Should().Be(12345);
        result.PlayerConnections[0].IpAddress.Should().Be("192.168.1.100");
    }

    [Fact]
    public async Task ParseLogFileAsync_WithMultiplePlayerConnects_ShouldExtractAll()
    {
        var content = @"Player connected: Player1 (111) from 10.0.0.1:27015
Player connected: Player2 (222) from 10.0.0.2:27015
Player connected: Player3 (333) from 10.0.0.3:27015";
        var logPath = CreateTestLog(content);

        var result = await _service.ParseLogFileAsync(logPath);

        result.PlayerConnections.Should().HaveCount(3);
    }

    [Fact]
    public async Task ParseLogFileAsync_WithPlayerDisconnect_ShouldExtract()
    {
        var content = "Player disconnected: TestPlayer (12345)";
        var logPath = CreateTestLog(content);

        var result = await _service.ParseLogFileAsync(logPath);

        result.Success.Should().BeTrue();
        result.PlayerDisconnections.Should().HaveCount(1);
        result.PlayerDisconnections[0].PlayerName.Should().Be("TestPlayer");
        result.PlayerDisconnections[0].PlayerId.Should().Be(12345);
    }

    [Fact]
    public async Task ParseLogFileAsync_WithMatchEnd_ShouldExtract()
    {
        var content = "Match ended. Winner: Legion";
        var logPath = CreateTestLog(content);

        var result = await _service.ParseLogFileAsync(logPath);

        result.Success.Should().BeTrue();
        result.MatchResult.Should().Be("Legion");
    }

    [Theory]
    [InlineData("Legion")]
    [InlineData("Hellbourne")]
    [InlineData("Draw")]
    public async Task ParseLogFileAsync_WithDifferentWinners_ShouldExtract(string winner)
    {
        var content = $"Match ended. Winner: {winner}";
        var logPath = CreateTestLog(content);

        var result = await _service.ParseLogFileAsync(logPath);

        result.MatchResult.Should().Be(winner);
    }

    [Fact]
    public async Task ParseLogFileAsync_WithMap_ShouldExtract()
    {
        var content = "Starting map: caldavar";
        var logPath = CreateTestLog(content);

        var result = await _service.ParseLogFileAsync(logPath);

        result.MapName.Should().Be("caldavar");
    }

    [Fact]
    public async Task ParseLogFileAsync_WithGameMode_ShouldExtract()
    {
        var content = "Game mode: normal";
        var logPath = CreateTestLog(content);

        var result = await _service.ParseLogFileAsync(logPath);

        result.GameMode.Should().Be("normal");
    }

    [Fact]
    public async Task ParseLogFileAsync_WithServerName_ShouldExtract()
    {
        var content = "Server name: My Test Server";
        var logPath = CreateTestLog(content);

        // Note: ServerName is extracted via ExtractGameInfoAsync, not ParseLogFileAsync
        var result = await _service.ExtractGameInfoAsync(logPath);

        result.Should().NotBeNull();
        result!.ServerName.Should().Be("My Test Server");
    }

    [Fact]
    public async Task ParseLogFileAsync_WithErrors_ShouldExtract()
    {
        var content = @"[2024-01-15 10:30:00] Normal operation
ERROR: Failed to load resource
[2024-01-15 10:30:01] Continuing
ERROR: Another error occurred";
        var logPath = CreateTestLog(content);

        var result = await _service.ParseLogFileAsync(logPath);

        result.Errors.Should().HaveCount(2);
        result.Errors[0].Message.Should().Contain("Failed to load resource");
        result.Errors[1].Message.Should().Contain("Another error occurred");
    }

    [Fact]
    public async Task ParseLogFileAsync_WithWarnings_ShouldExtract()
    {
        var content = @"[2024-01-15 10:30:00] Normal operation
WARNING: Low memory warning
WARNING: High CPU usage";
        var logPath = CreateTestLog(content);

        var result = await _service.ParseLogFileAsync(logPath);

        result.Warnings.Should().HaveCount(2);
    }

    [Fact]
    public async Task ParseLogFileAsync_WithTimestamp_ShouldExtract()
    {
        var content = "[2024-01-15 10:30:45] Server started";
        var logPath = CreateTestLog(content);

        var result = await _service.ParseLogFileAsync(logPath);

        result.Success.Should().BeTrue();
    }

    #endregion

    #region ExtractPlayerConnectionsAsync Tests

    [Fact]
    public async Task ExtractPlayerConnectionsAsync_FileNotFound_ShouldReturnEmpty()
    {
        var result = await _service.ExtractPlayerConnectionsAsync("/nonexistent/path.log");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractPlayerConnectionsAsync_NoConnections_ShouldReturnEmpty()
    {
        var logPath = CreateTestLog("Some log content without player connections");

        var result = await _service.ExtractPlayerConnectionsAsync(logPath);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractPlayerConnectionsAsync_WithConnections_ShouldExtract()
    {
        var content = @"Player connected: Player1 (111) from 192.168.1.1:27015
Player connected: Player2 (222) from 192.168.1.2:27015";
        var logPath = CreateTestLog(content);

        var result = await _service.ExtractPlayerConnectionsAsync(logPath);

        result.Should().HaveCount(2);
        result[0].PlayerName.Should().Be("Player1");
        result[0].PlayerId.Should().Be(111);
        result[0].IpAddress.Should().Be("192.168.1.1");
    }

    #endregion

    #region ExtractMatchIdAsync Tests

    [Fact]
    public async Task ExtractMatchIdAsync_FileNotFound_ShouldReturnNull()
    {
        var result = await _service.ExtractMatchIdAsync("/nonexistent/path.log");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ExtractMatchIdAsync_NoMatchId_ShouldReturnNull()
    {
        var logPath = CreateTestLog("Some log content without match ID");

        var result = await _service.ExtractMatchIdAsync(logPath);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ExtractMatchIdAsync_WithMatchId_ShouldExtract()
    {
        var logPath = CreateTestLog("Some content\nMatch ID: 999888777\nMore content");

        var result = await _service.ExtractMatchIdAsync(logPath);

        result.Should().Be(999888777);
    }

    [Fact]
    public async Task ExtractMatchIdAsync_WithMultipleMatchIds_ShouldReturnFirst()
    {
        var content = @"Match ID: 111111
Match ID: 222222";
        var logPath = CreateTestLog(content);

        var result = await _service.ExtractMatchIdAsync(logPath);

        result.Should().Be(111111);
    }

    #endregion

    #region ExtractGameInfoAsync Tests

    [Fact]
    public async Task ExtractGameInfoAsync_FileNotFound_ShouldReturnNull()
    {
        var result = await _service.ExtractGameInfoAsync("/nonexistent/path.log");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ExtractGameInfoAsync_NoGameInfo_ShouldReturnNull()
    {
        var logPath = CreateTestLog("Some log content without game info");

        var result = await _service.ExtractGameInfoAsync(logPath);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ExtractGameInfoAsync_WithMap_ShouldExtract()
    {
        var logPath = CreateTestLog("Starting map: caldavar");

        var result = await _service.ExtractGameInfoAsync(logPath);

        result.Should().NotBeNull();
        result!.MapName.Should().Be("caldavar");
    }

    [Fact]
    public async Task ExtractGameInfoAsync_WithGameMode_ShouldExtract()
    {
        var logPath = CreateTestLog("Game mode: lockpick");

        var result = await _service.ExtractGameInfoAsync(logPath);

        result.Should().NotBeNull();
        result!.GameMode.Should().Be("lockpick");
    }

    [Fact]
    public async Task ExtractGameInfoAsync_WithServerName_ShouldExtract()
    {
        var logPath = CreateTestLog("Server name: Epic Gaming Server");

        var result = await _service.ExtractGameInfoAsync(logPath);

        result.Should().NotBeNull();
        result!.ServerName.Should().Be("Epic Gaming Server");
    }

    [Fact]
    public async Task ExtractGameInfoAsync_WithAllInfo_ShouldExtractAll()
    {
        var content = @"Starting map: darkwood
Game mode: ranked
Server name: Competitive Server";
        var logPath = CreateTestLog(content);

        var result = await _service.ExtractGameInfoAsync(logPath);

        result.Should().NotBeNull();
        result!.MapName.Should().Be("darkwood");
        result.GameMode.Should().Be("ranked");
        result.ServerName.Should().Be("Competitive Server");
    }

    #endregion

    #region Complete Log Parsing Integration Tests

    [Fact]
    public async Task ParseLogFileAsync_CompleteGameSession_ShouldExtractAllInfo()
    {
        var content = @"[2024-01-15 10:00:00] Server starting...
Server name: Test Server
[2024-01-15 10:00:01] Initialization complete
Starting map: caldavar
Game mode: normal
[2024-01-15 10:01:00] Match ID: 12345678
Player connected: Alice (1001) from 192.168.1.10:27015
Player connected: Bob (1002) from 192.168.1.11:27015
[2024-01-15 10:30:00] Game in progress
WARNING: Memory usage high
[2024-01-15 10:45:00] Player disconnected: Bob (1002)
[2024-01-15 11:00:00] Match ended. Winner: Legion";
        var logPath = CreateTestLog(content);

        var result = await _service.ParseLogFileAsync(logPath);

        result.Success.Should().BeTrue();
        result.MatchId.Should().Be(12345678);
        result.MapName.Should().Be("caldavar");
        result.GameMode.Should().Be("normal");
        result.PlayerConnections.Should().HaveCount(2);
        result.PlayerDisconnections.Should().HaveCount(1);
        result.Warnings.Should().HaveCount(1);
        result.MatchResult.Should().Be("Legion");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task ParseLogFileAsync_WithSpecialCharactersInPlayerName_ShouldHandle()
    {
        var content = "Player connected: Test_Player-123 (999) from 10.0.0.1:27015";
        var logPath = CreateTestLog(content);

        var result = await _service.ParseLogFileAsync(logPath);

        result.PlayerConnections.Should().HaveCount(1);
        result.PlayerConnections[0].PlayerName.Should().Be("Test_Player-123");
    }

    [Fact]
    public async Task ParseLogFileAsync_WithLargeNumbers_ShouldHandle()
    {
        var content = "Match ID: 9999999999";
        var logPath = CreateTestLog(content);

        var result = await _service.ParseLogFileAsync(logPath);

        result.MatchId.Should().Be(9999999999);
    }

    #endregion
}
