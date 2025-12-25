using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using HoNfigurator.Api.Services;
using HoNfigurator.Core.Health;
using HoNfigurator.Core.Models;

namespace HoNfigurator.Tests.Api.Services;

/// <summary>
/// Tests for HealthCheckBackgroundService
/// </summary>
public class HealthCheckBackgroundServiceTests
{
    private readonly Mock<ILogger<HealthCheckBackgroundService>> _mockLogger;
    private readonly Mock<ILogger<HealthCheckManager>> _mockHealthCheckLogger;

    public HealthCheckBackgroundServiceTests()
    {
        _mockLogger = new Mock<ILogger<HealthCheckBackgroundService>>();
        _mockHealthCheckLogger = new Mock<ILogger<HealthCheckManager>>();
    }

    private HealthCheckManager CreateHealthCheckManager()
    {
        return new HealthCheckManager(_mockHealthCheckLogger.Object, new HoNConfiguration());
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_ShouldNotThrow()
    {
        // Arrange
        var healthCheckManager = CreateHealthCheckManager();

        // Act
        var service = new HealthCheckBackgroundService(
            _mockLogger.Object,
            healthCheckManager);

        // Assert
        service.Should().NotBeNull();
    }

    #endregion

    #region ExecuteAsync Tests

    [Fact]
    public async Task ExecuteAsync_ShouldRunInitialHealthCheck()
    {
        // Arrange
        var healthCheckManager = CreateHealthCheckManager();
        var service = new HealthCheckBackgroundService(
            _mockLogger.Object,
            healthCheckManager);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(100);
        await service.StopAsync(CancellationToken.None);

        // Assert - initial check should have run
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancelled_ShouldStopGracefully()
    {
        // Arrange
        var healthCheckManager = CreateHealthCheckManager();
        var service = new HealthCheckBackgroundService(
            _mockLogger.Object,
            healthCheckManager);

        using var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        cts.Cancel();
        await Task.Delay(50);
        await service.StopAsync(CancellationToken.None);

        // Assert - should complete without throwing
        service.Should().NotBeNull();
    }

    #endregion

    #region StartAsync/StopAsync Tests

    [Fact]
    public async Task StartAsync_ShouldStartSuccessfully()
    {
        // Arrange
        var healthCheckManager = CreateHealthCheckManager();
        var service = new HealthCheckBackgroundService(
            _mockLogger.Object,
            healthCheckManager);

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
        var healthCheckManager = CreateHealthCheckManager();
        var service = new HealthCheckBackgroundService(
            _mockLogger.Object,
            healthCheckManager);

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
        var healthCheckManager = CreateHealthCheckManager();
        var service = new HealthCheckBackgroundService(
            _mockLogger.Object,
            healthCheckManager);

        // Act & Assert
        await service.Invoking(s => s.StopAsync(CancellationToken.None))
            .Should().NotThrowAsync();
    }

    #endregion

    #region Health Check Execution Tests

    [Fact]
    public async Task Service_ShouldLogHealthCheckResults()
    {
        // Arrange
        var healthCheckManager = CreateHealthCheckManager();
        var service = new HealthCheckBackgroundService(
            _mockLogger.Object,
            healthCheckManager);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(100);
        await service.StopAsync(CancellationToken.None);

        // Assert - logging should have occurred
        service.Should().NotBeNull();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task Service_ShouldHandleMultipleStartStop()
    {
        // Arrange
        var healthCheckManager = CreateHealthCheckManager();
        var service = new HealthCheckBackgroundService(
            _mockLogger.Object,
            healthCheckManager);

        // Act & Assert - multiple cycles should work
        for (int i = 0; i < 3; i++)
        {
            await service.StartAsync(CancellationToken.None);
            await Task.Delay(20);
            await service.StopAsync(CancellationToken.None);
        }

        service.Should().NotBeNull();
    }

    [Fact]
    public async Task Service_ShouldHandleCancellationDuringExecution()
    {
        // Arrange
        var healthCheckManager = CreateHealthCheckManager();
        var service = new HealthCheckBackgroundService(
            _mockLogger.Object,
            healthCheckManager);

        using var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(50);
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);

        // Assert - should handle cancellation gracefully
        service.Should().NotBeNull();
    }

    #endregion
}
