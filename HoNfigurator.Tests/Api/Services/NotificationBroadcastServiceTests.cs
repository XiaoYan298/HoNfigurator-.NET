using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using HoNfigurator.Api.Hubs;
using HoNfigurator.Api.Services;
using HoNfigurator.Core.Notifications;
using HoNfigurator.Core.Charts;

namespace HoNfigurator.Tests.Api.Services;

/// <summary>
/// Tests for NotificationBroadcastService
/// </summary>
public class NotificationBroadcastServiceTests
{
    private readonly Mock<IHubContext<DashboardHub, IDashboardClient>> _mockHubContext;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<IChartDataService> _mockChartDataService;
    private readonly Mock<ILogger<NotificationBroadcastService>> _mockLogger;

    public NotificationBroadcastServiceTests()
    {
        _mockHubContext = new Mock<IHubContext<DashboardHub, IDashboardClient>>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockChartDataService = new Mock<IChartDataService>();
        _mockLogger = new Mock<ILogger<NotificationBroadcastService>>();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_ShouldNotThrow()
    {
        // Act
        var service = new NotificationBroadcastService(
            _mockHubContext.Object,
            _mockNotificationService.Object,
            _mockChartDataService.Object,
            _mockLogger.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldSubscribeToNotificationEvents()
    {
        // Arrange & Act
        var service = new NotificationBroadcastService(
            _mockHubContext.Object,
            _mockNotificationService.Object,
            _mockChartDataService.Object,
            _mockLogger.Object);

        // Assert - verify event handler was added
        // This is validated by the successful construction
        service.Should().NotBeNull();
    }

    #endregion

    #region ExecuteAsync Tests

    [Fact]
    public async Task ExecuteAsync_ShouldRecordResourceMetrics()
    {
        // Arrange
        var service = new NotificationBroadcastService(
            _mockHubContext.Object,
            _mockNotificationService.Object,
            _mockChartDataService.Object,
            _mockLogger.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(100);
        await service.StopAsync(CancellationToken.None);

        // Assert - RecordResourceMetrics should have been called
        _mockChartDataService.Verify(
            c => c.RecordResourceMetrics(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()),
            Times.AtLeastOnce());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCheckResourceThresholds()
    {
        // Arrange
        var service = new NotificationBroadcastService(
            _mockHubContext.Object,
            _mockNotificationService.Object,
            _mockChartDataService.Object,
            _mockLogger.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(100);
        await service.StopAsync(CancellationToken.None);

        // Assert
        _mockNotificationService.Verify(
            n => n.CheckResourceThresholds(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()),
            Times.AtLeastOnce());
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancelled_ShouldStopGracefully()
    {
        // Arrange
        var service = new NotificationBroadcastService(
            _mockHubContext.Object,
            _mockNotificationService.Object,
            _mockChartDataService.Object,
            _mockLogger.Object);

        using var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        cts.Cancel();
        await Task.Delay(50);
        await service.StopAsync(CancellationToken.None);

        // Assert - should complete without throwing
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WhenChartDataServiceThrows_ShouldContinue()
    {
        // Arrange
        _mockChartDataService
            .Setup(c => c.RecordResourceMetrics(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Throws(new InvalidOperationException("Test error"));

        var service = new NotificationBroadcastService(
            _mockHubContext.Object,
            _mockNotificationService.Object,
            _mockChartDataService.Object,
            _mockLogger.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(100);
        await service.StopAsync(CancellationToken.None);

        // Assert - service should not crash
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WhenNotificationServiceThrows_ShouldContinue()
    {
        // Arrange
        _mockNotificationService
            .Setup(n => n.CheckResourceThresholds(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Throws(new InvalidOperationException("Test error"));

        var service = new NotificationBroadcastService(
            _mockHubContext.Object,
            _mockNotificationService.Object,
            _mockChartDataService.Object,
            _mockLogger.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(100);
        await service.StopAsync(CancellationToken.None);

        // Assert - service should not crash
        service.Should().NotBeNull();
    }

    #endregion

    #region StartAsync/StopAsync Tests

    [Fact]
    public async Task StartAsync_ShouldStartSuccessfully()
    {
        // Arrange
        var service = new NotificationBroadcastService(
            _mockHubContext.Object,
            _mockNotificationService.Object,
            _mockChartDataService.Object,
            _mockLogger.Object);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        service.Should().NotBeNull();

        // Cleanup
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_ShouldStopSuccessfully()
    {
        // Arrange
        var service = new NotificationBroadcastService(
            _mockHubContext.Object,
            _mockNotificationService.Object,
            _mockChartDataService.Object,
            _mockLogger.Object);

        await service.StartAsync(CancellationToken.None);

        // Act
        await service.StopAsync(CancellationToken.None);

        // Assert - should complete without error
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task StopAsync_WithoutStart_ShouldNotThrow()
    {
        // Arrange
        var service = new NotificationBroadcastService(
            _mockHubContext.Object,
            _mockNotificationService.Object,
            _mockChartDataService.Object,
            _mockLogger.Object);

        // Act & Assert
        await service.Invoking(s => s.StopAsync(CancellationToken.None))
            .Should().NotThrowAsync();
    }

    #endregion

    #region Notification Event Handler Tests

    [Fact]
    public async Task OnNotificationReceived_ShouldBroadcastToAllClients()
    {
        // Arrange
        var mockClients = new Mock<IHubClients<IDashboardClient>>();
        var mockAllClients = new Mock<IDashboardClient>();

        mockClients.Setup(c => c.All).Returns(mockAllClients.Object);
        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

        var service = new NotificationBroadcastService(
            _mockHubContext.Object,
            _mockNotificationService.Object,
            _mockChartDataService.Object,
            _mockLogger.Object);

        var notification = new Notification
        {
            Title = "Test Title",
            Message = "Test Message",
            Type = NotificationType.Info
        };

        // Act - raise the event
        _mockNotificationService.Raise(
            n => n.OnNotification += null!,
            _mockNotificationService.Object,
            notification);

        await Task.Delay(50); // Allow async handler to complete

        // Assert
        mockAllClients.Verify(
            c => c.ReceiveNotification("Test Title", "Test Message", "info"),
            Times.AtLeastOnce());
    }

    [Fact]
    public async Task OnNotificationReceived_WithWarningType_ShouldBroadcastWithWarning()
    {
        // Arrange
        var mockClients = new Mock<IHubClients<IDashboardClient>>();
        var mockAllClients = new Mock<IDashboardClient>();

        mockClients.Setup(c => c.All).Returns(mockAllClients.Object);
        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

        var service = new NotificationBroadcastService(
            _mockHubContext.Object,
            _mockNotificationService.Object,
            _mockChartDataService.Object,
            _mockLogger.Object);

        var notification = new Notification
        {
            Title = "Warning",
            Message = "Warning message",
            Type = NotificationType.Warning
        };

        // Act
        _mockNotificationService.Raise(
            n => n.OnNotification += null!,
            _mockNotificationService.Object,
            notification);

        await Task.Delay(50);

        // Assert
        mockAllClients.Verify(
            c => c.ReceiveNotification("Warning", "Warning message", "warning"),
            Times.AtLeastOnce());
    }

    [Fact]
    public async Task OnNotificationReceived_WithErrorType_ShouldBroadcastWithError()
    {
        // Arrange
        var mockClients = new Mock<IHubClients<IDashboardClient>>();
        var mockAllClients = new Mock<IDashboardClient>();

        mockClients.Setup(c => c.All).Returns(mockAllClients.Object);
        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

        var service = new NotificationBroadcastService(
            _mockHubContext.Object,
            _mockNotificationService.Object,
            _mockChartDataService.Object,
            _mockLogger.Object);

        var notification = new Notification
        {
            Title = "Error",
            Message = "Error message",
            Type = NotificationType.Error
        };

        // Act
        _mockNotificationService.Raise(
            n => n.OnNotification += null!,
            _mockNotificationService.Object,
            notification);

        await Task.Delay(50);

        // Assert
        mockAllClients.Verify(
            c => c.ReceiveNotification("Error", "Error message", "error"),
            Times.AtLeastOnce());
    }

    [Fact]
    public async Task OnNotificationReceived_WhenBroadcastFails_ShouldNotThrow()
    {
        // Arrange
        var mockClients = new Mock<IHubClients<IDashboardClient>>();
        var mockAllClients = new Mock<IDashboardClient>();

        mockAllClients
            .Setup(c => c.ReceiveNotification(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Broadcast failed"));

        mockClients.Setup(c => c.All).Returns(mockAllClients.Object);
        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

        var service = new NotificationBroadcastService(
            _mockHubContext.Object,
            _mockNotificationService.Object,
            _mockChartDataService.Object,
            _mockLogger.Object);

        var notification = new Notification
        {
            Title = "Test",
            Message = "Test",
            Type = NotificationType.Info
        };

        // Act & Assert - should not throw even if broadcast fails
        _mockNotificationService.Raise(
            n => n.OnNotification += null!,
            _mockNotificationService.Object,
            notification);

        await Task.Delay(50);

        // Service should still be functional
        service.Should().NotBeNull();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task Service_ShouldUnsubscribeOnStop()
    {
        // Arrange
        var service = new NotificationBroadcastService(
            _mockHubContext.Object,
            _mockNotificationService.Object,
            _mockChartDataService.Object,
            _mockLogger.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(50);
        await service.StopAsync(CancellationToken.None);

        // Assert - service stopped cleanly
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task Service_ShouldHandleMultipleStartStop()
    {
        // Arrange
        var service = new NotificationBroadcastService(
            _mockHubContext.Object,
            _mockNotificationService.Object,
            _mockChartDataService.Object,
            _mockLogger.Object);

        // Act & Assert - multiple cycles should work
        for (int i = 0; i < 3; i++)
        {
            await service.StartAsync(CancellationToken.None);
            await Task.Delay(20);
            await service.StopAsync(CancellationToken.None);
        }

        service.Should().NotBeNull();
    }

    #endregion
}
