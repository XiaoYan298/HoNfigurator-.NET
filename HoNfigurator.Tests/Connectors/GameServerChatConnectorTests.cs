using FluentAssertions;
using HoNfigurator.Core.Connectors;
using HoNfigurator.Core.Protocol;
using Microsoft.Extensions.Logging;
using Moq;

namespace HoNfigurator.Tests.Connectors;

#region ArrangedMatchType Enum Tests

public class ArrangedMatchTypeTests
{
    [Fact]
    public void ArrangedMatchType_HasPublicValue()
    {
        // Assert
        ArrangedMatchType.Public.Should().Be(ArrangedMatchType.Public);
        ((byte)ArrangedMatchType.Public).Should().Be(0);
    }

    [Fact]
    public void ArrangedMatchType_HasMatchmakingValue()
    {
        // Assert
        ArrangedMatchType.Matchmaking.Should().Be(ArrangedMatchType.Matchmaking);
        ((byte)ArrangedMatchType.Matchmaking).Should().Be(1);
    }

    [Fact]
    public void ArrangedMatchType_HasTournamentValue()
    {
        // Assert
        ArrangedMatchType.Tournament.Should().Be(ArrangedMatchType.Tournament);
        ((byte)ArrangedMatchType.Tournament).Should().Be(2);
    }

    [Fact]
    public void ArrangedMatchType_HasBotMatchValue()
    {
        // Assert
        ArrangedMatchType.BotMatch.Should().Be(ArrangedMatchType.BotMatch);
        ((byte)ArrangedMatchType.BotMatch).Should().Be(3);
    }

    [Fact]
    public void ArrangedMatchType_HasCustomValue()
    {
        // Assert
        ArrangedMatchType.Custom.Should().Be(ArrangedMatchType.Custom);
        ((byte)ArrangedMatchType.Custom).Should().Be(4);
    }

    [Fact]
    public void ArrangedMatchType_HasExpectedValues()
    {
        // Assert
        var values = Enum.GetValues<ArrangedMatchType>();
        values.Should().HaveCount(5);
    }
}

#endregion

#region ArrangedMatchPlayer Tests

public class ArrangedMatchPlayerTests
{
    [Fact]
    public void ArrangedMatchPlayer_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var player = new ArrangedMatchPlayer();

        // Assert
        player.AccountId.Should().Be(0);
        player.Name.Should().BeEmpty();
        player.Slot.Should().Be(0);
        player.IsReady.Should().BeFalse();
    }

    [Fact]
    public void ArrangedMatchPlayer_CanSetAccountId()
    {
        // Arrange & Act
        var player = new ArrangedMatchPlayer { AccountId = 12345 };

        // Assert
        player.AccountId.Should().Be(12345);
    }

    [Fact]
    public void ArrangedMatchPlayer_CanSetName()
    {
        // Arrange & Act
        var player = new ArrangedMatchPlayer { Name = "TestPlayer" };

        // Assert
        player.Name.Should().Be("TestPlayer");
    }

    [Fact]
    public void ArrangedMatchPlayer_CanSetSlot()
    {
        // Arrange & Act
        var player = new ArrangedMatchPlayer { Slot = 3 };

        // Assert
        player.Slot.Should().Be(3);
    }

    [Fact]
    public void ArrangedMatchPlayer_CanSetIsReady()
    {
        // Arrange & Act
        var player = new ArrangedMatchPlayer { IsReady = true };

        // Assert
        player.IsReady.Should().BeTrue();
    }

    [Fact]
    public void ArrangedMatchPlayer_IsRecord_SupportsEquality()
    {
        // Arrange
        var player1 = new ArrangedMatchPlayer { AccountId = 100, Name = "Player", Slot = 1 };
        var player2 = new ArrangedMatchPlayer { AccountId = 100, Name = "Player", Slot = 1 };

        // Assert
        player1.Should().BeEquivalentTo(player2);
    }

    [Fact]
    public void ArrangedMatchPlayer_WithCopying_Preserves()
    {
        // Arrange
        var original = new ArrangedMatchPlayer
        {
            AccountId = 100,
            Name = "Original",
            Slot = 1,
            IsReady = false
        };

        // Act
        var copied = original with { IsReady = true };

        // Assert
        copied.IsReady.Should().BeTrue();
        copied.AccountId.Should().Be(100);
        original.IsReady.Should().BeFalse();
    }

    [Fact]
    public void ArrangedMatchPlayer_CompletePlayer()
    {
        // Arrange & Act
        var player = new ArrangedMatchPlayer
        {
            AccountId = 54321,
            Name = "ProPlayer",
            Slot = 5,
            IsReady = true
        };

        // Assert
        player.AccountId.Should().Be(54321);
        player.Name.Should().Be("ProPlayer");
        player.Slot.Should().Be(5);
        player.IsReady.Should().BeTrue();
    }
}

#endregion

#region ArrangedMatchData Tests

public class ArrangedMatchDataTests
{
    [Fact]
    public void ArrangedMatchData_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var data = new ArrangedMatchData();

        // Assert
        data.MatchId.Should().Be(0);
        data.Map.Should().Be("caldavar");
        data.GameMode.Should().Be("normal");
        data.MatchType.Should().Be(ArrangedMatchType.Matchmaking);
        data.Team1.Should().BeEmpty();
        data.Team2.Should().BeEmpty();
        data.Options.Should().BeEmpty();
    }

    [Fact]
    public void ArrangedMatchData_CanSetMatchId()
    {
        // Arrange & Act
        var data = new ArrangedMatchData { MatchId = 999 };

        // Assert
        data.MatchId.Should().Be(999);
    }

    [Fact]
    public void ArrangedMatchData_CanSetMap()
    {
        // Arrange & Act
        var data = new ArrangedMatchData { Map = "darkwood" };

        // Assert
        data.Map.Should().Be("darkwood");
    }

    [Fact]
    public void ArrangedMatchData_CanSetGameMode()
    {
        // Arrange & Act
        var data = new ArrangedMatchData { GameMode = "single_draft" };

        // Assert
        data.GameMode.Should().Be("single_draft");
    }

    [Fact]
    public void ArrangedMatchData_CanSetMatchType()
    {
        // Arrange & Act
        var data = new ArrangedMatchData { MatchType = ArrangedMatchType.Tournament };

        // Assert
        data.MatchType.Should().Be(ArrangedMatchType.Tournament);
    }

    [Fact]
    public void ArrangedMatchData_CanSetTeams()
    {
        // Arrange
        var team1 = new List<ArrangedMatchPlayer>
        {
            new() { AccountId = 1, Name = "Player1", Slot = 0 },
            new() { AccountId = 2, Name = "Player2", Slot = 1 }
        };
        var team2 = new List<ArrangedMatchPlayer>
        {
            new() { AccountId = 3, Name = "Player3", Slot = 0 },
            new() { AccountId = 4, Name = "Player4", Slot = 1 }
        };

        // Act
        var data = new ArrangedMatchData { Team1 = team1, Team2 = team2 };

        // Assert
        data.Team1.Should().HaveCount(2);
        data.Team2.Should().HaveCount(2);
    }

    [Fact]
    public void ArrangedMatchData_CanSetOptions()
    {
        // Arrange
        var options = new Dictionary<string, string>
        {
            { "all_heroes", "true" },
            { "no_stats", "false" }
        };

        // Act
        var data = new ArrangedMatchData { Options = options };

        // Assert
        data.Options.Should().HaveCount(2);
        data.Options["all_heroes"].Should().Be("true");
    }

    [Fact]
    public void ArrangedMatchData_IsRecord_SupportsEquality()
    {
        // Arrange
        var data1 = new ArrangedMatchData { MatchId = 1, Map = "caldavar" };
        var data2 = new ArrangedMatchData { MatchId = 1, Map = "caldavar" };

        // Assert
        data1.MatchId.Should().Be(data2.MatchId);
        data1.Map.Should().Be(data2.Map);
    }

    [Fact]
    public void ArrangedMatchData_CompleteMatch()
    {
        // Arrange & Act
        var data = new ArrangedMatchData
        {
            MatchId = 12345,
            Map = "caldavar",
            GameMode = "normal",
            MatchType = ArrangedMatchType.Matchmaking,
            Team1 = new List<ArrangedMatchPlayer>
            {
                new() { AccountId = 1, Name = "P1", Slot = 0, IsReady = true },
                new() { AccountId = 2, Name = "P2", Slot = 1, IsReady = true }
            },
            Team2 = new List<ArrangedMatchPlayer>
            {
                new() { AccountId = 3, Name = "P3", Slot = 0, IsReady = true },
                new() { AccountId = 4, Name = "P4", Slot = 1, IsReady = true }
            },
            Options = new Dictionary<string, string> { { "mode", "ap" } }
        };

        // Assert
        data.MatchId.Should().Be(12345);
        data.Team1.Should().HaveCount(2);
        data.Team2.Should().HaveCount(2);
        data.Team1.All(p => p.IsReady).Should().BeTrue();
        data.Options.Should().ContainKey("mode");
    }
}

#endregion

#region ClientAuthResult Tests

public class ClientAuthResultTests
{
    [Fact]
    public void ClientAuthResult_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var result = new ClientAuthResult();

        // Assert
        result.AccountId.Should().Be(0);
        result.AccountName.Should().BeEmpty();
        result.Success.Should().BeFalse();
        result.Error.Should().BeEmpty();
    }

    [Fact]
    public void ClientAuthResult_SuccessfulAuth()
    {
        // Arrange & Act
        var result = new ClientAuthResult
        {
            AccountId = 12345,
            AccountName = "TestPlayer",
            Success = true
        };

        // Assert
        result.AccountId.Should().Be(12345);
        result.AccountName.Should().Be("TestPlayer");
        result.Success.Should().BeTrue();
        result.Error.Should().BeEmpty();
    }

    [Fact]
    public void ClientAuthResult_FailedAuth()
    {
        // Arrange & Act
        var result = new ClientAuthResult
        {
            Success = false,
            Error = "Invalid session"
        };

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Invalid session");
    }

    [Fact]
    public void ClientAuthResult_IsRecord_SupportsEquality()
    {
        // Arrange
        var result1 = new ClientAuthResult { AccountId = 100, Success = true };
        var result2 = new ClientAuthResult { AccountId = 100, Success = true };

        // Assert
        result1.Should().BeEquivalentTo(result2);
    }

    [Fact]
    public void ClientAuthResult_WithCopying_Preserves()
    {
        // Arrange
        var original = new ClientAuthResult
        {
            AccountId = 100,
            AccountName = "Original",
            Success = true
        };

        // Act
        var copied = original with { AccountName = "Copied" };

        // Assert
        copied.AccountName.Should().Be("Copied");
        copied.AccountId.Should().Be(100);
        original.AccountName.Should().Be("Original");
    }
}

#endregion

#region NexusServerStatus Enum Tests

public class NexusServerStatusTests
{
    [Fact]
    public void NexusServerStatus_HasSleepingValue()
    {
        // Assert
        NexusServerStatus.Sleeping.Should().Be(NexusServerStatus.Sleeping);
        ((byte)NexusServerStatus.Sleeping).Should().Be(0);
    }

    [Fact]
    public void NexusServerStatus_HasIdleValue()
    {
        // Assert
        NexusServerStatus.Idle.Should().Be(NexusServerStatus.Idle);
        ((byte)NexusServerStatus.Idle).Should().Be(1);
    }

    [Fact]
    public void NexusServerStatus_HasLoadingValue()
    {
        // Assert
        NexusServerStatus.Loading.Should().Be(NexusServerStatus.Loading);
        ((byte)NexusServerStatus.Loading).Should().Be(2);
    }

    [Fact]
    public void NexusServerStatus_HasActiveValue()
    {
        // Assert
        NexusServerStatus.Active.Should().Be(NexusServerStatus.Active);
        ((byte)NexusServerStatus.Active).Should().Be(3);
    }

    [Fact]
    public void NexusServerStatus_HasCrashedValue()
    {
        // Assert
        NexusServerStatus.Crashed.Should().Be(NexusServerStatus.Crashed);
        ((byte)NexusServerStatus.Crashed).Should().Be(4);
    }

    [Fact]
    public void NexusServerStatus_HasKilledValue()
    {
        // Assert
        NexusServerStatus.Killed.Should().Be(NexusServerStatus.Killed);
        ((byte)NexusServerStatus.Killed).Should().Be(5);
    }

    [Fact]
    public void NexusServerStatus_HasExpectedValues()
    {
        // Assert
        var values = Enum.GetValues<NexusServerStatus>();
        values.Should().HaveCount(6);
    }
}

#endregion

#region GameServerChatConnector Tests

public class GameServerChatConnectorTests : IDisposable
{
    private readonly Mock<ILogger<GameServerChatConnector>> _loggerMock;
    private readonly GameServerChatConnector _connector;

    public GameServerChatConnectorTests()
    {
        _loggerMock = new Mock<ILogger<GameServerChatConnector>>();
        _connector = new GameServerChatConnector(_loggerMock.Object);
    }

    public void Dispose()
    {
        _connector.Dispose();
    }

    [Fact]
    public void Constructor_WithValidLogger_CreatesInstance()
    {
        // Arrange & Act
        using var connector = new GameServerChatConnector(_loggerMock.Object);

        // Assert
        connector.Should().NotBeNull();
    }

    [Fact]
    public void IsConnected_InitialState_IsFalse()
    {
        // Assert
        _connector.IsConnected.Should().BeFalse();
    }

    [Fact]
    public void ServerId_InitialState_IsNull()
    {
        // Assert
        _connector.ServerId.Should().BeNull();
    }

    [Fact]
    public void OnConnected_Event_CanBeSubscribed()
    {
        // Arrange
        var eventRaised = false;
        _connector.OnConnected += () => eventRaised = true;

        // Assert
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public void OnDisconnected_Event_CanBeSubscribed()
    {
        // Arrange
        var eventRaised = false;
        _connector.OnDisconnected += () => eventRaised = true;

        // Assert
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public void OnLoginAccepted_Event_CanBeSubscribed()
    {
        // Arrange
        var eventRaised = false;
        _connector.OnLoginAccepted += () => eventRaised = true;

        // Assert
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public void OnLoginRejected_Event_CanBeSubscribed()
    {
        // Arrange
        string? reason = null;
        _connector.OnLoginRejected += r => reason = r;

        // Assert
        reason.Should().BeNull();
    }

    [Fact]
    public void OnCreateMatchRequest_Event_CanBeSubscribed()
    {
        // Arrange
        ArrangedMatchData? matchData = null;
        _connector.OnCreateMatchRequest += data => matchData = data;

        // Assert
        matchData.Should().BeNull();
    }

    [Fact]
    public void OnEndMatchRequest_Event_CanBeSubscribed()
    {
        // Arrange
        int? matchId = null;
        _connector.OnEndMatchRequest += id => matchId = id;

        // Assert
        matchId.Should().BeNull();
    }

    [Fact]
    public void OnRemoteCommand_Event_CanBeSubscribed()
    {
        // Arrange
        string? command = null;
        _connector.OnRemoteCommand += cmd => command = cmd;

        // Assert
        command.Should().BeNull();
    }

    [Fact]
    public void OnOptionsReceived_Event_CanBeSubscribed()
    {
        // Arrange
        Dictionary<string, string>? options = null;
        _connector.OnOptionsReceived += opts => options = opts;

        // Assert
        options.Should().BeNull();
    }

    [Fact]
    public void OnHeartbeatReceived_Event_CanBeSubscribed()
    {
        // Arrange
        var eventRaised = false;
        _connector.OnHeartbeatReceived += () => eventRaised = true;

        // Assert
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public async Task ConnectAsync_ToInvalidHost_ReturnsFalse()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act
        var result = await _connector.ConnectAsync("invalid.host.test", 99999, cts.Token);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DisconnectAsync_WhenNotConnected_DoesNotThrow()
    {
        // Arrange & Act
        var act = async () => await _connector.DisconnectAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void Dispose_DisposesResources()
    {
        // Arrange
        var connector = new GameServerChatConnector(_loggerMock.Object);

        // Act & Assert
        var act = () => connector.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_MultipleDispose_DoesNotThrow()
    {
        // Arrange
        var connector = new GameServerChatConnector(_loggerMock.Object);

        // Act & Assert
        var act = () =>
        {
            connector.Dispose();
            connector.Dispose();
        };
        act.Should().NotThrow();
    }
}

#endregion

#region IGameServerChatConnector Interface Tests

public class IGameServerChatConnectorInterfaceTests
{
    [Fact]
    public void GameServerChatConnector_ImplementsInterface()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<GameServerChatConnector>>();

        // Act
        using var connector = new GameServerChatConnector(loggerMock.Object);

        // Assert
        connector.Should().BeAssignableTo<IGameServerChatConnector>();
    }

    [Fact]
    public void GameServerChatConnector_ImplementsIDisposable()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<GameServerChatConnector>>();

        // Act
        using var connector = new GameServerChatConnector(loggerMock.Object);

        // Assert
        connector.Should().BeAssignableTo<IDisposable>();
    }
}

#endregion
