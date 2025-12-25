using FluentAssertions;
using HoNfigurator.Core.Connectors;
using Microsoft.Extensions.Logging;
using Moq;

namespace HoNfigurator.Tests.Connectors;

/// <summary>
/// Tests for ChatServerConnector and related types
/// </summary>
public class ChatServerConnectorTests
{
    #region ReplayUploadStatus Enum Tests

    [Fact]
    public void ReplayUploadStatus_NotFound_ShouldHaveCorrectValue()
    {
        // Assert
        ((byte)ReplayUploadStatus.NotFound).Should().Be(0x01);
    }

    [Fact]
    public void ReplayUploadStatus_AlreadyUploaded_ShouldHaveCorrectValue()
    {
        // Assert
        ((byte)ReplayUploadStatus.AlreadyUploaded).Should().Be(0x02);
    }

    [Fact]
    public void ReplayUploadStatus_InQueue_ShouldHaveCorrectValue()
    {
        // Assert
        ((byte)ReplayUploadStatus.InQueue).Should().Be(0x03);
    }

    [Fact]
    public void ReplayUploadStatus_Uploading_ShouldHaveCorrectValue()
    {
        // Assert
        ((byte)ReplayUploadStatus.Uploading).Should().Be(0x04);
    }

    [Fact]
    public void ReplayUploadStatus_HaveReplay_ShouldHaveCorrectValue()
    {
        // Assert
        ((byte)ReplayUploadStatus.HaveReplay).Should().Be(0x05);
    }

    [Fact]
    public void ReplayUploadStatus_UploadingNow_ShouldHaveCorrectValue()
    {
        // Assert
        ((byte)ReplayUploadStatus.UploadingNow).Should().Be(0x06);
    }

    [Fact]
    public void ReplayUploadStatus_UploadComplete_ShouldHaveCorrectValue()
    {
        // Assert
        ((byte)ReplayUploadStatus.UploadComplete).Should().Be(0x07);
    }

    [Fact]
    public void ReplayUploadStatus_ShouldHaveCorrectCount()
    {
        // Arrange
        var values = Enum.GetValues<ReplayUploadStatus>();

        // Assert
        values.Should().HaveCount(7);
    }

    [Theory]
    [InlineData(ReplayUploadStatus.NotFound, "NotFound")]
    [InlineData(ReplayUploadStatus.AlreadyUploaded, "AlreadyUploaded")]
    [InlineData(ReplayUploadStatus.InQueue, "InQueue")]
    [InlineData(ReplayUploadStatus.Uploading, "Uploading")]
    [InlineData(ReplayUploadStatus.HaveReplay, "HaveReplay")]
    [InlineData(ReplayUploadStatus.UploadingNow, "UploadingNow")]
    [InlineData(ReplayUploadStatus.UploadComplete, "UploadComplete")]
    public void ReplayUploadStatus_ToStringShouldReturnExpected(ReplayUploadStatus status, string expected)
    {
        // Act & Assert
        status.ToString().Should().Be(expected);
    }

    #endregion

    #region IChatServerConnector Interface Tests

    [Fact]
    public void IChatServerConnector_ShouldDefineIsConnected()
    {
        // Arrange
        var properties = typeof(IChatServerConnector).GetProperties();

        // Assert
        properties.Should().Contain(p => p.Name == "IsConnected" && p.PropertyType == typeof(bool));
    }

    [Fact]
    public void IChatServerConnector_ShouldDefineServerId()
    {
        // Arrange
        var properties = typeof(IChatServerConnector).GetProperties();

        // Assert
        properties.Should().Contain(p => p.Name == "ServerId" && p.PropertyType == typeof(int?));
    }

    [Fact]
    public void IChatServerConnector_ShouldDefineConnectAsync()
    {
        // Arrange
        var methods = typeof(IChatServerConnector).GetMethods();

        // Assert
        methods.Should().Contain(m => m.Name == "ConnectAsync");
    }

    [Fact]
    public void IChatServerConnector_ShouldDefineDisconnectAsync()
    {
        // Arrange
        var methods = typeof(IChatServerConnector).GetMethods();

        // Assert
        methods.Should().Contain(m => m.Name == "DisconnectAsync");
    }

    [Fact]
    public void IChatServerConnector_ShouldDefineSendHandshakeAsync()
    {
        // Arrange
        var methods = typeof(IChatServerConnector).GetMethods();

        // Assert
        methods.Should().Contain(m => m.Name == "SendHandshakeAsync");
    }

    [Fact]
    public void IChatServerConnector_ShouldDefineSendServerInfoAsync()
    {
        // Arrange
        var methods = typeof(IChatServerConnector).GetMethods();

        // Assert
        methods.Should().Contain(m => m.Name == "SendServerInfoAsync");
    }

    [Fact]
    public void IChatServerConnector_ShouldDefineSendHeartbeatAsync()
    {
        // Arrange
        var methods = typeof(IChatServerConnector).GetMethods();

        // Assert
        methods.Should().Contain(m => m.Name == "SendHeartbeatAsync");
    }

    [Fact]
    public void IChatServerConnector_ShouldDefineSendReplayStatusUpdateAsync()
    {
        // Arrange
        var methods = typeof(IChatServerConnector).GetMethods();

        // Assert
        methods.Should().Contain(m => m.Name == "SendReplayStatusUpdateAsync");
    }

    #endregion

    #region ChatServerConnector Constructor Tests

    [Fact]
    public void Constructor_WithValidLogger_ShouldNotThrow()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ChatServerConnector>>();

        // Act
        var connector = new ChatServerConnector(mockLogger.Object);

        // Assert
        connector.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldInitializeIsConnectedToFalse()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ChatServerConnector>>();

        // Act
        var connector = new ChatServerConnector(mockLogger.Object);

        // Assert
        connector.IsConnected.Should().BeFalse();
    }

    [Fact]
    public void Constructor_ShouldInitializeServerIdToNull()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ChatServerConnector>>();

        // Act
        var connector = new ChatServerConnector(mockLogger.Object);

        // Assert
        connector.ServerId.Should().BeNull();
    }

    #endregion

    #region ChatServerConnector Interface Implementation Tests

    [Fact]
    public void ChatServerConnector_ShouldImplementIChatServerConnector()
    {
        // Assert
        typeof(ChatServerConnector).Should().Implement<IChatServerConnector>();
    }

    [Fact]
    public void ChatServerConnector_ShouldImplementIDisposable()
    {
        // Assert
        typeof(ChatServerConnector).Should().Implement<IDisposable>();
    }

    #endregion

    #region ChatServerConnector Event Tests

    [Fact]
    public void ChatServerConnector_ShouldHaveOnConnectedEvent()
    {
        // Arrange
        var eventInfo = typeof(ChatServerConnector).GetEvent("OnConnected");

        // Assert
        eventInfo.Should().NotBeNull();
    }

    [Fact]
    public void ChatServerConnector_ShouldHaveOnDisconnectedEvent()
    {
        // Arrange
        var eventInfo = typeof(ChatServerConnector).GetEvent("OnDisconnected");

        // Assert
        eventInfo.Should().NotBeNull();
    }

    [Fact]
    public void ChatServerConnector_ShouldHaveOnHandshakeAcceptedEvent()
    {
        // Arrange
        var eventInfo = typeof(ChatServerConnector).GetEvent("OnHandshakeAccepted");

        // Assert
        eventInfo.Should().NotBeNull();
    }

    [Fact]
    public void ChatServerConnector_ShouldHaveOnReplayRequestEvent()
    {
        // Arrange
        var eventInfo = typeof(ChatServerConnector).GetEvent("OnReplayRequest");

        // Assert
        eventInfo.Should().NotBeNull();
    }

    [Fact]
    public void ChatServerConnector_ShouldHaveOnShutdownNoticeEvent()
    {
        // Arrange
        var eventInfo = typeof(ChatServerConnector).GetEvent("OnShutdownNotice");

        // Assert
        eventInfo.Should().NotBeNull();
    }

    #endregion

    #region ChatServerConnector Dispose Tests

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ChatServerConnector>>();
        var connector = new ChatServerConnector(mockLogger.Object);

        // Act & Assert
        connector.Invoking(c => c.Dispose()).Should().NotThrow();
    }

    [Fact]
    public void Dispose_WhenNotConnected_ShouldNotThrow()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ChatServerConnector>>();
        var connector = new ChatServerConnector(mockLogger.Object);

        // Act & Assert
        connector.Invoking(c => c.Dispose()).Should().NotThrow();
    }

    [Fact]
    public void Dispose_MultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ChatServerConnector>>();
        var connector = new ChatServerConnector(mockLogger.Object);

        // Act & Assert
        connector.Dispose();
        connector.Invoking(c => c.Dispose()).Should().NotThrow();
    }

    #endregion

    #region ChatServerConnector Method Signature Tests

    [Fact]
    public void ConnectAsync_ShouldReturnTaskOfBool()
    {
        // Arrange
        var method = typeof(ChatServerConnector).GetMethod("ConnectAsync");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<bool>));
    }

    [Fact]
    public void DisconnectAsync_ShouldReturnTask()
    {
        // Arrange
        var method = typeof(ChatServerConnector).GetMethod("DisconnectAsync");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task));
    }

    [Fact]
    public void SendHandshakeAsync_ShouldReturnTaskOfBool()
    {
        // Arrange
        var method = typeof(ChatServerConnector).GetMethod("SendHandshakeAsync");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<bool>));
    }

    [Fact]
    public void SendServerInfoAsync_ShouldReturnTask()
    {
        // Arrange
        var method = typeof(ChatServerConnector).GetMethod("SendServerInfoAsync");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task));
    }

    [Fact]
    public void SendHeartbeatAsync_ShouldReturnTask()
    {
        // Arrange
        var method = typeof(ChatServerConnector).GetMethod("SendHeartbeatAsync");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task));
    }

    [Fact]
    public void SendReplayStatusUpdateAsync_ShouldReturnTask()
    {
        // Arrange
        var method = typeof(ChatServerConnector).GetMethod("SendReplayStatusUpdateAsync");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task));
    }

    #endregion
}
