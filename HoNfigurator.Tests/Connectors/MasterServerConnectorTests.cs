using FluentAssertions;
using HoNfigurator.Core.Connectors;
using Microsoft.Extensions.Logging;
using Moq;

namespace HoNfigurator.Tests.Connectors;

#region MasterServerAuthResponse Tests

public class MasterServerAuthResponseTests
{
    [Fact]
    public void MasterServerAuthResponse_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var response = new MasterServerAuthResponse();

        // Assert
        response.Success.Should().BeFalse();
        response.ServerId.Should().Be(0);
        response.SessionId.Should().BeEmpty();
        response.ChatServerHost.Should().BeEmpty();
        response.ChatServerPort.Should().Be(0);
        response.IsOfficial.Should().BeTrue();
        response.CdnUploadHost.Should().BeNull();
        response.CdnUploadTarget.Should().BeNull();
        response.Error.Should().BeEmpty();
    }

    [Fact]
    public void MasterServerAuthResponse_SuccessfulAuth_HasCorrectValues()
    {
        // Arrange & Act
        var response = new MasterServerAuthResponse
        {
            Success = true,
            ServerId = 123,
            SessionId = "abc-def-ghi",
            ChatServerHost = "chat.kongor.net",
            ChatServerPort = 11031,
            IsOfficial = true
        };

        // Assert
        response.Success.Should().BeTrue();
        response.ServerId.Should().Be(123);
        response.SessionId.Should().Be("abc-def-ghi");
        response.ChatServerHost.Should().Be("chat.kongor.net");
        response.ChatServerPort.Should().Be(11031);
        response.IsOfficial.Should().BeTrue();
    }

    [Fact]
    public void MasterServerAuthResponse_FailedAuth_HasError()
    {
        // Arrange & Act
        var response = new MasterServerAuthResponse
        {
            Success = false,
            Error = "Invalid credentials"
        };

        // Assert
        response.Success.Should().BeFalse();
        response.Error.Should().Be("Invalid credentials");
    }

    [Fact]
    public void MasterServerAuthResponse_WithCdnUpload_HasValues()
    {
        // Arrange & Act
        var response = new MasterServerAuthResponse
        {
            Success = true,
            CdnUploadHost = "kongor.net",
            CdnUploadTarget = "upload"
        };

        // Assert
        response.CdnUploadHost.Should().Be("kongor.net");
        response.CdnUploadTarget.Should().Be("upload");
    }

    [Fact]
    public void MasterServerAuthResponse_UnofficialServer_IsOfficialFalse()
    {
        // Arrange & Act
        var response = new MasterServerAuthResponse { IsOfficial = false };

        // Assert
        response.IsOfficial.Should().BeFalse();
    }

    [Fact]
    public void MasterServerAuthResponse_IsRecord_SupportsEquality()
    {
        // Arrange
        var response1 = new MasterServerAuthResponse
        {
            Success = true,
            ServerId = 123,
            SessionId = "test-session"
        };
        var response2 = new MasterServerAuthResponse
        {
            Success = true,
            ServerId = 123,
            SessionId = "test-session"
        };

        // Assert
        response1.Should().BeEquivalentTo(response2);
    }

    [Fact]
    public void MasterServerAuthResponse_WithCopying_Preserves()
    {
        // Arrange
        var original = new MasterServerAuthResponse
        {
            Success = true,
            ServerId = 100,
            SessionId = "session-id"
        };

        // Act - using with expression
        var copied = original with { ServerId = 200 };

        // Assert
        copied.ServerId.Should().Be(200);
        copied.Success.Should().BeTrue();
        copied.SessionId.Should().Be("session-id");
        original.ServerId.Should().Be(100);
    }
}

#endregion

#region GameServerAuthResponse Tests

public class GameServerAuthResponseTests
{
    [Fact]
    public void GameServerAuthResponse_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var response = new GameServerAuthResponse();

        // Assert
        response.Success.Should().BeFalse();
        response.ServerId.Should().Be(0);
        response.SessionId.Should().BeEmpty();
        response.ChatServerHost.Should().BeEmpty();
        response.ChatServerPort.Should().Be(0);
        response.LeaverThreshold.Should().Be(0.05);
        response.Error.Should().BeEmpty();
    }

    [Fact]
    public void GameServerAuthResponse_SuccessfulAuth_HasCorrectValues()
    {
        // Arrange & Act
        var response = new GameServerAuthResponse
        {
            Success = true,
            ServerId = 456,
            SessionId = "game-session-id",
            ChatServerHost = "chat.kongor.net",
            ChatServerPort = 11032,
            LeaverThreshold = 0.10
        };

        // Assert
        response.Success.Should().BeTrue();
        response.ServerId.Should().Be(456);
        response.SessionId.Should().Be("game-session-id");
        response.ChatServerHost.Should().Be("chat.kongor.net");
        response.ChatServerPort.Should().Be(11032);
        response.LeaverThreshold.Should().Be(0.10);
    }

    [Fact]
    public void GameServerAuthResponse_FailedAuth_HasError()
    {
        // Arrange & Act
        var response = new GameServerAuthResponse
        {
            Success = false,
            Error = "Account is not a Server Host"
        };

        // Assert
        response.Success.Should().BeFalse();
        response.Error.Should().Be("Account is not a Server Host");
    }

    [Fact]
    public void GameServerAuthResponse_DefaultLeaverThreshold_IsFivePercent()
    {
        // Arrange & Act
        var response = new GameServerAuthResponse();

        // Assert
        response.LeaverThreshold.Should().BeApproximately(0.05, 0.0001);
    }

    [Fact]
    public void GameServerAuthResponse_IsRecord_SupportsEquality()
    {
        // Arrange
        var response1 = new GameServerAuthResponse { Success = true, ServerId = 100 };
        var response2 = new GameServerAuthResponse { Success = true, ServerId = 100 };

        // Assert
        response1.Should().BeEquivalentTo(response2);
    }

    [Fact]
    public void GameServerAuthResponse_WithCopying_Preserves()
    {
        // Arrange
        var original = new GameServerAuthResponse
        {
            Success = true,
            ServerId = 100,
            LeaverThreshold = 0.05
        };

        // Act
        var copied = original with { LeaverThreshold = 0.15 };

        // Assert
        copied.LeaverThreshold.Should().Be(0.15);
        copied.ServerId.Should().Be(100);
        original.LeaverThreshold.Should().Be(0.05);
    }
}

#endregion

#region ReplayUploadResponse Tests

public class ReplayUploadResponseTests
{
    [Fact]
    public void ReplayUploadResponse_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var response = new ReplayUploadResponse();

        // Assert
        response.Success.Should().BeFalse();
        response.Url.Should().BeEmpty();
        response.Error.Should().BeEmpty();
    }

    [Fact]
    public void ReplayUploadResponse_SuccessfulUpload_HasUrl()
    {
        // Arrange & Act
        var response = new ReplayUploadResponse
        {
            Success = true,
            Url = "https://replays.kongor.net/match/12345.honreplay"
        };

        // Assert
        response.Success.Should().BeTrue();
        response.Url.Should().Contain("12345");
    }

    [Fact]
    public void ReplayUploadResponse_FailedUpload_HasError()
    {
        // Arrange & Act
        var response = new ReplayUploadResponse
        {
            Success = false,
            Error = "File not found"
        };

        // Assert
        response.Success.Should().BeFalse();
        response.Error.Should().Be("File not found");
    }

    [Fact]
    public void ReplayUploadResponse_IsRecord_SupportsEquality()
    {
        // Arrange
        var response1 = new ReplayUploadResponse { Success = true, Url = "url" };
        var response2 = new ReplayUploadResponse { Success = true, Url = "url" };

        // Assert
        response1.Should().BeEquivalentTo(response2);
    }

    [Fact]
    public void ReplayUploadResponse_WithCopying_Preserves()
    {
        // Arrange
        var original = new ReplayUploadResponse { Success = true, Url = "original" };

        // Act
        var copied = original with { Url = "copied" };

        // Assert
        copied.Url.Should().Be("copied");
        original.Url.Should().Be("original");
    }
}

#endregion

#region MasterServerConnector Tests

public class MasterServerConnectorTests : IDisposable
{
    private readonly Mock<ILogger<MasterServerConnector>> _loggerMock;
    private readonly MasterServerConnector _connector;

    public MasterServerConnectorTests()
    {
        _loggerMock = new Mock<ILogger<MasterServerConnector>>();
        _connector = new MasterServerConnector(_loggerMock.Object);
    }

    public void Dispose()
    {
        _connector.Dispose();
    }

    [Fact]
    public void Constructor_WithDefaultParams_CreatesInstance()
    {
        // Arrange & Act
        using var connector = new MasterServerConnector(_loggerMock.Object);

        // Assert
        connector.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithCustomUrl_CreatesInstance()
    {
        // Arrange & Act
        using var connector = new MasterServerConnector(
            _loggerMock.Object, 
            masterServerUrl: "http://test.kongor.net");

        // Assert
        connector.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithCustomVersion_CreatesInstance()
    {
        // Arrange & Act
        using var connector = new MasterServerConnector(
            _loggerMock.Object, 
            version: "4.10.2");

        // Assert
        connector.Should().NotBeNull();
    }

    [Fact]
    public void IsAuthenticated_InitialState_IsFalse()
    {
        // Arrange & Act & Assert
        _connector.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void ServerId_InitialState_IsNull()
    {
        // Arrange & Act & Assert
        _connector.ServerId.Should().BeNull();
    }

    [Fact]
    public void SessionId_InitialState_IsNull()
    {
        // Arrange & Act & Assert
        _connector.SessionId.Should().BeNull();
    }

    [Fact]
    public void ChatServerHost_InitialState_IsNull()
    {
        // Arrange & Act & Assert
        _connector.ChatServerHost.Should().BeNull();
    }

    [Fact]
    public void ChatServerPort_InitialState_IsNull()
    {
        // Arrange & Act & Assert
        _connector.ChatServerPort.Should().BeNull();
    }

    [Fact]
    public void Disconnect_ClearsAuthenticationState()
    {
        // Arrange & Act
        _connector.Disconnect();

        // Assert
        _connector.IsAuthenticated.Should().BeFalse();
        _connector.ServerId.Should().BeNull();
        _connector.SessionId.Should().BeNull();
        _connector.ChatServerHost.Should().BeNull();
        _connector.ChatServerPort.Should().BeNull();
    }

    [Fact]
    public void Disconnect_RaisesOnDisconnectedEvent()
    {
        // Arrange
        var eventRaised = false;
        _connector.OnDisconnected += () => eventRaised = true;

        // Act
        _connector.Disconnect();

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void Disconnect_MultipleDisconnects_DoesNotThrow()
    {
        // Arrange & Act
        var act = () =>
        {
            _connector.Disconnect();
            _connector.Disconnect();
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task ValidateSessionAsync_WhenNotAuthenticated_ReturnsFalse()
    {
        // Arrange & Act
        var result = await _connector.ValidateSessionAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UploadReplayAsync_WhenNotAuthenticated_ReturnsError()
    {
        // Arrange & Act
        var result = await _connector.UploadReplayAsync(12345, "nonexistent.honreplay");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Not authenticated");
    }

    [Fact]
    public void Dispose_DisposesResources()
    {
        // Arrange
        var connector = new MasterServerConnector(_loggerMock.Object);

        // Act & Assert
        var act = () => connector.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_MultipleDispose_DoesNotThrow()
    {
        // Arrange
        var connector = new MasterServerConnector(_loggerMock.Object);

        // Act & Assert
        var act = () =>
        {
            connector.Dispose();
            connector.Dispose();
        };
        act.Should().NotThrow();
    }

    [Fact]
    public void OnAuthenticated_Event_CanBeSubscribed()
    {
        // Arrange
        var eventRaised = false;
        _connector.OnAuthenticated += () => eventRaised = true;

        // Assert - just verifying subscription works
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public void OnAuthenticationFailed_Event_CanBeSubscribed()
    {
        // Arrange
        string? errorMessage = null;
        _connector.OnAuthenticationFailed += msg => errorMessage = msg;

        // Assert - just verifying subscription works
        errorMessage.Should().BeNull();
    }

    [Fact]
    public void OnDisconnected_Event_CanBeSubscribed()
    {
        // Arrange
        var eventRaised = false;
        _connector.OnDisconnected += () => eventRaised = true;

        // Assert - just verifying subscription works
        eventRaised.Should().BeFalse();
    }
}

#endregion

#region MasterServerConnector Authentication Tests

public class MasterServerConnectorAuthTests : IDisposable
{
    private readonly Mock<ILogger<MasterServerConnector>> _loggerMock;
    private readonly MasterServerConnector _connector;

    public MasterServerConnectorAuthTests()
    {
        _loggerMock = new Mock<ILogger<MasterServerConnector>>();
        // Use a URL that won't connect - just for testing error handling
        _connector = new MasterServerConnector(
            _loggerMock.Object, 
            masterServerUrl: "http://localhost:65535");
    }

    public void Dispose()
    {
        _connector.Dispose();
    }

    [Fact]
    public async Task AuthenticateAsync_WithInvalidServer_ReturnsError()
    {
        // Arrange & Act
        var result = await _connector.AuthenticateAsync("user", "pass");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task AuthenticateAsync_RaisesOnAuthenticationFailedEvent()
    {
        // Arrange
        string? capturedError = null;
        _connector.OnAuthenticationFailed += err => capturedError = err;

        // Act
        await _connector.AuthenticateAsync("user", "pass");

        // Assert
        capturedError.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task AuthenticateGameServerAsync_WithInvalidServer_ReturnsError()
    {
        // Arrange & Act
        var result = await _connector.AuthenticateGameServerAsync(
            hostAccount: "host",
            serverInstance: 1,
            password: "pass",
            port: 11031,
            serverName: "Test Server",
            description: "Test",
            location: "US",
            ipAddress: "127.0.0.1");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task AuthenticateAsync_WithCancellation_StopsProperly()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _connector.AuthenticateAsync("user", "pass", cts.Token);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task AuthenticateGameServerAsync_WithCancellation_StopsProperly()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _connector.AuthenticateGameServerAsync(
            "host", 1, "pass", 11031, "Server", "Desc", "US", "127.0.0.1", cts.Token);

        // Assert
        result.Success.Should().BeFalse();
    }
}

#endregion

#region IMasterServerConnector Interface Tests

public class IMasterServerConnectorInterfaceTests
{
    [Fact]
    public void MasterServerConnector_ImplementsInterface()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MasterServerConnector>>();

        // Act
        using var connector = new MasterServerConnector(loggerMock.Object);

        // Assert
        connector.Should().BeAssignableTo<IMasterServerConnector>();
    }

    [Fact]
    public void MasterServerConnector_ImplementsIDisposable()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MasterServerConnector>>();

        // Act
        using var connector = new MasterServerConnector(loggerMock.Object);

        // Assert
        connector.Should().BeAssignableTo<IDisposable>();
    }
}

#endregion
