using FluentAssertions;
using HoNfigurator.Core.Health;
using HoNfigurator.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace HoNfigurator.Tests.Health;

#region PingResult Tests

public class PingResultTests
{
    [Fact]
    public void PingResult_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var result = new PingResult();

        // Assert
        result.Port.Should().Be(0);
        result.Success.Should().BeFalse();
        result.ResponseTime.Should().Be(0);
        result.Error.Should().BeNull();
    }

    [Fact]
    public void PingResult_CanSetPort()
    {
        // Arrange & Act
        var result = new PingResult { Port = 11031 };

        // Assert
        result.Port.Should().Be(11031);
    }

    [Fact]
    public void PingResult_CanSetSuccess()
    {
        // Arrange & Act
        var result = new PingResult { Success = true };

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public void PingResult_CanSetResponseTime()
    {
        // Arrange & Act
        var result = new PingResult { ResponseTime = 150 };

        // Assert
        result.ResponseTime.Should().Be(150);
    }

    [Fact]
    public void PingResult_CanSetError()
    {
        // Arrange & Act
        var result = new PingResult { Error = "Connection refused" };

        // Assert
        result.Error.Should().Be("Connection refused");
    }

    [Fact]
    public void PingResult_SuccessfulPing_HasNoError()
    {
        // Arrange & Act
        var result = new PingResult
        {
            Port = 11031,
            Success = true,
            ResponseTime = 25,
            Error = null
        };

        // Assert
        result.Success.Should().BeTrue();
        result.Error.Should().BeNull();
        result.ResponseTime.Should().BePositive();
    }

    [Fact]
    public void PingResult_FailedPing_HasError()
    {
        // Arrange & Act
        var result = new PingResult
        {
            Port = 11031,
            Success = false,
            ResponseTime = 0,
            Error = "Timeout"
        };

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }
}

#endregion

#region ServerHealthStatus Tests

public class ServerHealthStatusTests
{
    [Fact]
    public void ServerHealthStatus_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var status = new ServerHealthStatus();

        // Assert
        status.Port.Should().Be(0);
        status.IsHealthy.Should().BeFalse();
        status.ConsecutiveFailures.Should().Be(0);
        status.LastPingTime.Should().BeNull();
        status.LastResponseTime.Should().Be(0);
        status.AverageResponseTime.Should().Be(0);
    }

    [Fact]
    public void ServerHealthStatus_CanSetPort()
    {
        // Arrange & Act
        var status = new ServerHealthStatus { Port = 11032 };

        // Assert
        status.Port.Should().Be(11032);
    }

    [Fact]
    public void ServerHealthStatus_CanSetIsHealthy()
    {
        // Arrange & Act
        var status = new ServerHealthStatus { IsHealthy = true };

        // Assert
        status.IsHealthy.Should().BeTrue();
    }

    [Fact]
    public void ServerHealthStatus_CanSetConsecutiveFailures()
    {
        // Arrange & Act
        var status = new ServerHealthStatus { ConsecutiveFailures = 5 };

        // Assert
        status.ConsecutiveFailures.Should().Be(5);
    }

    [Fact]
    public void ServerHealthStatus_CanSetLastPingTime()
    {
        // Arrange
        var pingTime = DateTime.UtcNow;

        // Act
        var status = new ServerHealthStatus { LastPingTime = pingTime };

        // Assert
        status.LastPingTime.Should().Be(pingTime);
    }

    [Fact]
    public void ServerHealthStatus_CanSetLastResponseTime()
    {
        // Arrange & Act
        var status = new ServerHealthStatus { LastResponseTime = 50 };

        // Assert
        status.LastResponseTime.Should().Be(50);
    }

    [Fact]
    public void ServerHealthStatus_CanSetAverageResponseTime()
    {
        // Arrange & Act
        var status = new ServerHealthStatus { AverageResponseTime = 42.5 };

        // Assert
        status.AverageResponseTime.Should().Be(42.5);
    }

    [Fact]
    public void ServerHealthStatus_HealthyServer_HasCorrectState()
    {
        // Arrange & Act
        var status = new ServerHealthStatus
        {
            Port = 11031,
            IsHealthy = true,
            ConsecutiveFailures = 0,
            LastPingTime = DateTime.UtcNow,
            LastResponseTime = 25,
            AverageResponseTime = 30.5
        };

        // Assert
        status.IsHealthy.Should().BeTrue();
        status.ConsecutiveFailures.Should().Be(0);
        status.AverageResponseTime.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ServerHealthStatus_UnhealthyServer_HasCorrectState()
    {
        // Arrange & Act
        var status = new ServerHealthStatus
        {
            Port = 11031,
            IsHealthy = false,
            ConsecutiveFailures = 5,
            LastPingTime = DateTime.UtcNow.AddMinutes(-5),
            LastResponseTime = 0,
            AverageResponseTime = 0
        };

        // Assert
        status.IsHealthy.Should().BeFalse();
        status.ConsecutiveFailures.Should().BeGreaterThan(0);
    }
}

#endregion

#region ServerUnhealthyEventArgs Tests

public class ServerUnhealthyEventArgsTests
{
    [Fact]
    public void ServerUnhealthyEventArgs_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var args = new ServerUnhealthyEventArgs();

        // Assert
        args.Port.Should().Be(0);
        args.ConsecutiveFailures.Should().Be(0);
        args.LastError.Should().BeNull();
    }

    [Fact]
    public void ServerUnhealthyEventArgs_CanSetPort()
    {
        // Arrange & Act
        var args = new ServerUnhealthyEventArgs { Port = 11031 };

        // Assert
        args.Port.Should().Be(11031);
    }

    [Fact]
    public void ServerUnhealthyEventArgs_CanSetConsecutiveFailures()
    {
        // Arrange & Act
        var args = new ServerUnhealthyEventArgs { ConsecutiveFailures = 3 };

        // Assert
        args.ConsecutiveFailures.Should().Be(3);
    }

    [Fact]
    public void ServerUnhealthyEventArgs_CanSetLastError()
    {
        // Arrange & Act
        var args = new ServerUnhealthyEventArgs { LastError = "Connection refused" };

        // Assert
        args.LastError.Should().Be("Connection refused");
    }

    [Fact]
    public void ServerUnhealthyEventArgs_InheritsFromEventArgs()
    {
        // Arrange & Act
        var args = new ServerUnhealthyEventArgs();

        // Assert
        args.Should().BeAssignableTo<EventArgs>();
    }

    [Fact]
    public void ServerUnhealthyEventArgs_CompleteState()
    {
        // Arrange & Act
        var args = new ServerUnhealthyEventArgs
        {
            Port = 11031,
            ConsecutiveFailures = 5,
            LastError = "Socket error: ConnectionRefused"
        };

        // Assert
        args.Port.Should().Be(11031);
        args.ConsecutiveFailures.Should().Be(5);
        args.LastError.Should().Contain("ConnectionRefused");
    }
}

#endregion

#region ServerRecoveredEventArgs Tests

public class ServerRecoveredEventArgsTests
{
    [Fact]
    public void ServerRecoveredEventArgs_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var args = new ServerRecoveredEventArgs();

        // Assert
        args.Port.Should().Be(0);
        args.ResponseTime.Should().Be(0);
    }

    [Fact]
    public void ServerRecoveredEventArgs_CanSetPort()
    {
        // Arrange & Act
        var args = new ServerRecoveredEventArgs { Port = 11031 };

        // Assert
        args.Port.Should().Be(11031);
    }

    [Fact]
    public void ServerRecoveredEventArgs_CanSetResponseTime()
    {
        // Arrange & Act
        var args = new ServerRecoveredEventArgs { ResponseTime = 50 };

        // Assert
        args.ResponseTime.Should().Be(50);
    }

    [Fact]
    public void ServerRecoveredEventArgs_InheritsFromEventArgs()
    {
        // Arrange & Act
        var args = new ServerRecoveredEventArgs();

        // Assert
        args.Should().BeAssignableTo<EventArgs>();
    }

    [Fact]
    public void ServerRecoveredEventArgs_CompleteState()
    {
        // Arrange & Act
        var args = new ServerRecoveredEventArgs
        {
            Port = 11032,
            ResponseTime = 25
        };

        // Assert
        args.Port.Should().Be(11032);
        args.ResponseTime.Should().Be(25);
    }
}

#endregion

#region RestartRecommendedEventArgs Tests

public class RestartRecommendedEventArgsTests
{
    [Fact]
    public void RestartRecommendedEventArgs_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var args = new RestartRecommendedEventArgs();

        // Assert
        args.Port.Should().Be(0);
        args.Reason.Should().BeEmpty();
    }

    [Fact]
    public void RestartRecommendedEventArgs_CanSetPort()
    {
        // Arrange & Act
        var args = new RestartRecommendedEventArgs { Port = 11031 };

        // Assert
        args.Port.Should().Be(11031);
    }

    [Fact]
    public void RestartRecommendedEventArgs_CanSetReason()
    {
        // Arrange & Act
        var args = new RestartRecommendedEventArgs { Reason = "Server unresponsive" };

        // Assert
        args.Reason.Should().Be("Server unresponsive");
    }

    [Fact]
    public void RestartRecommendedEventArgs_InheritsFromEventArgs()
    {
        // Arrange & Act
        var args = new RestartRecommendedEventArgs();

        // Assert
        args.Should().BeAssignableTo<EventArgs>();
    }

    [Fact]
    public void RestartRecommendedEventArgs_CompleteState()
    {
        // Arrange & Act
        var args = new RestartRecommendedEventArgs
        {
            Port = 11031,
            Reason = "Server unresponsive after 6 ping failures"
        };

        // Assert
        args.Port.Should().Be(11031);
        args.Reason.Should().Contain("6 ping failures");
    }
}

#endregion

#region AutoPingHealthService Tests

public class AutoPingHealthServiceTests : IDisposable
{
    private readonly Mock<ILogger<AutoPingHealthService>> _loggerMock;
    private readonly HoNConfiguration _config;
    private readonly AutoPingHealthService _service;

    public AutoPingHealthServiceTests()
    {
        _loggerMock = new Mock<ILogger<AutoPingHealthService>>();
        _config = new HoNConfiguration
        {
            HonData = new HoNData
            {
                MasterServer = "api.kongor.net"
            },
            HealthMonitoring = new HealthMonitoringConfiguration
            {
                AutoPingIntervalMs = 1000,
                MaxConsecutiveFailures = 3
            }
        };
        _service = new AutoPingHealthService(_loggerMock.Object, _config);
    }

    public void Dispose()
    {
        _service.Dispose();
    }

    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        // Arrange & Act
        using var service = new AutoPingHealthService(_loggerMock.Object, _config);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void GetHealthStatus_BeforeStartMonitoring_ReturnsEmptyDictionary()
    {
        // Arrange & Act
        var status = _service.GetHealthStatus();

        // Assert
        status.Should().BeEmpty();
    }

    [Fact]
    public void GetServerHealth_UnknownPort_ReturnsNull()
    {
        // Arrange & Act
        var health = _service.GetServerHealth(11031);

        // Assert
        health.Should().BeNull();
    }

    [Fact]
    public void AddServer_AddsToMonitoring()
    {
        // Arrange & Act
        _service.AddServer(11031);
        var status = _service.GetHealthStatus();

        // Assert
        status.Should().ContainKey(11031);
    }

    [Fact]
    public void AddServer_SamePortTwice_DoesNotDuplicate()
    {
        // Arrange
        _service.AddServer(11031);

        // Act
        _service.AddServer(11031);
        var status = _service.GetHealthStatus();

        // Assert
        status.Should().HaveCount(1);
    }

    [Fact]
    public void AddServer_MultiplePorts_TracksAll()
    {
        // Arrange & Act
        _service.AddServer(11031);
        _service.AddServer(11032);
        _service.AddServer(11033);
        var status = _service.GetHealthStatus();

        // Assert
        status.Should().HaveCount(3);
        status.Keys.Should().Contain(new[] { 11031, 11032, 11033 });
    }

    [Fact]
    public void RemoveServer_RemovesFromMonitoring()
    {
        // Arrange
        _service.AddServer(11031);
        _service.AddServer(11032);

        // Act
        _service.RemoveServer(11031);
        var status = _service.GetHealthStatus();

        // Assert
        status.Should().NotContainKey(11031);
        status.Should().ContainKey(11032);
    }

    [Fact]
    public void RemoveServer_UnknownPort_DoesNotThrow()
    {
        // Arrange & Act
        var act = () => _service.RemoveServer(99999);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void GetServerHealth_AfterAddServer_ReturnsStatus()
    {
        // Arrange
        _service.AddServer(11031);

        // Act
        var health = _service.GetServerHealth(11031);

        // Assert
        health.Should().NotBeNull();
        health!.Port.Should().Be(11031);
    }

    [Fact]
    public void GetServerHealth_NewServer_IsInitiallyHealthy()
    {
        // Arrange
        _service.AddServer(11031);

        // Act
        var health = _service.GetServerHealth(11031);

        // Assert - new servers are healthy (0 failures < 3 threshold)
        health.Should().NotBeNull();
        health!.IsHealthy.Should().BeTrue();
        health.ConsecutiveFailures.Should().Be(0);
    }

    [Fact]
    public void StartMonitoring_WithPorts_InitializesHealthStates()
    {
        // Arrange
        var ports = new[] { 11031, 11032, 11033 };

        // Act
        _service.StartMonitoring(ports);
        var status = _service.GetHealthStatus();

        // Assert
        status.Should().HaveCount(3);
        foreach (var port in ports)
        {
            status.Should().ContainKey(port);
        }
    }

    [Fact]
    public void StartMonitoring_ClearsExistingState()
    {
        // Arrange
        _service.AddServer(99999);

        // Act
        _service.StartMonitoring(new[] { 11031 });
        var status = _service.GetHealthStatus();

        // Assert
        status.Should().NotContainKey(99999);
        status.Should().ContainKey(11031);
    }

    [Fact]
    public void StartMonitoring_TwiceLogs_Warning()
    {
        // Arrange
        _service.StartMonitoring(new[] { 11031 });

        // Act
        _service.StartMonitoring(new[] { 11032 });

        // Assert - second call should log warning, first set of ports retained
        var status = _service.GetHealthStatus();
        status.Should().ContainKey(11031);
    }

    [Fact]
    public async Task StopMonitoringAsync_AfterStart_Stops()
    {
        // Arrange
        _service.StartMonitoring(new[] { 11031 });

        // Act
        await _service.StopMonitoringAsync();

        // Assert - should not throw
        var status = _service.GetHealthStatus();
        status.Should().ContainKey(11031); // State is preserved after stop
    }

    [Fact]
    public async Task StopMonitoringAsync_WhenNotStarted_DoesNotThrow()
    {
        // Arrange & Act
        var act = async () => await _service.StopMonitoringAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void Dispose_DisposesResources()
    {
        // Arrange
        var service = new AutoPingHealthService(_loggerMock.Object, _config);
        service.StartMonitoring(new[] { 11031 });

        // Act & Assert
        var act = () => service.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_MultipleDispose_DoesNotThrow()
    {
        // Arrange
        var service = new AutoPingHealthService(_loggerMock.Object, _config);

        // Act & Assert
        var act = () =>
        {
            service.Dispose();
            service.Dispose();
        };
        act.Should().NotThrow();
    }

    [Fact]
    public async Task PingServerAsync_NonExistentServer_ReturnsFailed()
    {
        // Arrange - use a port that's unlikely to have a HoN server
        var port = 65000;

        // Act
        var result = await _service.PingServerAsync(port, timeoutMs: 100);

        // Assert
        result.Should().NotBeNull();
        result.Port.Should().Be(port);
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ServerUnhealthy_Event_CanBeSubscribed()
    {
        // Arrange
        var eventRaised = false;
        _service.ServerUnhealthy += (sender, args) => eventRaised = true;

        // Assert - just verifying subscription works
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public void ServerRecovered_Event_CanBeSubscribed()
    {
        // Arrange
        var eventRaised = false;
        _service.ServerRecovered += (sender, args) => eventRaised = true;

        // Assert - just verifying subscription works
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public void RestartRecommended_Event_CanBeSubscribed()
    {
        // Arrange
        var eventRaised = false;
        _service.RestartRecommended += (sender, args) => eventRaised = true;

        // Assert - just verifying subscription works
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public void GetHealthStatus_ReturnsCopy_NotReference()
    {
        // Arrange
        _service.AddServer(11031);
        var status1 = _service.GetHealthStatus();

        // Act - add another server
        _service.AddServer(11032);
        var status2 = _service.GetHealthStatus();

        // Assert - first snapshot should not be affected
        status1.Should().HaveCount(1);
        status2.Should().HaveCount(2);
    }
}

#endregion

#region AutoPingHealthService Configuration Tests

public class AutoPingHealthServiceConfigurationTests : IDisposable
{
    private readonly Mock<ILogger<AutoPingHealthService>> _loggerMock;
    private readonly HoNConfiguration _config;
    private readonly AutoPingHealthService _service;

    public AutoPingHealthServiceConfigurationTests()
    {
        _loggerMock = new Mock<ILogger<AutoPingHealthService>>();
        _config = new HoNConfiguration
        {
            HonData = new HoNData
            {
                MasterServer = "api.kongor.net"
            }
        };
        _service = new AutoPingHealthService(_loggerMock.Object, _config);
    }

    public void Dispose()
    {
        _service.Dispose();
    }

    [Fact]
    public void Constructor_WithNullHealthMonitoring_UsesDefaults()
    {
        // Arrange - config without HealthMonitoring
        var configWithoutMonitoring = new HoNConfiguration
        {
            HonData = new HoNData { MasterServer = "test" }
        };

        // Act
        using var service = new AutoPingHealthService(_loggerMock.Object, configWithoutMonitoring);

        // Assert - should use default values and not throw
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithCustomConfig_AcceptsValues()
    {
        // Arrange
        var customConfig = new HoNConfiguration
        {
            HonData = new HoNData { MasterServer = "test" },
            HealthMonitoring = new HealthMonitoringConfiguration
            {
                AutoPingIntervalMs = 5000,
                MaxConsecutiveFailures = 5
            }
        };

        // Act
        using var service = new AutoPingHealthService(_loggerMock.Object, customConfig);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void StartMonitoring_WithEmptyPorts_InitializesEmptyState()
    {
        // Arrange & Act
        _service.StartMonitoring(Array.Empty<int>());
        var status = _service.GetHealthStatus();

        // Assert
        status.Should().BeEmpty();
    }
}

#endregion

#region HealthMonitoringConfiguration Tests

public class HealthMonitoringConfigurationTests
{
    [Fact]
    public void HealthMonitoringConfiguration_DefaultValues()
    {
        // Arrange & Act
        var config = new HealthMonitoringConfiguration();

        // Assert - default values are initialized
        config.AutoPingIntervalMs.Should().BeGreaterThan(0);
        config.MaxConsecutiveFailures.Should().BeGreaterThan(0);
    }

    [Fact]
    public void HealthMonitoringConfiguration_CanSetAutoPingIntervalMs()
    {
        // Arrange & Act
        var config = new HealthMonitoringConfiguration { AutoPingIntervalMs = 30000 };

        // Assert
        config.AutoPingIntervalMs.Should().Be(30000);
    }

    [Fact]
    public void HealthMonitoringConfiguration_CanSetMaxConsecutiveFailures()
    {
        // Arrange & Act
        var config = new HealthMonitoringConfiguration { MaxConsecutiveFailures = 5 };

        // Assert
        config.MaxConsecutiveFailures.Should().Be(5);
    }

    [Fact]
    public void HealthMonitoringConfiguration_TypicalValues()
    {
        // Arrange & Act
        var config = new HealthMonitoringConfiguration
        {
            AutoPingIntervalMs = 30000,
            MaxConsecutiveFailures = 3
        };

        // Assert
        config.AutoPingIntervalMs.Should().Be(30000);
        config.MaxConsecutiveFailures.Should().Be(3);
    }
}

#endregion
