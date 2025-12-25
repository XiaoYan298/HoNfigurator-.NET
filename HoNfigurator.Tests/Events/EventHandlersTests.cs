using FluentAssertions;
using HoNfigurator.Core.Events;
using Microsoft.Extensions.Logging;
using Moq;

namespace HoNfigurator.Tests.Events;

public class EventHandlersTests
{
    [Fact]
    public void LoggingEventHandler_CanHandle_ShouldReturnTrueForAllEventTypes()
    {
        var logger = Mock.Of<ILogger<LoggingEventHandler>>();
        var handler = new LoggingEventHandler(logger);

        foreach (var eventType in Enum.GetValues<GameEventType>())
        {
            handler.CanHandle(eventType).Should().BeTrue();
        }
    }

    [Fact]
    public async Task LoggingEventHandler_HandleAsync_ShouldLogEvent()
    {
        var mockLogger = new Mock<ILogger<LoggingEventHandler>>();
        var handler = new LoggingEventHandler(mockLogger.Object);
        var gameEvent = new GameEvent
        {
            EventType = GameEventType.PlayerConnected,
            ServerId = 1,
            Data = new Dictionary<string, object>
            {
                ["playerId"] = "12345",
                ["playerName"] = "TestPlayer"
            }
        };

        await handler.HandleAsync(gameEvent);

        mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LoggingEventHandler_HandleAsync_ServerCrashed_ShouldLogError()
    {
        var mockLogger = new Mock<ILogger<LoggingEventHandler>>();
        var handler = new LoggingEventHandler(mockLogger.Object);
        var gameEvent = new GameEvent { EventType = GameEventType.ServerCrashed, ServerId = 1 };

        await handler.HandleAsync(gameEvent);

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(GameEventType.HealthCheckFailed)]
    [InlineData(GameEventType.ResourceWarning)]
    [InlineData(GameEventType.PlayerBanned)]
    [InlineData(GameEventType.PlayerKicked)]
    public async Task LoggingEventHandler_HandleAsync_WarningEvents_ShouldLogWarning(GameEventType eventType)
    {
        var mockLogger = new Mock<ILogger<LoggingEventHandler>>();
        var handler = new LoggingEventHandler(mockLogger.Object);
        var gameEvent = new GameEvent { EventType = eventType, ServerId = 1 };

        await handler.HandleAsync(gameEvent);

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(GameEventType.PlayerConnected)]
    [InlineData(GameEventType.PlayerDisconnected)]
    [InlineData(GameEventType.MatchStarted)]
    [InlineData(GameEventType.MatchEnded)]
    [InlineData(GameEventType.ServerStarted)]
    [InlineData(GameEventType.ServerStopped)]
    public async Task LoggingEventHandler_HandleAsync_InfoEvents_ShouldLogInformation(GameEventType eventType)
    {
        var mockLogger = new Mock<ILogger<LoggingEventHandler>>();
        var handler = new LoggingEventHandler(mockLogger.Object);
        var gameEvent = new GameEvent { EventType = eventType, ServerId = 1 };

        await handler.HandleAsync(gameEvent);

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void MatchStatsHandler_CanHandle_MatchStarted_ShouldReturnTrue()
    {
        var logger = Mock.Of<ILogger<MatchStatsHandler>>();
        var handler = new MatchStatsHandler(logger);

        handler.CanHandle(GameEventType.MatchStarted).Should().BeTrue();
    }

    [Fact]
    public void MatchStatsHandler_CanHandle_MatchEnded_ShouldReturnTrue()
    {
        var logger = Mock.Of<ILogger<MatchStatsHandler>>();
        var handler = new MatchStatsHandler(logger);

        handler.CanHandle(GameEventType.MatchEnded).Should().BeTrue();
    }

    [Fact]
    public void MatchStatsHandler_CanHandle_MatchAborted_ShouldReturnTrue()
    {
        var logger = Mock.Of<ILogger<MatchStatsHandler>>();
        var handler = new MatchStatsHandler(logger);

        handler.CanHandle(GameEventType.MatchAborted).Should().BeTrue();
    }

    [Theory]
    [InlineData(GameEventType.PlayerConnected)]
    [InlineData(GameEventType.PlayerDisconnected)]
    [InlineData(GameEventType.ServerStarted)]
    [InlineData(GameEventType.ServerStopped)]
    [InlineData(GameEventType.ServerCrashed)]
    public void MatchStatsHandler_CanHandle_OtherEvents_ShouldReturnFalse(GameEventType eventType)
    {
        var logger = Mock.Of<ILogger<MatchStatsHandler>>();
        var handler = new MatchStatsHandler(logger);

        handler.CanHandle(eventType).Should().BeFalse();
    }

    [Fact]
    public async Task MatchStatsHandler_HandleAsync_MatchStarted_ShouldAddMatchStats()
    {
        var logger = Mock.Of<ILogger<MatchStatsHandler>>();
        var handler = new MatchStatsHandler(logger);
        var gameEvent = new GameEvent
        {
            EventType = GameEventType.MatchStarted,
            ServerId = 1,
            Data = new Dictionary<string, object>
            {
                ["matchId"] = 12345L,
                ["gameMode"] = "Normal"
            }
        };

        await handler.HandleAsync(gameEvent);
        var matches = handler.GetRecentMatches();

        matches.Should().HaveCount(1);
        matches[0].MatchId.Should().Be(12345);
        matches[0].ServerId.Should().Be(1);
        matches[0].GameMode.Should().Be("Normal");
        matches[0].WasAborted.Should().BeFalse();
    }

    [Fact]
    public async Task MatchStatsHandler_HandleAsync_MatchStarted_WithoutGameMode_ShouldUseUnknown()
    {
        var logger = Mock.Of<ILogger<MatchStatsHandler>>();
        var handler = new MatchStatsHandler(logger);
        var gameEvent = new GameEvent
        {
            EventType = GameEventType.MatchStarted,
            ServerId = 1,
            Data = new Dictionary<string, object> { ["matchId"] = 12345L }
        };

        await handler.HandleAsync(gameEvent);
        var matches = handler.GetRecentMatches();

        matches[0].GameMode.Should().Be("Unknown");
    }

    [Fact]
    public async Task MatchStatsHandler_HandleAsync_MatchEnded_ShouldUpdateMatchStats()
    {
        var logger = Mock.Of<ILogger<MatchStatsHandler>>();
        var handler = new MatchStatsHandler(logger);

        var startEvent = new GameEvent
        {
            EventType = GameEventType.MatchStarted,
            ServerId = 1,
            Data = new Dictionary<string, object>
            {
                ["matchId"] = 12345L,
                ["gameMode"] = "Normal"
            }
        };
        await handler.HandleAsync(startEvent);
        await Task.Delay(10);

        var endEvent = new GameEvent
        {
            EventType = GameEventType.MatchEnded,
            ServerId = 1,
            Data = new Dictionary<string, object>
            {
                ["matchId"] = 12345L,
                ["winner"] = "Legion"
            }
        };

        await handler.HandleAsync(endEvent);
        var matches = handler.GetRecentMatches();

        matches[0].EndTime.Should().NotBeNull();
        matches[0].Duration.Should().NotBeNull();
        matches[0].Winner.Should().Be("Legion");
    }

    [Fact]
    public async Task MatchStatsHandler_HandleAsync_MatchAborted_ShouldMarkAsAborted()
    {
        var logger = Mock.Of<ILogger<MatchStatsHandler>>();
        var handler = new MatchStatsHandler(logger);

        var startEvent = new GameEvent
        {
            EventType = GameEventType.MatchStarted,
            ServerId = 1,
            Data = new Dictionary<string, object> { ["matchId"] = 12345L }
        };
        await handler.HandleAsync(startEvent);

        var abortEvent = new GameEvent
        {
            EventType = GameEventType.MatchAborted,
            ServerId = 1,
            Data = new Dictionary<string, object> { ["matchId"] = 12345L }
        };

        await handler.HandleAsync(abortEvent);
        var matches = handler.GetRecentMatches();

        matches[0].WasAborted.Should().BeTrue();
        matches[0].EndTime.Should().NotBeNull();
    }

    [Fact]
    public async Task MatchStatsHandler_HandleAsync_UnknownMatchEnd_ShouldNotThrow()
    {
        var logger = Mock.Of<ILogger<MatchStatsHandler>>();
        var handler = new MatchStatsHandler(logger);

        var endEvent = new GameEvent
        {
            EventType = GameEventType.MatchEnded,
            ServerId = 1,
            Data = new Dictionary<string, object> { ["matchId"] = 99999L }
        };

        var act = () => handler.HandleAsync(endEvent);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task MatchStatsHandler_HandleAsync_MultipleMatches_ShouldTrackAll()
    {
        var logger = Mock.Of<ILogger<MatchStatsHandler>>();
        var handler = new MatchStatsHandler(logger);

        for (int i = 1; i <= 5; i++)
        {
            var startEvent = new GameEvent
            {
                EventType = GameEventType.MatchStarted,
                ServerId = i,
                Data = new Dictionary<string, object>
                {
                    ["matchId"] = (long)(1000 + i),
                    ["gameMode"] = "Normal"
                }
            };
            await handler.HandleAsync(startEvent);
        }

        var matches = handler.GetRecentMatches();

        matches.Should().HaveCount(5);
    }

    [Fact]
    public async Task MatchStatsHandler_HandleAsync_MoreThan100Matches_ShouldTrimOldest()
    {
        var logger = Mock.Of<ILogger<MatchStatsHandler>>();
        var handler = new MatchStatsHandler(logger);

        for (int i = 1; i <= 105; i++)
        {
            var startEvent = new GameEvent
            {
                EventType = GameEventType.MatchStarted,
                ServerId = 1,
                Data = new Dictionary<string, object> { ["matchId"] = (long)i }
            };
            await handler.HandleAsync(startEvent);
        }

        var matches = handler.GetRecentMatches();

        matches.Should().HaveCount(100);
        matches[0].MatchId.Should().Be(6);
        matches[99].MatchId.Should().Be(105);
    }

    [Fact]
    public async Task MatchStatsHandler_GetRecentMatches_ShouldReturnCopy()
    {
        var logger = Mock.Of<ILogger<MatchStatsHandler>>();
        var handler = new MatchStatsHandler(logger);

        var startEvent = new GameEvent
        {
            EventType = GameEventType.MatchStarted,
            ServerId = 1,
            Data = new Dictionary<string, object> { ["matchId"] = 12345L }
        };
        await handler.HandleAsync(startEvent);

        var matches1 = handler.GetRecentMatches();
        var matches2 = handler.GetRecentMatches();

        matches1.Should().NotBeSameAs(matches2);
        matches1.Should().BeEquivalentTo(matches2);
    }

    [Fact]
    public void MatchStats_DefaultValues_ShouldBeInitialized()
    {
        var stats = new MatchStats();

        stats.MatchId.Should().Be(0);
        stats.ServerId.Should().Be(0);
        stats.GameMode.Should().BeEmpty();
        stats.Winner.Should().BeNull();
        stats.WasAborted.Should().BeFalse();
    }

    [Fact]
    public void MatchStats_WithValues_ShouldStoreCorrectly()
    {
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddMinutes(30);

        var stats = new MatchStats
        {
            MatchId = 12345,
            ServerId = 1,
            StartTime = startTime,
            EndTime = endTime,
            Duration = endTime - startTime,
            GameMode = "Normal",
            Winner = "Legion",
            WasAborted = false
        };

        stats.MatchId.Should().Be(12345);
        stats.Duration.Should().Be(TimeSpan.FromMinutes(30));
        stats.Winner.Should().Be("Legion");
    }
}
