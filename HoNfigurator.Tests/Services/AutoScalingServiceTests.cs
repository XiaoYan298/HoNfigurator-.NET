using FluentAssertions;
using HoNfigurator.Core.Models;
using HoNfigurator.GameServer.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HoNfigurator.Tests.Services;

/// <summary>
/// Tests for AutoScalingService - Game server auto-scaling
/// </summary>
public class AutoScalingServiceTests
{
    private readonly Mock<ILogger<AutoScalingService>> _loggerMock;
    private readonly Mock<IGameServerManager> _serverManagerMock;

    public AutoScalingServiceTests()
    {
        _loggerMock = new Mock<ILogger<AutoScalingService>>();
        _serverManagerMock = new Mock<IGameServerManager>();
    }

    private AutoScalingService CreateService(HoNConfiguration? config = null)
    {
        config ??= CreateTestConfig();
        return new AutoScalingService(
            _loggerMock.Object,
            _serverManagerMock.Object,
            config);
    }

    private HoNConfiguration CreateTestConfig(bool enabled = false)
    {
        return new HoNConfiguration
        {
            ApplicationData = new ApplicationData
            {
                AutoScaling = new AutoScalingSettings
                {
                    Enabled = enabled,
                    MinServers = 1,
                    MaxServers = 10,
                    ScaleUpThreshold = 80,
                    ScaleDownThreshold = 20,
                    CheckIntervalSeconds = 30,
                    CooldownSeconds = 60,
                    MinReadyServers = 1
                }
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
    public void Constructor_WithNullSettings_ShouldUseDefaults()
    {
        // Arrange
        var config = new HoNConfiguration
        {
            ApplicationData = new ApplicationData { AutoScaling = null }
        };

        // Act
        var service = CreateService(config);

        // Assert
        service.Should().NotBeNull();
    }

    #endregion

    #region IsEnabled Tests

    [Fact]
    public void IsEnabled_WhenEnabled_ShouldReturnTrue()
    {
        // Arrange
        var config = CreateTestConfig(enabled: true);
        var service = CreateService(config);

        // Assert
        service.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void IsEnabled_WhenDisabled_ShouldReturnFalse()
    {
        // Arrange
        var config = CreateTestConfig(enabled: false);
        var service = CreateService(config);

        // Assert
        service.IsEnabled.Should().BeFalse();
    }

    #endregion

    #region LastScaleTime Tests

    [Fact]
    public void LastScaleTime_Initially_ShouldBeMinValue()
    {
        // Arrange
        var service = CreateService();

        // Assert
        service.LastScaleTime.Should().Be(DateTime.MinValue);
    }

    #endregion

    #region LastScaleAction Tests

    [Fact]
    public void LastScaleAction_Initially_ShouldBeNone()
    {
        // Arrange
        var service = CreateService();

        // Assert
        service.LastScaleAction.Should().Be("None");
    }

    #endregion

    #region ExecuteAsync Tests

    [Fact]
    public async Task ExecuteAsync_WhenDisabled_ShouldReturn()
    {
        // Arrange
        var config = CreateTestConfig(enabled: false);
        var service = CreateService(config);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(50);

        // Assert
        service.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellation_ShouldStop()
    {
        // Arrange
        var config = CreateTestConfig(enabled: true);
        var service = CreateService(config);
        using var cts = new CancellationTokenSource();

        _serverManagerMock.Setup(m => m.Instances)
            .Returns(new List<GameServerInstance>());

        // Act
        var task = service.StartAsync(cts.Token);
        await Task.Delay(50);
        cts.Cancel();

        // Assert
        var act = async () => await task;
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StopAsync_ShouldNotThrow()
    {
        // Arrange
        var service = CreateService();

        // Act
        var act = async () => await service.StopAsync(CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Service_ShouldImplementBackgroundService()
    {
        // Assert
        typeof(AutoScalingService).BaseType!.Name.Should().Be("BackgroundService");
    }

    [Fact]
    public async Task Service_WithNoServers_ShouldNotScale()
    {
        // Arrange
        var config = CreateTestConfig(enabled: true);
        var service = CreateService(config);
        
        _serverManagerMock.Setup(m => m.Instances)
            .Returns(new List<GameServerInstance>());

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(50);

        // Assert
        service.LastScaleAction.Should().Be("None");
    }

    #endregion
}

#region AutoScalingSettings DTO Tests

public class AutoScalingSettingsDtoTests
{
    [Fact]
    public void AutoScalingSettings_DefaultValues()
    {
        // Act
        var settings = new AutoScalingSettings();

        // Assert
        settings.Enabled.Should().BeFalse();
        settings.MinServers.Should().BeGreaterThanOrEqualTo(0);
        settings.MaxServers.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void AutoScalingSettings_ShouldAllowCustomValues()
    {
        // Act
        var settings = new AutoScalingSettings
        {
            Enabled = true,
            MinServers = 2,
            MaxServers = 20,
            ScaleUpThreshold = 90,
            ScaleDownThreshold = 10,
            CheckIntervalSeconds = 60,
            CooldownSeconds = 120,
            MinReadyServers = 2
        };

        // Assert
        settings.Enabled.Should().BeTrue();
        settings.MinServers.Should().Be(2);
        settings.MaxServers.Should().Be(20);
        settings.ScaleUpThreshold.Should().Be(90);
    }
}

#endregion
