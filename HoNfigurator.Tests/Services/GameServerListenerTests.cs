using FluentAssertions;
using HoNfigurator.Core.Connectors;
using HoNfigurator.Core.Discord;
using HoNfigurator.Core.Models;
using HoNfigurator.Core.Services;
using HoNfigurator.Core.Statistics;
using HoNfigurator.GameServer.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HoNfigurator.Tests.Services;

/// <summary>
/// Tests for GameServerListener - TCP listener for game server status
/// </summary>
public class GameServerListenerTests
{
    private readonly Mock<ILogger<GameServerListener>> _loggerMock;
    private readonly Mock<IGameServerManager> _serverManagerMock;
    private readonly Mock<IGameLogReader> _logReaderMock;

    public GameServerListenerTests()
    {
        _loggerMock = new Mock<ILogger<GameServerListener>>();
        _serverManagerMock = new Mock<IGameServerManager>();
        _logReaderMock = new Mock<IGameLogReader>();
    }

    private GameServerListener CreateService(HoNConfiguration? config = null)
    {
        config ??= CreateTestConfig();
        return new GameServerListener(
            _loggerMock.Object,
            _serverManagerMock.Object,
            _logReaderMock.Object,
            config);
    }

    private HoNConfiguration CreateTestConfig()
    {
        return new HoNConfiguration
        {
            HonData = new HoNData
            {
                ServerName = "TestServer",
                StartingGamePort = 11000
            }
        };
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidConfig_ShouldInitialize()
    {
        // Act
        var service = CreateService();

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithOptionalServices_ShouldInitialize()
    {
        // Arrange
        var mqttMock = new Mock<IMqttHandler>();
        var discordMock = new Mock<IDiscordBotService>();
        var statsMock = new Mock<IMatchStatisticsService>();
        var replayMock = new Mock<ReplayManager>(
            Mock.Of<ILogger<ReplayManager>>(),
            "replays");

        // Act
        var service = new GameServerListener(
            _loggerMock.Object,
            _serverManagerMock.Object,
            _logReaderMock.Object,
            CreateTestConfig(),
            mqttMock.Object,
            discordMock.Object,
            statsMock.Object,
            replayMock.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullOptionalServices_ShouldInitialize()
    {
        // Act
        var service = new GameServerListener(
            _loggerMock.Object,
            _serverManagerMock.Object,
            _logReaderMock.Object,
            CreateTestConfig(),
            null, null, null, null);

        // Assert
        service.Should().NotBeNull();
    }

    #endregion

    #region IsListening Tests

    [Fact]
    public void IsListening_Initially_ShouldBeFalse()
    {
        // Arrange
        var service = CreateService();

        // Assert
        service.IsListening.Should().BeFalse();
    }

    #endregion

    #region StartAsync Tests

    [Fact]
    public async Task StartAsync_ShouldStartListening()
    {
        // Arrange
        var service = CreateService();
        var port = GetFreePort();

        // Act
        await service.StartAsync(port);

        // Assert
        service.IsListening.Should().BeTrue();

        // Cleanup
        await service.StopAsync();
    }

    [Fact]
    public async Task StartAsync_WhenAlreadyListening_ShouldNotThrow()
    {
        // Arrange
        var service = CreateService();
        var port = GetFreePort();
        await service.StartAsync(port);

        // Act
        var act = async () => await service.StartAsync(port);

        // Assert
        await act.Should().NotThrowAsync();

        // Cleanup
        await service.StopAsync();
    }

    [Fact]
    public async Task StartAsync_WithInvalidPort_ShouldThrow()
    {
        // Arrange
        var service = CreateService();

        // Act
        var act = async () => await service.StartAsync(-1);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region StopAsync Tests

    [Fact]
    public async Task StopAsync_WhenListening_ShouldStop()
    {
        // Arrange
        var service = CreateService();
        var port = GetFreePort();
        await service.StartAsync(port);

        // Act
        await service.StopAsync();

        // Assert
        service.IsListening.Should().BeFalse();
    }

    [Fact]
    public async Task StopAsync_WhenNotListening_ShouldNotThrow()
    {
        // Arrange
        var service = CreateService();

        // Act
        var act = async () => await service.StopAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region SendShutdownCommandAsync Tests

    [Fact]
    public async Task SendShutdownCommandAsync_WithNoConnection_ShouldReturnFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.SendShutdownCommandAsync(1);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region SendMessageAsync Tests

    [Fact]
    public async Task SendMessageAsync_WithNoConnection_ShouldReturnFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.SendMessageAsync(1, "Test message");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendMessageAsync_WithEmptyMessage_ShouldNotThrow()
    {
        // Arrange
        var service = CreateService();

        // Act
        var act = async () => await service.SendMessageAsync(1, "");

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region SendCommandAsync Tests

    [Fact]
    public async Task SendCommandAsync_WithNoConnection_ShouldReturnFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.SendCommandAsync(1, "status");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendCommandAsync_WithNullCommand_ShouldNotThrow()
    {
        // Arrange
        var service = CreateService();

        // Act
        var act = async () => await service.SendCommandAsync(1, null!);

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task StartAndStop_ShouldWork()
    {
        // Arrange
        var service = CreateService();
        var port = GetFreePort();

        // Act
        await service.StartAsync(port);
        var wasListening = service.IsListening;
        await service.StopAsync();

        // Assert
        wasListening.Should().BeTrue();
        service.IsListening.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private static int GetFreePort()
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    #endregion
}

/// <summary>
/// Tests for IGameServerListener interface
/// </summary>
public class GameServerListenerInterfaceTests
{
    [Fact]
    public void GameServerListener_ShouldImplementInterface()
    {
        // Assert
        typeof(GameServerListener).Should().Implement<IGameServerListener>();
    }

    [Fact]
    public void IGameServerListener_ShouldHaveRequiredMethods()
    {
        // Assert
        typeof(IGameServerListener).GetMethod("StartAsync").Should().NotBeNull();
        typeof(IGameServerListener).GetMethod("StopAsync").Should().NotBeNull();
        typeof(IGameServerListener).GetMethod("SendShutdownCommandAsync").Should().NotBeNull();
        typeof(IGameServerListener).GetMethod("SendMessageAsync").Should().NotBeNull();
        typeof(IGameServerListener).GetMethod("SendCommandAsync").Should().NotBeNull();
        typeof(IGameServerListener).GetProperty("IsListening").Should().NotBeNull();
    }
}
