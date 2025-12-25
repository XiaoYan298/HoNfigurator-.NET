using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using HoNfigurator.Core.Models;
using HoNfigurator.Core.Services;

namespace HoNfigurator.Tests.Services;

/// <summary>
/// Tests for ServerScalingService - Dynamic server scaling operations
/// </summary>
public class ServerScalingServiceTests
{
    private readonly Mock<ILogger<ServerScalingService>> _loggerMock;
    private readonly HoNConfiguration _config;

    public ServerScalingServiceTests()
    {
        _loggerMock = new Mock<ILogger<ServerScalingService>>();
        _config = new HoNConfiguration
        {
            HonData = new HoNData { TotalServers = 10 },
            ApplicationData = new ApplicationData
            {
                AutoScaling = new AutoScalingSettings
                {
                    Enabled = true,
                    MinServers = 1,
                    MaxServers = 10,
                    MinReadyServers = 2
                }
            }
        };
    }

    private ServerScalingService CreateService()
    {
        return new ServerScalingService(_loggerMock.Object, _config);
    }

    private Mock<IServerScalingProvider> CreateProviderMock(List<GameServerInstance>? instances = null)
    {
        var mock = new Mock<IServerScalingProvider>();
        var instanceList = instances ?? new List<GameServerInstance>();
        mock.Setup(x => x.Instances).Returns(instanceList);
        return mock;
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldInitialize()
    {
        // Act
        var service = CreateService();

        // Assert
        service.Should().NotBeNull();
    }

    #endregion

    #region SetServerProvider Tests

    [Fact]
    public void SetServerProvider_ShouldAllowSettingProvider()
    {
        // Arrange
        var service = CreateService();
        var providerMock = CreateProviderMock();

        // Act
        var act = () => service.SetServerProvider(providerMock.Object);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region GetStatus Tests

    [Fact]
    public void GetStatus_WhenProviderNotSet_ShouldReturnMessageStatus()
    {
        // Arrange
        var service = CreateService();

        // Act
        var status = service.GetStatus();

        // Assert
        status.Message.Should().Be("Server provider not configured");
    }

    [Fact]
    public void GetStatus_WithProvider_ShouldReturnCorrectCounts()
    {
        // Arrange
        var service = CreateService();
        var instances = new List<GameServerInstance>
        {
            new() { Id = 1, Status = ServerStatus.Ready },
            new() { Id = 2, Status = ServerStatus.Occupied },
            new() { Id = 3, Status = ServerStatus.Idle },
            new() { Id = 4, Status = ServerStatus.Offline }
        };
        var providerMock = CreateProviderMock(instances);
        service.SetServerProvider(providerMock.Object);

        // Act
        var status = service.GetStatus();

        // Assert
        status.CurrentServers.Should().Be(4);
        status.RunningServers.Should().Be(3); // Ready, Occupied, Idle
        status.IdleServers.Should().Be(2); // Idle, Ready
        status.OccupiedServers.Should().Be(1);
        status.OfflineServers.Should().Be(1);
    }

    [Fact]
    public void GetStatus_ShouldShowMaxServersFromConfig()
    {
        // Arrange
        var service = CreateService();
        var providerMock = CreateProviderMock();
        service.SetServerProvider(providerMock.Object);

        // Act
        var status = service.GetStatus();

        // Assert
        status.MaxServers.Should().Be(10);
    }

    [Fact]
    public void GetStatus_CanScaleUp_WhenBelowMax()
    {
        // Arrange
        var service = CreateService();
        var instances = new List<GameServerInstance>
        {
            new() { Id = 1, Status = ServerStatus.Ready }
        };
        var providerMock = CreateProviderMock(instances);
        service.SetServerProvider(providerMock.Object);

        // Act
        var status = service.GetStatus();

        // Assert
        status.CanScaleUp.Should().BeTrue();
    }

    [Fact]
    public void GetStatus_CannotScaleUp_WhenAtMax()
    {
        // Arrange
        var config = new HoNConfiguration { HonData = new HoNData { TotalServers = 2 } };
        var service = new ServerScalingService(_loggerMock.Object, config);
        var instances = new List<GameServerInstance>
        {
            new() { Id = 1, Status = ServerStatus.Ready },
            new() { Id = 2, Status = ServerStatus.Ready }
        };
        var providerMock = CreateProviderMock(instances);
        service.SetServerProvider(providerMock.Object);

        // Act
        var status = service.GetStatus();

        // Assert
        status.CanScaleUp.Should().BeFalse();
    }

    [Fact]
    public void GetStatus_CanScaleDown_WhenHasRunningServers()
    {
        // Arrange
        var service = CreateService();
        var instances = new List<GameServerInstance>
        {
            new() { Id = 1, Status = ServerStatus.Ready }
        };
        var providerMock = CreateProviderMock(instances);
        service.SetServerProvider(providerMock.Object);

        // Act
        var status = service.GetStatus();

        // Assert
        status.CanScaleDown.Should().BeTrue();
    }

    [Fact]
    public void GetStatus_AutoScalingEnabled_FromConfig()
    {
        // Arrange
        var service = CreateService();
        var providerMock = CreateProviderMock();
        service.SetServerProvider(providerMock.Object);

        // Act
        var status = service.GetStatus();

        // Assert
        status.AutoScalingEnabled.Should().BeTrue();
    }

    #endregion

    #region AddServersAsync Tests

    [Fact]
    public async Task AddServersAsync_WithZeroCount_ShouldReturnError()
    {
        // Arrange
        var service = CreateService();
        var providerMock = CreateProviderMock();
        service.SetServerProvider(providerMock.Object);

        // Act
        var result = await service.AddServersAsync(0);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("positive");
    }

    [Fact]
    public async Task AddServersAsync_WithNegativeCount_ShouldReturnError()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.AddServersAsync(-1);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("positive");
    }

    [Fact]
    public async Task AddServersAsync_WhenProviderNotSet_ShouldReturnError()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.AddServersAsync(1);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("provider not configured");
    }

    [Fact]
    public async Task AddServersAsync_WhenAtMax_ShouldReturnError()
    {
        // Arrange
        var config = new HoNConfiguration { HonData = new HoNData { TotalServers = 2 } };
        var service = new ServerScalingService(_loggerMock.Object, config);
        var instances = new List<GameServerInstance>
        {
            new() { Id = 1 },
            new() { Id = 2 }
        };
        var providerMock = CreateProviderMock(instances);
        service.SetServerProvider(providerMock.Object);

        // Act
        var result = await service.AddServersAsync(1);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("maximum");
    }

    [Fact]
    public async Task AddServersAsync_ShouldCallProviderAddAndStart()
    {
        // Arrange
        var service = CreateService();
        var instances = new List<GameServerInstance>();
        var providerMock = new Mock<IServerScalingProvider>();
        providerMock.Setup(x => x.Instances).Returns(instances);
        providerMock.Setup(x => x.AddNewServer()).Returns(1);
        providerMock.Setup(x => x.StartServerAsync(1))
            .ReturnsAsync(new GameServerInstance { Id = 1, Port = 11111 });
        service.SetServerProvider(providerMock.Object);

        // Act
        var result = await service.AddServersAsync(1);

        // Assert
        providerMock.Verify(x => x.AddNewServer(), Times.Once);
        providerMock.Verify(x => x.StartServerAsync(1), Times.Once);
    }

    [Fact]
    public async Task AddServersAsync_ShouldLimitToMaxServers()
    {
        // Arrange
        var config = new HoNConfiguration { HonData = new HoNData { TotalServers = 3 } };
        var service = new ServerScalingService(_loggerMock.Object, config);
        var instances = new List<GameServerInstance> { new() { Id = 1 } };
        var providerMock = new Mock<IServerScalingProvider>();
        providerMock.Setup(x => x.Instances).Returns(instances);
        var nextId = 2;
        providerMock.Setup(x => x.AddNewServer()).Returns(() => nextId++);
        providerMock.Setup(x => x.StartServerAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => new GameServerInstance { Id = id });
        service.SetServerProvider(providerMock.Object);

        // Act - Try to add 5, but max is 3 (already have 1)
        var result = await service.AddServersAsync(5);

        // Assert - Should only add 2
        providerMock.Verify(x => x.AddNewServer(), Times.Exactly(2));
    }

    #endregion

    #region RemoveServersAsync Tests

    [Fact]
    public async Task RemoveServersAsync_WithZeroCount_ShouldReturnError()
    {
        // Arrange
        var service = CreateService();
        var providerMock = CreateProviderMock();
        service.SetServerProvider(providerMock.Object);

        // Act
        var result = await service.RemoveServersAsync(0);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("positive");
    }

    [Fact]
    public async Task RemoveServersAsync_WhenProviderNotSet_ShouldReturnError()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.RemoveServersAsync(1);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("provider not configured");
    }

    [Fact]
    public async Task RemoveServersAsync_WhenNoEligibleServers_ShouldReturnError()
    {
        // Arrange
        var service = CreateService();
        var instances = new List<GameServerInstance>
        {
            new() { Id = 1, Status = ServerStatus.Occupied, NumClients = 10 }
        };
        var providerMock = CreateProviderMock(instances);
        service.SetServerProvider(providerMock.Object);

        // Act
        var result = await service.RemoveServersAsync(1, forceRemoveOccupied: false);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("occupied");
    }

    [Fact]
    public async Task RemoveServersAsync_ShouldPreferIdleServers()
    {
        // Arrange
        var service = CreateService();
        var instances = new List<GameServerInstance>
        {
            new() { Id = 1, Status = ServerStatus.Idle },
            new() { Id = 2, Status = ServerStatus.Ready },
            new() { Id = 3, Status = ServerStatus.Occupied }
        };
        var providerMock = new Mock<IServerScalingProvider>();
        providerMock.Setup(x => x.Instances).Returns(instances);
        providerMock.Setup(x => x.StopServerAsync(It.IsAny<int>(), true)).ReturnsAsync(true);
        service.SetServerProvider(providerMock.Object);

        // Act
        await service.RemoveServersAsync(1);

        // Assert - Should prefer idle over ready over occupied
        providerMock.Verify(x => x.StopServerAsync(It.IsIn(1, 2), true), Times.Once);
    }

    [Fact]
    public async Task RemoveServersAsync_WithForce_ShouldRemoveOccupied()
    {
        // Arrange
        var service = CreateService();
        var instances = new List<GameServerInstance>
        {
            new() { Id = 1, Status = ServerStatus.Occupied, NumClients = 5 }
        };
        var providerMock = new Mock<IServerScalingProvider>();
        providerMock.Setup(x => x.Instances).Returns(instances);
        providerMock.Setup(x => x.StopServerAsync(1, true)).ReturnsAsync(true);
        service.SetServerProvider(providerMock.Object);

        // Act
        var result = await service.RemoveServersAsync(1, forceRemoveOccupied: true);

        // Assert
        providerMock.Verify(x => x.StopServerAsync(1, true), Times.Once);
    }

    #endregion

    #region ScaleToAsync Tests

    [Fact]
    public async Task ScaleToAsync_WhenProviderNotSet_ShouldReturnError()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.ScaleToAsync(5);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("provider not configured");
    }

    [Fact]
    public async Task ScaleToAsync_WhenAlreadyAtTarget_ShouldReturnSuccess()
    {
        // Arrange
        var service = CreateService();
        var instances = new List<GameServerInstance>
        {
            new() { Id = 1 },
            new() { Id = 2 }
        };
        var providerMock = CreateProviderMock(instances);
        service.SetServerProvider(providerMock.Object);

        // Act
        var result = await service.ScaleToAsync(2);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("target");
    }

    [Fact]
    public async Task ScaleToAsync_WhenNeedMoreServers_ShouldCallAdd()
    {
        // Arrange
        var service = CreateService();
        var instances = new List<GameServerInstance> { new() { Id = 1 } };
        var providerMock = new Mock<IServerScalingProvider>();
        providerMock.Setup(x => x.Instances).Returns(instances);
        providerMock.Setup(x => x.AddNewServer()).Returns(2);
        providerMock.Setup(x => x.StartServerAsync(2))
            .ReturnsAsync(new GameServerInstance { Id = 2 });
        service.SetServerProvider(providerMock.Object);

        // Act
        await service.ScaleToAsync(2);

        // Assert
        providerMock.Verify(x => x.AddNewServer(), Times.Once);
    }

    [Fact]
    public async Task ScaleToAsync_WhenNeedFewerServers_ShouldCallRemove()
    {
        // Arrange
        var service = CreateService();
        var instances = new List<GameServerInstance>
        {
            new() { Id = 1, Status = ServerStatus.Idle },
            new() { Id = 2, Status = ServerStatus.Idle }
        };
        var providerMock = new Mock<IServerScalingProvider>();
        providerMock.Setup(x => x.Instances).Returns(instances);
        providerMock.Setup(x => x.StopServerAsync(It.IsAny<int>(), true)).ReturnsAsync(true);
        service.SetServerProvider(providerMock.Object);

        // Act
        await service.ScaleToAsync(1);

        // Assert
        providerMock.Verify(x => x.StopServerAsync(It.IsAny<int>(), true), Times.Once);
    }

    #endregion

    #region AutoBalanceAsync Tests

    [Fact]
    public async Task AutoBalanceAsync_WhenProviderNotSet_ShouldReturnError()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.AutoBalanceAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("provider not configured");
    }

    [Fact]
    public async Task AutoBalanceAsync_WhenAutoScalingDisabled_ShouldReturnError()
    {
        // Arrange
        var config = new HoNConfiguration
        {
            ApplicationData = new ApplicationData
            {
                AutoScaling = new AutoScalingSettings { Enabled = false }
            }
        };
        var service = new ServerScalingService(_loggerMock.Object, config);
        var providerMock = CreateProviderMock();
        service.SetServerProvider(providerMock.Object);

        // Act
        var result = await service.AutoBalanceAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not enabled");
    }

    [Fact]
    public async Task AutoBalanceAsync_WhenBalanced_ShouldReturnNoAction()
    {
        // Arrange
        var service = CreateService();
        var instances = new List<GameServerInstance>
        {
            new() { Id = 1, Status = ServerStatus.Idle },
            new() { Id = 2, Status = ServerStatus.Ready }
        };
        var providerMock = CreateProviderMock(instances);
        service.SetServerProvider(providerMock.Object);

        // Act
        var result = await service.AutoBalanceAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("No scaling action needed");
    }

    [Fact]
    public async Task AutoBalanceAsync_WhenTooFewIdle_ShouldScaleUp()
    {
        // Arrange
        var service = CreateService();
        var instances = new List<GameServerInstance>
        {
            new() { Id = 1, Status = ServerStatus.Occupied }
        };
        var providerMock = new Mock<IServerScalingProvider>();
        providerMock.Setup(x => x.Instances).Returns(instances);
        providerMock.Setup(x => x.AddNewServer()).Returns(2);
        providerMock.Setup(x => x.StartServerAsync(2))
            .ReturnsAsync(new GameServerInstance { Id = 2 });
        service.SetServerProvider(providerMock.Object);

        // Act
        await service.AutoBalanceAsync();

        // Assert
        providerMock.Verify(x => x.AddNewServer(), Times.AtLeastOnce);
    }

    #endregion
}

#region DTO Tests

public class ScalingStatusDtoTests
{
    [Fact]
    public void ScalingStatus_DefaultConstructor_ShouldInitialize()
    {
        // Act
        var status = new ScalingStatus();

        // Assert
        status.CurrentServers.Should().Be(0);
        status.RunningServers.Should().Be(0);
        status.IdleServers.Should().Be(0);
        status.OccupiedServers.Should().Be(0);
        status.OfflineServers.Should().Be(0);
        status.MaxServers.Should().Be(0);
        status.CanScaleUp.Should().BeFalse();
        status.CanScaleDown.Should().BeFalse();
        status.AutoScalingEnabled.Should().BeFalse();
        status.Message.Should().BeNull();
    }

    [Fact]
    public void ScalingStatus_ShouldAllowSettingProperties()
    {
        // Act
        var status = new ScalingStatus
        {
            CurrentServers = 5,
            RunningServers = 4,
            IdleServers = 2,
            OccupiedServers = 2,
            OfflineServers = 1,
            MaxServers = 10,
            CanScaleUp = true,
            CanScaleDown = true,
            AutoScalingEnabled = true,
            Message = "Test message"
        };

        // Assert
        status.CurrentServers.Should().Be(5);
        status.MaxServers.Should().Be(10);
        status.Message.Should().Be("Test message");
    }
}

public class ScalingResultDtoTests
{
    [Fact]
    public void ScalingResult_DefaultConstructor_ShouldInitialize()
    {
        // Act
        var result = new ScalingResult();

        // Assert
        result.Success.Should().BeFalse();
        result.PreviousCount.Should().Be(0);
        result.CurrentCount.Should().Be(0);
        result.Added.Should().Be(0);
        result.Removed.Should().Be(0);
        result.Message.Should().BeNull();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void ScalingResult_SuccessfulScaleUp()
    {
        // Act
        var result = new ScalingResult
        {
            Success = true,
            PreviousCount = 2,
            CurrentCount = 5,
            Added = 3,
            Removed = 0,
            Message = "Added 3 servers"
        };

        // Assert
        result.Success.Should().BeTrue();
        result.Added.Should().Be(3);
        result.Removed.Should().Be(0);
    }

    [Fact]
    public void ScalingResult_SuccessfulScaleDown()
    {
        // Act
        var result = new ScalingResult
        {
            Success = true,
            PreviousCount = 5,
            CurrentCount = 3,
            Added = 0,
            Removed = 2,
            Message = "Removed 2 servers"
        };

        // Assert
        result.Success.Should().BeTrue();
        result.Added.Should().Be(0);
        result.Removed.Should().Be(2);
    }

    [Fact]
    public void ScalingResult_FailedOperation()
    {
        // Act
        var result = new ScalingResult
        {
            Success = false,
            PreviousCount = 5,
            CurrentCount = 5,
            Error = "Failed to add server: Connection refused"
        };

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }
}

#endregion
