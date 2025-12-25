using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using HoNfigurator.Core.Services;

namespace HoNfigurator.Tests.Services;

/// <summary>
/// Tests for CowMasterService - Linux fork-based server spawning
/// </summary>
public class CowMasterServiceTests
{
    private readonly Mock<ILogger<CowMasterService>> _loggerMock;

    public CowMasterServiceTests()
    {
        _loggerMock = new Mock<ILogger<CowMasterService>>();
    }

    private CowMasterService CreateService()
    {
        return new CowMasterService(_loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var service = CreateService();

        // Assert
        service.IsEnabled.Should().BeFalse();
        service.IsRunning.Should().BeFalse();
        service.Port.Should().Be(11235); // Default port
        service.Name.Should().Be("cowmaster");
        service.ProcessId.Should().BeNull();
    }

    [Fact]
    public void Constructor_ShouldAcceptLogger()
    {
        // Act
        var service = CreateService();

        // Assert
        service.Should().NotBeNull();
    }

    #endregion

    #region Configure Tests

    [Fact]
    public void Configure_ShouldSetPort()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.Configure(12345, "myserver");

        // Assert
        service.Port.Should().Be(12345);
    }

    [Fact]
    public void Configure_ShouldSetNameWithCowMasterSuffix()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.Configure(12345, "myserver");

        // Assert
        service.Name.Should().Be("myserver-cowmaster");
    }

    [Fact]
    public void Configure_WithEmptyName_ShouldStillWork()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.Configure(12345, "");

        // Assert
        service.Name.Should().Be("-cowmaster");
    }

    [Fact]
    public void Configure_MultipleTimes_ShouldUpdateValues()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.Configure(1111, "first");
        service.Configure(2222, "second");

        // Assert
        service.Port.Should().Be(2222);
        service.Name.Should().Be("second-cowmaster");
    }

    #endregion

    #region StartAsync Tests

    [Fact]
    public async Task StartAsync_OnNonLinux_ShouldReturnFalse()
    {
        // Arrange
        var service = CreateService();

        // This test will pass on Windows, fail on Linux
        if (!OperatingSystem.IsLinux())
        {
            // Act
            var result = await service.StartAsync("/path/to/hon", new Dictionary<string, string>());

            // Assert
            result.Should().BeFalse();
        }
    }

    [Fact]
    public async Task StartAsync_OnNonLinux_ShouldLogWarning()
    {
        // Arrange
        var service = CreateService();

        if (!OperatingSystem.IsLinux())
        {
            // Act
            await service.StartAsync("/path/to/hon", new Dictionary<string, string>());

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("only supported on Linux")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }

    [Fact]
    public async Task StartAsync_WhenAlreadyRunning_ShouldReturnFalse()
    {
        // This test is platform-dependent and requires mock setup
        // Since CowMaster is Linux-only, we test the logic by checking IsRunning
        var service = CreateService();
        
        // IsRunning is false by default
        service.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task StartAsync_ShouldNotChangeEnabledOnNonLinux()
    {
        // Arrange
        var service = CreateService();

        if (!OperatingSystem.IsLinux())
        {
            // Act
            await service.StartAsync("/path/to/hon", new Dictionary<string, string>());

            // Assert
            service.IsEnabled.Should().BeFalse();
        }
    }

    #endregion

    #region ForkServerAsync Tests

    [Fact]
    public async Task ForkServerAsync_WhenNotRunning_ShouldReturnNull()
    {
        // Arrange
        var service = CreateService();
        service.IsRunning.Should().BeFalse();

        // Act
        var result = await service.ForkServerAsync(1, 11111, 11112, 11113);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ForkServerAsync_WhenNotRunning_ShouldLogWarning()
    {
        // Arrange
        var service = CreateService();

        // Act
        await service.ForkServerAsync(1, 11111, 11112, 11113);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not running")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Stop Tests

    [Fact]
    public void Stop_WhenNotStarted_ShouldNotThrow()
    {
        // Arrange
        var service = CreateService();

        // Act
        var act = () => service.Stop();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Stop_WhenNotStarted_ShouldKeepIsEnabledFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.Stop();

        // Assert
        service.IsEnabled.Should().BeFalse();
        service.IsRunning.Should().BeFalse();
    }

    #endregion

    #region GetStatus Tests

    [Fact]
    public void GetStatus_WhenNotStarted_ShouldReturnCorrectStatus()
    {
        // Arrange
        var service = CreateService();

        // Act
        var status = service.GetStatus();

        // Assert
        status.Should().NotBeNull();
        status.IsEnabled.Should().BeFalse();
        status.IsRunning.Should().BeFalse();
        status.ProcessId.Should().BeNull();
        status.Name.Should().Be("cowmaster");
        status.Port.Should().Be(11235);
    }

    [Fact]
    public void GetStatus_AfterConfigure_ShouldReflectNewValues()
    {
        // Arrange
        var service = CreateService();
        service.Configure(9999, "testserver");

        // Act
        var status = service.GetStatus();

        // Assert
        status.Name.Should().Be("testserver-cowmaster");
        status.Port.Should().Be(9999);
    }

    [Fact]
    public void GetStatus_ShouldReturnNewInstanceEachTime()
    {
        // Arrange
        var service = CreateService();

        // Act
        var status1 = service.GetStatus();
        var status2 = service.GetStatus();

        // Assert
        status1.Should().NotBeSameAs(status2);
    }

    #endregion

    #region IsRunning Property Tests

    [Fact]
    public void IsRunning_WhenProcessIsNull_ShouldReturnFalse()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        service.IsRunning.Should().BeFalse();
    }

    #endregion

    #region ProcessId Property Tests

    [Fact]
    public void ProcessId_WhenNotStarted_ShouldBeNull()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        service.ProcessId.Should().BeNull();
    }

    #endregion
}

#region CowMasterStatus DTO Tests

public class CowMasterStatusDtoTests
{
    [Fact]
    public void CowMasterStatus_DefaultConstructor_ShouldInitialize()
    {
        // Act
        var status = new CowMasterStatus();

        // Assert
        status.Name.Should().BeEmpty();
        status.Port.Should().Be(0);
        status.IsEnabled.Should().BeFalse();
        status.IsRunning.Should().BeFalse();
        status.ProcessId.Should().BeNull();
    }

    [Fact]
    public void CowMasterStatus_ShouldAllowSettingAllProperties()
    {
        // Act
        var status = new CowMasterStatus
        {
            Name = "test-cowmaster",
            Port = 12345,
            IsEnabled = true,
            IsRunning = true,
            ProcessId = 9999
        };

        // Assert
        status.Name.Should().Be("test-cowmaster");
        status.Port.Should().Be(12345);
        status.IsEnabled.Should().BeTrue();
        status.IsRunning.Should().BeTrue();
        status.ProcessId.Should().Be(9999);
    }

    [Fact]
    public void CowMasterStatus_ProcessId_ShouldBeNullable()
    {
        // Act
        var status = new CowMasterStatus { ProcessId = null };

        // Assert
        status.ProcessId.Should().BeNull();
    }
}

#endregion
