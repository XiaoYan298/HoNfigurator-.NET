using FluentAssertions;
using HoNfigurator.Core.Connectors;
using HoNfigurator.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace HoNfigurator.Tests.Connectors;

/// <summary>
/// Tests for MqttHandler - MQTT message broker handler
/// </summary>
public class MqttHandlerTests : IDisposable
{
    private readonly Mock<ILogger<MqttHandler>> _mockLogger;

    public MqttHandlerTests()
    {
        _mockLogger = new Mock<ILogger<MqttHandler>>();
    }

    public void Dispose()
    {
    }

    private HoNConfiguration CreateConfig(bool mqttEnabled = true, string host = "localhost", int port = 1883)
    {
        return new HoNConfiguration
        {
            HonData = new HoNData { ServerName = "TestServer" },
            ApplicationData = new ApplicationData
            {
                Mqtt = new MqttSettings
                {
                    Enabled = mqttEnabled,
                    Host = host,
                    Port = port
                }
            }
        };
    }

    private HoNConfiguration CreateDisabledConfig()
    {
        return new HoNConfiguration
        {
            HonData = new HoNData { ServerName = "TestServer" },
            ApplicationData = new ApplicationData
            {
                Mqtt = new MqttSettings { Enabled = false }
            }
        };
    }

    private HoNConfiguration CreateNullMqttConfig()
    {
        return new HoNConfiguration
        {
            HonData = new HoNData { ServerName = "TestServer" },
            ApplicationData = new ApplicationData { Mqtt = null }
        };
    }

    #region MqttTopics Tests

    [Fact]
    public void MqttTopics_ServerStatus_ShouldHaveCorrectFormat()
    {
        MqttTopics.ServerStatus.Should().Be("server/{0}/status");
    }

    [Fact]
    public void MqttTopics_ServerMatch_ShouldHaveCorrectFormat()
    {
        MqttTopics.ServerMatch.Should().Be("server/{0}/match");
    }

    [Fact]
    public void MqttTopics_ServerPlayer_ShouldHaveCorrectFormat()
    {
        MqttTopics.ServerPlayer.Should().Be("server/{0}/player");
    }

    [Fact]
    public void MqttTopics_ManagerStatus_ShouldHaveCorrectFormat()
    {
        MqttTopics.ManagerStatus.Should().Be("manager/status");
    }

    [Fact]
    public void MqttTopics_ManagerAlert_ShouldHaveCorrectFormat()
    {
        MqttTopics.ManagerAlert.Should().Be("manager/alert");
    }

    [Fact]
    public void MqttTopics_ServerStatus_ShouldFormatWithServerId()
    {
        var topic = string.Format(MqttTopics.ServerStatus, 1);
        topic.Should().Be("server/1/status");
    }

    [Fact]
    public void MqttTopics_ServerMatch_ShouldFormatWithServerId()
    {
        var topic = string.Format(MqttTopics.ServerMatch, 5);
        topic.Should().Be("server/5/match");
    }

    [Fact]
    public void MqttTopics_ServerPlayer_ShouldFormatWithServerId()
    {
        var topic = string.Format(MqttTopics.ServerPlayer, 10);
        topic.Should().Be("server/10/player");
    }

    #endregion

    #region MqttEventTypes Tests

    [Fact]
    public void MqttEventTypes_ServerReady_ShouldHaveCorrectValue()
    {
        MqttEventTypes.ServerReady.Should().Be("server_ready");
    }

    [Fact]
    public void MqttEventTypes_ServerOccupied_ShouldHaveCorrectValue()
    {
        MqttEventTypes.ServerOccupied.Should().Be("server_occupied");
    }

    [Fact]
    public void MqttEventTypes_ServerOffline_ShouldHaveCorrectValue()
    {
        MqttEventTypes.ServerOffline.Should().Be("server_offline");
    }

    [Fact]
    public void MqttEventTypes_Heartbeat_ShouldHaveCorrectValue()
    {
        MqttEventTypes.Heartbeat.Should().Be("heartbeat");
    }

    [Fact]
    public void MqttEventTypes_LobbyCreated_ShouldHaveCorrectValue()
    {
        MqttEventTypes.LobbyCreated.Should().Be("lobby_created");
    }

    [Fact]
    public void MqttEventTypes_LobbyClosed_ShouldHaveCorrectValue()
    {
        MqttEventTypes.LobbyClosed.Should().Be("lobby_closed");
    }

    [Fact]
    public void MqttEventTypes_MatchStarted_ShouldHaveCorrectValue()
    {
        MqttEventTypes.MatchStarted.Should().Be("match_started");
    }

    [Fact]
    public void MqttEventTypes_MatchEnded_ShouldHaveCorrectValue()
    {
        MqttEventTypes.MatchEnded.Should().Be("match_ended");
    }

    [Fact]
    public void MqttEventTypes_PlayerJoined_ShouldHaveCorrectValue()
    {
        MqttEventTypes.PlayerJoined.Should().Be("player_joined");
    }

    [Fact]
    public void MqttEventTypes_PlayerLeft_ShouldHaveCorrectValue()
    {
        MqttEventTypes.PlayerLeft.Should().Be("player_left");
    }

    [Fact]
    public void MqttEventTypes_PlayerKicked_ShouldHaveCorrectValue()
    {
        MqttEventTypes.PlayerKicked.Should().Be("player_kicked");
    }

    [Fact]
    public void MqttEventTypes_ManagerOnline_ShouldHaveCorrectValue()
    {
        MqttEventTypes.ManagerOnline.Should().Be("online");
    }

    [Fact]
    public void MqttEventTypes_ManagerOffline_ShouldHaveCorrectValue()
    {
        MqttEventTypes.ManagerOffline.Should().Be("offline");
    }

    [Fact]
    public void MqttEventTypes_ManagerShutdown_ShouldHaveCorrectValue()
    {
        MqttEventTypes.ManagerShutdown.Should().Be("shutdown");
    }

    #endregion

    #region Service Creation Tests

    [Fact]
    public void Constructor_WithMqttDisabled_ShouldNotBeEnabled()
    {
        using var handler = new MqttHandler(_mockLogger.Object, CreateDisabledConfig());

        handler.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithMqttEnabled_ShouldBeEnabled()
    {
        using var handler = new MqttHandler(_mockLogger.Object, CreateConfig());

        handler.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithNullMqttSettings_ShouldNotBeEnabled()
    {
        using var handler = new MqttHandler(_mockLogger.Object, CreateNullMqttConfig());

        handler.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Constructor_ShouldNotBeConnectedInitially()
    {
        using var handler = new MqttHandler(_mockLogger.Object, CreateConfig());

        handler.IsConnected.Should().BeFalse();
    }

    #endregion

    #region ConnectAsync Tests

    [Fact]
    public async Task ConnectAsync_WhenDisabled_ShouldReturnFalse()
    {
        using var handler = new MqttHandler(_mockLogger.Object, CreateDisabledConfig());
        var result = await handler.ConnectAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ConnectAsync_WhenOptionsNotConfigured_ShouldReturnFalse()
    {
        using var handler = new MqttHandler(_mockLogger.Object, CreateNullMqttConfig());
        var result = await handler.ConnectAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ConnectAsync_WithInvalidBroker_ShouldReturnFalse()
    {
        var config = CreateConfig(host: "invalid.nonexistent.host.local");

        using var handler = new MqttHandler(_mockLogger.Object, config);
        var result = await handler.ConnectAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ConnectAsync_WithCancellation_ShouldNotThrow()
    {
        using var handler = new MqttHandler(_mockLogger.Object, CreateConfig());
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Connection attempt should handle cancellation gracefully
        await handler.Invoking(h => h.ConnectAsync(cts.Token))
            .Should().NotThrowAsync<TaskCanceledException>();
    }

    #endregion

    #region DisconnectAsync Tests

    [Fact]
    public async Task DisconnectAsync_WhenNotConnected_ShouldNotThrow()
    {
        using var handler = new MqttHandler(_mockLogger.Object, CreateDisabledConfig());

        await handler.Invoking(h => h.DisconnectAsync())
            .Should().NotThrowAsync();
    }

    #endregion

    #region PublishAsync Tests

    [Fact]
    public async Task PublishAsync_WhenNotConnected_ShouldNotThrow()
    {
        using var handler = new MqttHandler(_mockLogger.Object, CreateConfig());

        await handler.Invoking(h => h.PublishAsync("test/topic", "test message"))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_WithRetain_ShouldNotThrow()
    {
        using var handler = new MqttHandler(_mockLogger.Object, CreateConfig());

        await handler.Invoking(h => h.PublishAsync("test/topic", "test message", retain: true))
            .Should().NotThrowAsync();
    }

    #endregion

    #region PublishJsonAsync Tests

    [Fact]
    public async Task PublishJsonAsync_WhenNotConnected_ShouldNotThrow()
    {
        using var handler = new MqttHandler(_mockLogger.Object, CreateConfig());
        var testData = new { Name = "Test", Value = 123 };

        await handler.Invoking(h => h.PublishJsonAsync("test/topic", testData))
            .Should().NotThrowAsync();
    }

    #endregion

    #region PublishServerStatusAsync Tests

    [Fact]
    public async Task PublishServerStatusAsync_WhenNotConnected_ShouldNotThrow()
    {
        using var handler = new MqttHandler(_mockLogger.Object, CreateConfig());

        await handler.Invoking(h => h.PublishServerStatusAsync(1, MqttEventTypes.ServerReady))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishServerStatusAsync_WithData_ShouldNotThrow()
    {
        using var handler = new MqttHandler(_mockLogger.Object, CreateConfig());
        var data = new { PlayerCount = 5, MaxPlayers = 10 };

        await handler.Invoking(h => h.PublishServerStatusAsync(1, MqttEventTypes.ServerOccupied, data))
            .Should().NotThrowAsync();
    }

    #endregion

    #region PublishMatchEventAsync Tests

    [Fact]
    public async Task PublishMatchEventAsync_WhenNotConnected_ShouldNotThrow()
    {
        using var handler = new MqttHandler(_mockLogger.Object, CreateConfig());

        await handler.Invoking(h => h.PublishMatchEventAsync(1, MqttEventTypes.MatchStarted))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishMatchEventAsync_WithData_ShouldNotThrow()
    {
        using var handler = new MqttHandler(_mockLogger.Object, CreateConfig());
        var data = new { MatchId = 12345, Map = "caldavar" };

        await handler.Invoking(h => h.PublishMatchEventAsync(1, MqttEventTypes.MatchEnded, data))
            .Should().NotThrowAsync();
    }

    #endregion

    #region PublishPlayerEventAsync Tests

    [Fact]
    public async Task PublishPlayerEventAsync_WhenNotConnected_ShouldNotThrow()
    {
        using var handler = new MqttHandler(_mockLogger.Object, CreateConfig());

        await handler.Invoking(h => h.PublishPlayerEventAsync(1, MqttEventTypes.PlayerJoined, "TestPlayer"))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishPlayerEventAsync_WithData_ShouldNotThrow()
    {
        using var handler = new MqttHandler(_mockLogger.Object, CreateConfig());
        var data = new { Team = "Legion", Slot = 1 };

        await handler.Invoking(h => h.PublishPlayerEventAsync(1, MqttEventTypes.PlayerJoined, "TestPlayer", data))
            .Should().NotThrowAsync();
    }

    #endregion

    #region PublishManagerEventAsync Tests

    [Fact]
    public async Task PublishManagerEventAsync_WhenNotConnected_ShouldNotThrow()
    {
        using var handler = new MqttHandler(_mockLogger.Object, CreateConfig());

        await handler.Invoking(h => h.PublishManagerEventAsync(MqttEventTypes.ManagerOnline))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishManagerEventAsync_WithData_ShouldNotThrow()
    {
        using var handler = new MqttHandler(_mockLogger.Object, CreateConfig());
        var data = new { Version = "1.0.0", Uptime = 3600 };

        await handler.Invoking(h => h.PublishManagerEventAsync(MqttEventTypes.ManagerOnline, data))
            .Should().NotThrowAsync();
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        var handler = new MqttHandler(_mockLogger.Object, CreateConfig());

        handler.Invoking(h => h.Dispose()).Should().NotThrow();
    }

    [Fact]
    public void Dispose_MultipleCalls_ShouldNotThrow()
    {
        var handler = new MqttHandler(_mockLogger.Object, CreateConfig());

        handler.Dispose();
        handler.Invoking(h => h.Dispose()).Should().NotThrow();
    }

    #endregion

    #region Configuration Edge Cases

    [Fact]
    public void Constructor_WithCredentials_ShouldBeEnabled()
    {
        var config = new HoNConfiguration
        {
            HonData = new HoNData { ServerName = "TestServer" },
            ApplicationData = new ApplicationData
            {
                Mqtt = new MqttSettings
                {
                    Enabled = true,
                    Host = "localhost",
                    Port = 1883,
                    Username = "testuser",
                    Password = "testpass"
                }
            }
        };

        using var handler = new MqttHandler(_mockLogger.Object, config);

        handler.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithTls_ShouldBeEnabled()
    {
        var config = new HoNConfiguration
        {
            HonData = new HoNData { ServerName = "TestServer" },
            ApplicationData = new ApplicationData
            {
                Mqtt = new MqttSettings
                {
                    Enabled = true,
                    Host = "localhost",
                    Port = 8883,
                    UseTls = true
                }
            }
        };

        using var handler = new MqttHandler(_mockLogger.Object, config);

        handler.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithCustomTopicPrefix_ShouldBeEnabled()
    {
        var config = new HoNConfiguration
        {
            HonData = new HoNData { ServerName = "TestServer" },
            ApplicationData = new ApplicationData
            {
                Mqtt = new MqttSettings
                {
                    Enabled = true,
                    Host = "localhost",
                    Port = 1883,
                    TopicPrefix = "custom/prefix"
                }
            }
        };

        using var handler = new MqttHandler(_mockLogger.Object, config);

        handler.IsEnabled.Should().BeTrue();
    }

    #endregion
}
