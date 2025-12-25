using FluentAssertions;
using HoNfigurator.Core.Discord;
using HoNfigurator.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace HoNfigurator.Tests.Discord;

/// <summary>
/// Tests for DiscordBotService - Discord notification bot
/// </summary>
public class DiscordBotServiceTests : IDisposable
{
    private readonly Mock<ILogger<DiscordBotService>> _mockLogger;

    public DiscordBotServiceTests()
    {
        _mockLogger = new Mock<ILogger<DiscordBotService>>();
    }

    public void Dispose()
    {
    }

    #region NotificationLevel Tests

    [Fact]
    public void NotificationLevel_ShouldContainExpectedValues()
    {
        var values = Enum.GetValues<NotificationLevel>();

        values.Should().Contain(NotificationLevel.Info);
        values.Should().Contain(NotificationLevel.Warning);
        values.Should().Contain(NotificationLevel.Error);
        values.Should().Contain(NotificationLevel.Success);
    }

    [Theory]
    [InlineData(NotificationLevel.Info, 0)]
    [InlineData(NotificationLevel.Warning, 1)]
    [InlineData(NotificationLevel.Error, 2)]
    [InlineData(NotificationLevel.Success, 3)]
    public void NotificationLevel_ShouldHaveCorrectValues(NotificationLevel level, int expectedValue)
    {
        ((int)level).Should().Be(expectedValue);
    }

    #endregion

    #region Service Creation Tests

    [Fact]
    public void Constructor_WithEmptyToken_ShouldNotBeEnabled()
    {
        var config = new HoNConfiguration
        {
            ApplicationData = new ApplicationData
            {
                Discord = new DiscordSettings { BotToken = "" }
            }
        };

        using var service = new DiscordBotService(_mockLogger.Object, config);

        service.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithNullToken_ShouldNotBeEnabled()
    {
        var config = new HoNConfiguration
        {
            ApplicationData = new ApplicationData
            {
                Discord = new DiscordSettings { BotToken = null }
            }
        };

        using var service = new DiscordBotService(_mockLogger.Object, config);

        service.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithNullDiscordSettings_ShouldNotBeEnabled()
    {
        var config = new HoNConfiguration
        {
            ApplicationData = new ApplicationData { Discord = null }
        };

        using var service = new DiscordBotService(_mockLogger.Object, config);

        service.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithToken_ShouldBeEnabled()
    {
        var config = new HoNConfiguration
        {
            ApplicationData = new ApplicationData
            {
                Discord = new DiscordSettings { BotToken = "test-token-12345" }
            }
        };

        using var service = new DiscordBotService(_mockLogger.Object, config);

        service.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Constructor_ShouldNotBeConnectedInitially()
    {
        var config = new HoNConfiguration
        {
            ApplicationData = new ApplicationData
            {
                Discord = new DiscordSettings { BotToken = "test-token" }
            }
        };

        using var service = new DiscordBotService(_mockLogger.Object, config);

        service.IsConnected.Should().BeFalse();
    }

    #endregion

    #region StartAsync Tests

    [Fact]
    public async Task StartAsync_WhenDisabled_ShouldNotThrow()
    {
        var config = new HoNConfiguration
        {
            ApplicationData = new ApplicationData
            {
                Discord = new DiscordSettings { BotToken = "" }
            }
        };

        using var service = new DiscordBotService(_mockLogger.Object, config);

        await service.Invoking(s => s.StartAsync())
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task StartAsync_WithInvalidToken_ShouldHandleError()
    {
        var config = new HoNConfiguration
        {
            ApplicationData = new ApplicationData
            {
                Discord = new DiscordSettings { BotToken = "invalid-token" }
            }
        };

        using var service = new DiscordBotService(_mockLogger.Object, config);

        // Should not throw - errors are caught internally
        await service.Invoking(s => s.StartAsync())
            .Should().NotThrowAsync();
    }

    #endregion

    #region StopAsync Tests

    [Fact]
    public async Task StopAsync_WhenNotStarted_ShouldNotThrow()
    {
        var config = new HoNConfiguration
        {
            ApplicationData = new ApplicationData
            {
                Discord = new DiscordSettings { BotToken = "" }
            }
        };

        using var service = new DiscordBotService(_mockLogger.Object, config);

        await service.Invoking(s => s.StopAsync())
            .Should().NotThrowAsync();
    }

    #endregion

    #region SendServerNotificationAsync Tests

    [Fact]
    public async Task SendServerNotificationAsync_WhenNotConnected_ShouldNotThrow()
    {
        var config = new HoNConfiguration
        {
            ApplicationData = new ApplicationData
            {
                Discord = new DiscordSettings { BotToken = "token", NotificationChannelId = "12345" }
            }
        };

        using var service = new DiscordBotService(_mockLogger.Object, config);

        await service.Invoking(s => s.SendServerNotificationAsync("Test", "Message"))
            .Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(NotificationLevel.Info)]
    [InlineData(NotificationLevel.Warning)]
    [InlineData(NotificationLevel.Error)]
    [InlineData(NotificationLevel.Success)]
    public async Task SendServerNotificationAsync_WithDifferentLevels_ShouldNotThrow(NotificationLevel level)
    {
        var config = new HoNConfiguration
        {
            ApplicationData = new ApplicationData
            {
                Discord = new DiscordSettings { BotToken = "token", NotificationChannelId = "12345" }
            }
        };

        using var service = new DiscordBotService(_mockLogger.Object, config);

        await service.Invoking(s => s.SendServerNotificationAsync("Test", "Message", level))
            .Should().NotThrowAsync();
    }

    #endregion

    #region SendMatchStartedAsync Tests

    [Fact]
    public async Task SendMatchStartedAsync_WhenNotConnected_ShouldNotThrow()
    {
        var config = new HoNConfiguration
        {
            ApplicationData = new ApplicationData
            {
                Discord = new DiscordSettings { BotToken = "token", NotificationChannelId = "12345" }
            }
        };

        using var service = new DiscordBotService(_mockLogger.Object, config);
        var players = new List<string> { "Player1", "Player2" };

        await service.Invoking(s => s.SendMatchStartedAsync(1, "TestServer", players))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendMatchStartedAsync_WithEmptyPlayers_ShouldNotThrow()
    {
        var config = new HoNConfiguration
        {
            ApplicationData = new ApplicationData
            {
                Discord = new DiscordSettings { BotToken = "token", NotificationChannelId = "12345" }
            }
        };

        using var service = new DiscordBotService(_mockLogger.Object, config);
        var players = new List<string>();

        await service.Invoking(s => s.SendMatchStartedAsync(1, "TestServer", players))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendMatchStartedAsync_WithManyPlayers_ShouldNotThrow()
    {
        var config = new HoNConfiguration
        {
            ApplicationData = new ApplicationData
            {
                Discord = new DiscordSettings { BotToken = "token", NotificationChannelId = "12345" }
            }
        };

        using var service = new DiscordBotService(_mockLogger.Object, config);
        var players = Enumerable.Range(1, 100).Select(i => $"Player{i}").ToList();

        await service.Invoking(s => s.SendMatchStartedAsync(1, "TestServer", players))
            .Should().NotThrowAsync();
    }

    #endregion

    #region SendMatchEndedAsync Tests

    [Fact]
    public async Task SendMatchEndedAsync_WhenNotConnected_ShouldNotThrow()
    {
        var config = new HoNConfiguration
        {
            ApplicationData = new ApplicationData
            {
                Discord = new DiscordSettings { BotToken = "token", NotificationChannelId = "12345" }
            }
        };

        using var service = new DiscordBotService(_mockLogger.Object, config);

        await service.Invoking(s => s.SendMatchEndedAsync(1, "TestServer", 3600, "Legion"))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendMatchEndedAsync_WithoutWinner_ShouldNotThrow()
    {
        var config = new HoNConfiguration
        {
            ApplicationData = new ApplicationData
            {
                Discord = new DiscordSettings { BotToken = "token", NotificationChannelId = "12345" }
            }
        };

        using var service = new DiscordBotService(_mockLogger.Object, config);

        await service.Invoking(s => s.SendMatchEndedAsync(1, "TestServer", 3600))
            .Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(60)]
    [InlineData(3600)]
    [InlineData(7200)]
    public async Task SendMatchEndedAsync_WithVariousDurations_ShouldNotThrow(int duration)
    {
        var config = new HoNConfiguration
        {
            ApplicationData = new ApplicationData
            {
                Discord = new DiscordSettings { BotToken = "token", NotificationChannelId = "12345" }
            }
        };

        using var service = new DiscordBotService(_mockLogger.Object, config);

        await service.Invoking(s => s.SendMatchEndedAsync(1, "TestServer", duration))
            .Should().NotThrowAsync();
    }

    #endregion

    #region SendPlayerJoinedAsync Tests

    [Fact]
    public async Task SendPlayerJoinedAsync_WhenNotConnected_ShouldNotThrow()
    {
        var config = new HoNConfiguration
        {
            ApplicationData = new ApplicationData
            {
                Discord = new DiscordSettings 
                { 
                    BotToken = "token", 
                    NotificationChannelId = "12345",
                    NotifyPlayerJoinLeave = true
                }
            }
        };

        using var service = new DiscordBotService(_mockLogger.Object, config);

        await service.Invoking(s => s.SendPlayerJoinedAsync(1, "TestServer", "TestPlayer"))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendPlayerJoinedAsync_WhenNotificationsDisabled_ShouldNotThrow()
    {
        var config = new HoNConfiguration
        {
            ApplicationData = new ApplicationData
            {
                Discord = new DiscordSettings 
                { 
                    BotToken = "token", 
                    NotificationChannelId = "12345",
                    NotifyPlayerJoinLeave = false
                }
            }
        };

        using var service = new DiscordBotService(_mockLogger.Object, config);

        await service.Invoking(s => s.SendPlayerJoinedAsync(1, "TestServer", "TestPlayer"))
            .Should().NotThrowAsync();
    }

    #endregion

    #region SendPlayerLeftAsync Tests

    [Fact]
    public async Task SendPlayerLeftAsync_WhenNotConnected_ShouldNotThrow()
    {
        var config = new HoNConfiguration
        {
            ApplicationData = new ApplicationData
            {
                Discord = new DiscordSettings 
                { 
                    BotToken = "token", 
                    NotificationChannelId = "12345",
                    NotifyPlayerJoinLeave = true
                }
            }
        };

        using var service = new DiscordBotService(_mockLogger.Object, config);

        await service.Invoking(s => s.SendPlayerLeftAsync(1, "TestServer", "TestPlayer"))
            .Should().NotThrowAsync();
    }

    #endregion

    #region SendAlertAsync Tests

    [Fact]
    public async Task SendAlertAsync_WhenNotConnected_ShouldNotThrow()
    {
        var config = new HoNConfiguration
        {
            ApplicationData = new ApplicationData
            {
                Discord = new DiscordSettings { BotToken = "token", NotificationChannelId = "12345" }
            }
        };

        using var service = new DiscordBotService(_mockLogger.Object, config);

        await service.Invoking(s => s.SendAlertAsync("Alert Title", "Alert Message"))
            .Should().NotThrowAsync();
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        var config = new HoNConfiguration
        {
            ApplicationData = new ApplicationData
            {
                Discord = new DiscordSettings { BotToken = "token" }
            }
        };

        var service = new DiscordBotService(_mockLogger.Object, config);

        service.Invoking(s => s.Dispose()).Should().NotThrow();
    }

    [Fact]
    public void Dispose_MultipleCalls_ShouldNotThrow()
    {
        var config = new HoNConfiguration
        {
            ApplicationData = new ApplicationData
            {
                Discord = new DiscordSettings { BotToken = "token" }
            }
        };

        var service = new DiscordBotService(_mockLogger.Object, config);

        service.Dispose();
        service.Invoking(s => s.Dispose()).Should().NotThrow();
    }

    #endregion
}
