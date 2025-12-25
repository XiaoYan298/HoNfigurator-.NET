using FluentAssertions;
using HoNfigurator.Core.Models;
using HoNfigurator.GameServer.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HoNfigurator.Tests.Services;

/// <summary>
/// Tests for GameServerManager - HoN game server process management
/// </summary>
public class GameServerManagerTests : IDisposable
{
    private readonly Mock<ILogger<GameServerManager>> _loggerMock;
    private readonly string _tempDir;

    public GameServerManagerTests()
    {
        _loggerMock = new Mock<ILogger<GameServerManager>>();
        _tempDir = Path.Combine(Path.GetTempPath(), $"GameServerManagerTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }
        catch { }
    }

    private GameServerManager CreateService(string? exePath = null, string serverName = "TestServer")
    {
        return new GameServerManager(
            _loggerMock.Object,
            exePath ?? Path.Combine(_tempDir, "hon_server.exe"),
            serverName);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParams_ShouldInitialize()
    {
        // Act
        var service = CreateService();

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldInitializeEmptyInstances()
    {
        // Act
        var service = CreateService();

        // Assert
        service.Instances.Should().NotBeNull();
        service.Instances.Should().BeEmpty();
    }

    #endregion

    #region Instances Tests

    [Fact]
    public void Instances_Initially_ShouldBeEmpty()
    {
        // Arrange
        var service = CreateService();

        // Assert
        service.Instances.Should().BeEmpty();
    }

    [Fact]
    public void Instances_ShouldReturnOrderedList()
    {
        // Arrange
        var service = CreateService();
        service.AddServer(new GameServerInstance { Id = 3 });
        service.AddServer(new GameServerInstance { Id = 1 });
        service.AddServer(new GameServerInstance { Id = 2 });

        // Assert
        service.Instances[0].Id.Should().Be(1);
        service.Instances[1].Id.Should().Be(2);
        service.Instances[2].Id.Should().Be(3);
    }

    #endregion

    #region Connection Status Tests

    [Fact]
    public void MasterServerConnected_Initially_ShouldBeFalse()
    {
        // Arrange
        var service = CreateService();

        // Assert
        service.MasterServerConnected.Should().BeFalse();
    }

    [Fact]
    public void MasterServerConnected_ShouldBeSettable()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.MasterServerConnected = true;

        // Assert
        service.MasterServerConnected.Should().BeTrue();
    }

    [Fact]
    public void ChatServerConnected_Initially_ShouldBeFalse()
    {
        // Arrange
        var service = CreateService();

        // Assert
        service.ChatServerConnected.Should().BeFalse();
    }

    [Fact]
    public void ChatServerConnected_ShouldBeSettable()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.ChatServerConnected = true;

        // Assert
        service.ChatServerConnected.Should().BeTrue();
    }

    [Fact]
    public void MasterServerStatus_ShouldBeSettable()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.MasterServerStatus = "Connected";

        // Assert
        service.MasterServerStatus.Should().Be("Connected");
    }

    [Fact]
    public void ChatServerStatus_ShouldBeSettable()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.ChatServerStatus = "Online";

        // Assert
        service.ChatServerStatus.Should().Be("Online");
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Configuration_Initially_ShouldBeNull()
    {
        // Arrange
        var service = CreateService();

        // Assert
        service.Configuration.Should().BeNull();
    }

    [Fact]
    public void Configuration_ShouldBeSettable()
    {
        // Arrange
        var service = CreateService();
        var config = new HoNConfiguration();

        // Act
        service.Configuration = config;

        // Assert
        service.Configuration.Should().Be(config);
    }

    #endregion

    #region Initialize Tests

    [Fact]
    public void Initialize_ShouldCreateServerInstances()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.Initialize(3, 11000, 11200);

        // Assert
        service.Instances.Should().HaveCount(3);
    }

    [Fact]
    public void Initialize_ShouldAssignCorrectIds()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.Initialize(3, 11000, 11200);

        // Assert
        service.Instances[0].Id.Should().Be(1);
        service.Instances[1].Id.Should().Be(2);
        service.Instances[2].Id.Should().Be(3);
    }

    [Fact]
    public void Initialize_ShouldAssignCorrectPorts()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.Initialize(2, 11000, 11200);

        // Assert
        service.Instances[0].Port.Should().Be(11000);
        service.Instances[0].VoicePort.Should().Be(11200);
        service.Instances[1].Port.Should().Be(11001);
        service.Instances[1].VoicePort.Should().Be(11201);
    }

    [Fact]
    public void Initialize_WithZeroServers_ShouldNotCreateAny()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.Initialize(0, 11000, 11200);

        // Assert
        service.Instances.Should().BeEmpty();
    }

    #endregion

    #region AddServer Tests

    [Fact]
    public void AddServer_ShouldAddInstance()
    {
        // Arrange
        var service = CreateService();
        var instance = new GameServerInstance { Id = 1 };

        // Act
        service.AddServer(instance);

        // Assert
        service.Instances.Should().HaveCount(1);
        service.Instances[0].Id.Should().Be(1);
    }

    [Fact]
    public void AddServer_WithDuplicateId_ShouldNotReplace()
    {
        // Arrange - TryAdd does not replace existing keys
        var service = CreateService();
        var instance1 = new GameServerInstance { Id = 1, Port = 11000 };
        var instance2 = new GameServerInstance { Id = 1, Port = 12000 };
        
        service.AddServer(instance1);

        // Act
        service.AddServer(instance2);

        // Assert - Original remains, duplicate is ignored
        service.Instances.Should().HaveCount(1);
        service.Instances[0].Port.Should().Be(11000);
    }

    #endregion

    #region AddNewServer Tests

    [Fact]
    public void AddNewServer_ShouldReturnNewId()
    {
        // Arrange
        var service = CreateService();

        // Act
        var id = service.AddNewServer();

        // Assert
        id.Should().Be(1);
        service.Instances.Should().HaveCount(1);
    }

    [Fact]
    public void AddNewServer_ShouldIncrementId()
    {
        // Arrange
        var service = CreateService();
        service.AddNewServer();

        // Act
        var id = service.AddNewServer();

        // Assert
        id.Should().Be(2);
    }

    [Fact]
    public void AddNewServer_WithGap_ShouldUseMaxPlusOne()
    {
        // Arrange - AddNewServer uses max+1, doesn't fill gaps
        var service = CreateService();
        service.AddServer(new GameServerInstance { Id = 1 });
        service.AddServer(new GameServerInstance { Id = 3 });

        // Act
        var id = service.AddNewServer();

        // Assert - Uses max(3)+1 = 4, not gap filling
        id.Should().Be(4);
    }

    #endregion

    #region RemoveServer Tests

    [Fact]
    public void RemoveServer_WithExistingId_ShouldReturnTrue()
    {
        // Arrange
        var service = CreateService();
        service.AddServer(new GameServerInstance { Id = 1 });

        // Act
        var result = service.RemoveServer(1);

        // Assert
        result.Should().BeTrue();
        service.Instances.Should().BeEmpty();
    }

    [Fact]
    public void RemoveServer_WithNonExistingId_ShouldReturnFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.RemoveServer(999);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ClearServers Tests

    [Fact]
    public void ClearServers_ShouldRemoveAll()
    {
        // Arrange
        var service = CreateService();
        service.Initialize(3, 11000, 11200);

        // Act
        service.ClearServers();

        // Assert
        service.Instances.Should().BeEmpty();
    }

    #endregion

    #region GetStatus Tests

    [Fact]
    public void GetStatus_ShouldReturnStatusResponse()
    {
        // Arrange
        var service = CreateService();
        service.Initialize(2, 11000, 11200);

        // Act
        var status = service.GetStatus();

        // Assert
        status.Should().NotBeNull();
        status.TotalServers.Should().Be(2);
    }

    [Fact]
    public void GetStatus_ShouldIncludeConnectionStatus()
    {
        // Arrange
        var service = CreateService();
        service.MasterServerConnected = true;
        service.ChatServerConnected = true;

        // Act
        var status = service.GetStatus();

        // Assert
        status.MasterServerConnected.Should().BeTrue();
        status.ChatServerConnected.Should().BeTrue();
    }

    #endregion

    #region SetListener Tests

    [Fact]
    public void SetListener_ShouldNotThrow()
    {
        // Arrange
        var service = CreateService();
        var listenerMock = new Mock<IGameServerListener>();

        // Act
        var act = () => service.SetListener(listenerMock.Object);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region StartServerAsync Tests

    [Fact]
    public async Task StartServerAsync_WithNonExistingServer_ShouldReturnNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.StartServerAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task StartServerAsync_WithExistingServer_ShouldNotThrow()
    {
        // Arrange
        var service = CreateService();
        service.Initialize(1, 11000, 11200);

        // Act
        var act = async () => await service.StartServerAsync(1);

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region StopServerAsync Tests

    [Fact]
    public async Task StopServerAsync_WithNonExistingServer_ShouldReturnFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.StopServerAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task StopServerAsync_WithGracefulFlag_ShouldNotThrow()
    {
        // Arrange
        var service = CreateService();
        service.Initialize(1, 11000, 11200);

        // Act
        var act = async () => await service.StopServerAsync(1, graceful: true);

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region RestartServerAsync Tests

    [Fact]
    public async Task RestartServerAsync_WithNonExistingServer_ShouldReturnFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.RestartServerAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region StartAllServersAsync Tests

    [Fact]
    public async Task StartAllServersAsync_WithNoServers_ShouldNotThrow()
    {
        // Arrange
        var service = CreateService();

        // Act
        var act = async () => await service.StartAllServersAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region StopAllServersAsync Tests

    [Fact]
    public async Task StopAllServersAsync_WithNoServers_ShouldNotThrow()
    {
        // Arrange
        var service = CreateService();

        // Act
        var act = async () => await service.StopAllServersAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region SendMessageToServerAsync Tests

    [Fact]
    public async Task SendMessageToServerAsync_WithNoListener_ShouldNotThrow()
    {
        // Arrange
        var service = CreateService();
        service.Initialize(1, 11000, 11200);

        // Act
        var act = async () => await service.SendMessageToServerAsync(1, "Test message");

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region SendMessageToAllServersAsync Tests

    [Fact]
    public async Task SendMessageToAllServersAsync_WithNoListener_ShouldNotThrow()
    {
        // Arrange
        var service = CreateService();
        service.Initialize(2, 11000, 11200);

        // Act
        var act = async () => await service.SendMessageToAllServersAsync("Broadcast message");

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region UpdateProcessStats Tests

    [Fact]
    public void UpdateProcessStats_WithNoProcesses_ShouldNotThrow()
    {
        // Arrange
        var service = CreateService();
        service.Initialize(1, 11000, 11200);

        // Act
        var act = () => service.UpdateProcessStats();

        // Assert
        act.Should().NotThrow();
    }

    #endregion
}

/// <summary>
/// Tests for IGameServerManager interface
/// </summary>
public class GameServerManagerInterfaceTests
{
    [Fact]
    public void GameServerManager_ShouldImplementInterface()
    {
        // Assert
        typeof(GameServerManager).Should().Implement<IGameServerManager>();
    }

    [Fact]
    public void IGameServerManager_ShouldHaveRequiredProperties()
    {
        // Assert
        typeof(IGameServerManager).GetProperty("Instances").Should().NotBeNull();
        typeof(IGameServerManager).GetProperty("Configuration").Should().NotBeNull();
        typeof(IGameServerManager).GetProperty("MasterServerConnected").Should().NotBeNull();
        typeof(IGameServerManager).GetProperty("ChatServerConnected").Should().NotBeNull();
    }

    [Fact]
    public void IGameServerManager_ShouldHaveRequiredMethods()
    {
        // Assert
        typeof(IGameServerManager).GetMethod("StartServerAsync").Should().NotBeNull();
        typeof(IGameServerManager).GetMethod("StopServerAsync").Should().NotBeNull();
        typeof(IGameServerManager).GetMethod("RestartServerAsync").Should().NotBeNull();
        typeof(IGameServerManager).GetMethod("GetStatus").Should().NotBeNull();
        typeof(IGameServerManager).GetMethod("Initialize").Should().NotBeNull();
    }
}
