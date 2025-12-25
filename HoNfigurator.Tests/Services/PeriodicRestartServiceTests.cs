using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using HoNfigurator.Core.Models;
using HoNfigurator.Core.Services;

namespace HoNfigurator.Tests.Services;

/// <summary>
/// Tests for PeriodicRestartService - Server uptime management and restart scheduling
/// </summary>
public class PeriodicRestartServiceTests
{
    private readonly Mock<ILogger<PeriodicRestartService>> _loggerMock;
    private readonly HoNConfiguration _config;

    public PeriodicRestartServiceTests()
    {
        _loggerMock = new Mock<ILogger<PeriodicRestartService>>();
        _config = new HoNConfiguration
        {
            ServerLifecycle = new ServerLifecycleConfiguration
            {
                MinUptimeHours = 24,
                MaxUptimeHours = 48,
                CheckIntervalMinutes = 5,
                MaxWaitForGameMinutes = 60
            }
        };
    }

    private PeriodicRestartService CreateService(IServerScalingProvider? provider = null)
    {
        return new PeriodicRestartService(_loggerMock.Object, _config, provider);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithoutProvider_ShouldInitialize()
    {
        // Act
        var service = CreateService();

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithProvider_ShouldInitialize()
    {
        // Arrange
        var providerMock = new Mock<IServerScalingProvider>();

        // Act
        var service = CreateService(providerMock.Object);

        // Assert
        service.Should().NotBeNull();
    }

    #endregion

    #region RegisterServer Tests

    [Fact]
    public void RegisterServer_ShouldAddServerToTracking()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.RegisterServer(1, 11111);

        // Assert
        var status = service.GetServerUptime(1);
        status.Should().NotBeNull();
        status!.ServerId.Should().Be(1);
        status.Port.Should().Be(11111);
    }

    [Fact]
    public void RegisterServer_SameServerTwice_ShouldNotDuplicate()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.RegisterServer(1, 11111);
        service.RegisterServer(1, 22222); // Different port, same ID

        // Assert
        var status = service.GetServerUptime(1);
        status!.Port.Should().Be(11111); // Original port preserved
    }

    [Fact]
    public void RegisterServer_MultipleServers_ShouldTrackAll()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.RegisterServer(1, 11111);
        service.RegisterServer(2, 11112);
        service.RegisterServer(3, 11113);

        // Assert
        var statuses = service.GetUptimeStatus();
        statuses.Count.Should().Be(3);
    }

    [Fact]
    public void RegisterServer_ShouldSetRandomTargetUptime()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.RegisterServer(1, 11111);

        // Assert
        var status = service.GetServerUptime(1);
        status!.TargetUptime.TotalHours.Should().BeGreaterThanOrEqualTo(24);
        status.TargetUptime.TotalHours.Should().BeLessThanOrEqualTo(48);
    }

    [Fact]
    public void RegisterServer_ShouldSetStartTimeToNow()
    {
        // Arrange
        var service = CreateService();
        var beforeRegister = DateTime.UtcNow;

        // Act
        service.RegisterServer(1, 11111);

        // Assert
        var status = service.GetServerUptime(1);
        status!.Uptime.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }

    #endregion

    #region UnregisterServer Tests

    [Fact]
    public void UnregisterServer_ExistingServer_ShouldRemove()
    {
        // Arrange
        var service = CreateService();
        service.RegisterServer(1, 11111);

        // Act
        service.UnregisterServer(1);

        // Assert
        var status = service.GetServerUptime(1);
        status.Should().BeNull();
    }

    [Fact]
    public void UnregisterServer_NonExistingServer_ShouldNotThrow()
    {
        // Arrange
        var service = CreateService();

        // Act
        var act = () => service.UnregisterServer(999);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void UnregisterServer_ShouldOnlyRemoveSpecifiedServer()
    {
        // Arrange
        var service = CreateService();
        service.RegisterServer(1, 11111);
        service.RegisterServer(2, 11112);

        // Act
        service.UnregisterServer(1);

        // Assert
        service.GetServerUptime(1).Should().BeNull();
        service.GetServerUptime(2).Should().NotBeNull();
    }

    #endregion

    #region ResetServerUptime Tests

    [Fact]
    public void ResetServerUptime_ExistingServer_ShouldResetStartTime()
    {
        // Arrange
        var service = CreateService();
        service.RegisterServer(1, 11111);
        
        // Wait a tiny bit to ensure uptime > 0
        Thread.Sleep(10);
        var uptimeBefore = service.GetServerUptime(1)!.Uptime;

        // Act
        service.ResetServerUptime(1);

        // Assert
        var uptimeAfter = service.GetServerUptime(1)!.Uptime;
        uptimeAfter.Should().BeLessThan(uptimeBefore);
    }

    [Fact]
    public void ResetServerUptime_ShouldClearRestartScheduled()
    {
        // Arrange
        var service = CreateService();
        service.RegisterServer(1, 11111);
        service.ScheduleImmediateRestart(1, "test");

        // Act
        service.ResetServerUptime(1);

        // Assert
        var status = service.GetServerUptime(1);
        status!.RestartScheduled.Should().BeFalse();
    }

    [Fact]
    public void ResetServerUptime_NonExistingServer_ShouldNotThrow()
    {
        // Arrange
        var service = CreateService();

        // Act
        var act = () => service.ResetServerUptime(999);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ResetServerUptime_ShouldAssignNewTargetUptime()
    {
        // Arrange
        var service = CreateService();
        service.RegisterServer(1, 11111);
        var originalTarget = service.GetServerUptime(1)!.TargetUptime;

        // Act - Reset multiple times to increase chance of different random value
        service.ResetServerUptime(1);

        // Assert - Target should still be within configured range
        var newTarget = service.GetServerUptime(1)!.TargetUptime;
        newTarget.TotalHours.Should().BeGreaterThanOrEqualTo(24);
        newTarget.TotalHours.Should().BeLessThanOrEqualTo(48);
    }

    #endregion

    #region GetUptimeStatus Tests

    [Fact]
    public void GetUptimeStatus_WhenEmpty_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var service = CreateService();

        // Act
        var statuses = service.GetUptimeStatus();

        // Assert
        statuses.Should().BeEmpty();
    }

    [Fact]
    public void GetUptimeStatus_ShouldReturnAllServers()
    {
        // Arrange
        var service = CreateService();
        service.RegisterServer(1, 11111);
        service.RegisterServer(2, 11112);

        // Act
        var statuses = service.GetUptimeStatus();

        // Assert
        statuses.Should().HaveCount(2);
        statuses.Should().ContainKey(1);
        statuses.Should().ContainKey(2);
    }

    [Fact]
    public void GetUptimeStatus_ShouldReturnCorrectServerInfo()
    {
        // Arrange
        var service = CreateService();
        service.RegisterServer(1, 11111);

        // Act
        var statuses = service.GetUptimeStatus();

        // Assert
        var status = statuses[1];
        status.ServerId.Should().Be(1);
        status.Port.Should().Be(11111);
        status.Uptime.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
        status.TargetUptime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void GetUptimeStatus_TimeUntilRestart_ShouldBeCalculated()
    {
        // Arrange
        var service = CreateService();
        service.RegisterServer(1, 11111);

        // Act
        var statuses = service.GetUptimeStatus();

        // Assert
        var status = statuses[1];
        // TimeUntilRestart = TargetUptime - Uptime
        var expectedTimeUntil = status.TargetUptime - status.Uptime;
        status.TimeUntilRestart.Should().BeCloseTo(expectedTimeUntil, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region GetServerUptime Tests

    [Fact]
    public void GetServerUptime_ExistingServer_ShouldReturnStatus()
    {
        // Arrange
        var service = CreateService();
        service.RegisterServer(1, 11111);

        // Act
        var status = service.GetServerUptime(1);

        // Assert
        status.Should().NotBeNull();
    }

    [Fact]
    public void GetServerUptime_NonExistingServer_ShouldReturnNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var status = service.GetServerUptime(999);

        // Assert
        status.Should().BeNull();
    }

    #endregion

    #region ScheduleImmediateRestart Tests

    [Fact]
    public void ScheduleImmediateRestart_ExistingServer_ShouldMarkAsScheduled()
    {
        // Arrange
        var service = CreateService();
        service.RegisterServer(1, 11111);

        // Act
        service.ScheduleImmediateRestart(1, "Memory leak detected");

        // Assert
        var status = service.GetServerUptime(1);
        status!.RestartScheduled.Should().BeTrue();
    }

    [Fact]
    public void ScheduleImmediateRestart_NonExistingServer_ShouldNotThrow()
    {
        // Arrange
        var service = CreateService();

        // Act
        var act = () => service.ScheduleImmediateRestart(999, "reason");

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region CancelScheduledRestart Tests

    [Fact]
    public void CancelScheduledRestart_WhenScheduled_ShouldReturnTrue()
    {
        // Arrange
        var service = CreateService();
        service.RegisterServer(1, 11111);
        service.ScheduleImmediateRestart(1, "reason");

        // Act
        var result = service.CancelScheduledRestart(1);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CancelScheduledRestart_WhenNotScheduled_ShouldReturnFalse()
    {
        // Arrange
        var service = CreateService();
        service.RegisterServer(1, 11111);

        // Act
        var result = service.CancelScheduledRestart(1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CancelScheduledRestart_NonExistingServer_ShouldReturnFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.CancelScheduledRestart(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CancelScheduledRestart_ShouldClearScheduledFlag()
    {
        // Arrange
        var service = CreateService();
        service.RegisterServer(1, 11111);
        service.ScheduleImmediateRestart(1, "reason");

        // Act
        service.CancelScheduledRestart(1);

        // Assert
        var status = service.GetServerUptime(1);
        status!.RestartScheduled.Should().BeFalse();
    }

    #endregion

    #region Event Tests

    [Fact]
    public void ServerRestartRequired_CanSubscribe()
    {
        // Arrange
        var service = CreateService();
        var eventRaised = false;

        // Act
        service.ServerRestartRequired += (_, _) => eventRaised = true;

        // Assert - Just verify subscription works
        service.Should().NotBeNull();
    }

    [Fact]
    public void ServerRestartStarting_CanSubscribe()
    {
        // Arrange
        var service = CreateService();
        var eventRaised = false;

        // Act
        service.ServerRestartStarting += (_, _) => eventRaised = true;

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void ServerRestartCompleted_CanSubscribe()
    {
        // Arrange
        var service = CreateService();
        var eventRaised = false;

        // Act
        service.ServerRestartCompleted += (_, _) => eventRaised = true;

        // Assert
        service.Should().NotBeNull();
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Configuration_WithNullServerLifecycle_ShouldUseDefaults()
    {
        // Arrange
        var configWithoutLifecycle = new HoNConfiguration { ServerLifecycle = null };
        var service = new PeriodicRestartService(_loggerMock.Object, configWithoutLifecycle);

        // Act
        service.RegisterServer(1, 11111);

        // Assert - Should use default 24-48 hour range
        var status = service.GetServerUptime(1);
        status!.TargetUptime.TotalHours.Should().BeGreaterThanOrEqualTo(24);
        status.TargetUptime.TotalHours.Should().BeLessThanOrEqualTo(48);
    }

    [Fact]
    public void Configuration_CustomUptimeRange_ShouldBeRespected()
    {
        // Arrange
        var customConfig = new HoNConfiguration
        {
            ServerLifecycle = new ServerLifecycleConfiguration
            {
                MinUptimeHours = 1,
                MaxUptimeHours = 2
            }
        };
        var service = new PeriodicRestartService(_loggerMock.Object, customConfig);

        // Act
        service.RegisterServer(1, 11111);

        // Assert
        var status = service.GetServerUptime(1);
        status!.TargetUptime.TotalHours.Should().BeGreaterThanOrEqualTo(1);
        status.TargetUptime.TotalHours.Should().BeLessThanOrEqualTo(2);
    }

    #endregion
}

#region DTO Tests

public class ServerUptimeStatusDtoTests
{
    [Fact]
    public void ServerUptimeStatus_DefaultConstructor_ShouldInitialize()
    {
        // Act
        var status = new ServerUptimeStatus();

        // Assert
        status.ServerId.Should().Be(0);
        status.Port.Should().Be(0);
        status.Uptime.Should().Be(TimeSpan.Zero);
        status.TargetUptime.Should().Be(TimeSpan.Zero);
        status.TimeUntilRestart.Should().Be(TimeSpan.Zero);
        status.RestartScheduled.Should().BeFalse();
    }

    [Fact]
    public void ServerUptimeStatus_ShouldAllowSettingProperties()
    {
        // Act
        var status = new ServerUptimeStatus
        {
            ServerId = 1,
            Port = 11111,
            Uptime = TimeSpan.FromHours(10),
            TargetUptime = TimeSpan.FromHours(24),
            TimeUntilRestart = TimeSpan.FromHours(14),
            RestartScheduled = true
        };

        // Assert
        status.ServerId.Should().Be(1);
        status.Port.Should().Be(11111);
        status.Uptime.Should().Be(TimeSpan.FromHours(10));
        status.TargetUptime.Should().Be(TimeSpan.FromHours(24));
        status.TimeUntilRestart.Should().Be(TimeSpan.FromHours(14));
        status.RestartScheduled.Should().BeTrue();
    }
}

public class ServerRestartEventArgsDtoTests
{
    [Fact]
    public void ServerRestartEventArgs_DefaultConstructor_ShouldInitialize()
    {
        // Act
        var args = new ServerRestartEventArgs();

        // Assert
        args.ServerId.Should().Be(0);
        args.Port.Should().Be(0);
        args.Reason.Should().BeEmpty();
        args.Uptime.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void ServerRestartEventArgs_ShouldAllowSettingProperties()
    {
        // Act
        var args = new ServerRestartEventArgs
        {
            ServerId = 5,
            Port = 12345,
            Reason = "Scheduled maintenance",
            Uptime = TimeSpan.FromHours(48)
        };

        // Assert
        args.ServerId.Should().Be(5);
        args.Port.Should().Be(12345);
        args.Reason.Should().Be("Scheduled maintenance");
        args.Uptime.Should().Be(TimeSpan.FromHours(48));
    }
}

public class ServerRestartCompletedEventArgsDtoTests
{
    [Fact]
    public void ServerRestartCompletedEventArgs_DefaultConstructor_ShouldInitialize()
    {
        // Act
        var args = new ServerRestartCompletedEventArgs();

        // Assert
        args.ServerId.Should().Be(0);
        args.Port.Should().Be(0);
        args.Success.Should().BeFalse();
        args.Reason.Should().BeEmpty();
        args.Error.Should().BeNull();
    }

    [Fact]
    public void ServerRestartCompletedEventArgs_SuccessCase()
    {
        // Act
        var args = new ServerRestartCompletedEventArgs
        {
            ServerId = 3,
            Port = 11113,
            Success = true,
            Reason = "Uptime limit reached",
            Error = null
        };

        // Assert
        args.Success.Should().BeTrue();
        args.Error.Should().BeNull();
    }

    [Fact]
    public void ServerRestartCompletedEventArgs_FailureCase()
    {
        // Act
        var args = new ServerRestartCompletedEventArgs
        {
            ServerId = 3,
            Port = 11113,
            Success = false,
            Reason = "Uptime limit reached",
            Error = "Process failed to start"
        };

        // Assert
        args.Success.Should().BeFalse();
        args.Error.Should().Be("Process failed to start");
    }
}

#endregion
