using FluentAssertions;
using HoNfigurator.Api.Setup;
using HoNfigurator.Core.Models;

namespace HoNfigurator.Tests.Api.Setup;

/// <summary>
/// Tests for DependencyChecker
/// </summary>
public class DependencyCheckerTests
{
    #region DependencyStatus Tests

    [Fact]
    public void DependencyStatus_DefaultValues_ShouldBeFalseOrNull()
    {
        // Arrange & Act
        var status = new DependencyStatus();

        // Assert
        status.HonInstalled.Should().BeFalse();
        status.HonPath.Should().BeNull();
        status.ProxyInstalled.Should().BeFalse();
        status.ProxyPath.Should().BeNull();
        status.ProxyEnabled.Should().BeFalse();
        status.ProxyDownloadUrl.Should().BeNull();
    }

    [Fact]
    public void DependencyStatus_AllSatisfied_WhenHonInstalledAndProxyDisabled()
    {
        // Arrange
        var status = new DependencyStatus
        {
            HonInstalled = true,
            ProxyEnabled = false
        };

        // Act & Assert
        status.AllSatisfied.Should().BeTrue();
    }

    [Fact]
    public void DependencyStatus_AllSatisfied_WhenHonInstalledAndProxyEnabledAndInstalled()
    {
        // Arrange
        var status = new DependencyStatus
        {
            HonInstalled = true,
            ProxyEnabled = true,
            ProxyInstalled = true
        };

        // Act & Assert
        status.AllSatisfied.Should().BeTrue();
    }

    [Fact]
    public void DependencyStatus_NotSatisfied_WhenHonNotInstalled()
    {
        // Arrange
        var status = new DependencyStatus
        {
            HonInstalled = false,
            ProxyEnabled = false
        };

        // Act & Assert
        status.AllSatisfied.Should().BeFalse();
    }

    [Fact]
    public void DependencyStatus_NotSatisfied_WhenProxyEnabledButNotInstalled()
    {
        // Arrange
        var status = new DependencyStatus
        {
            HonInstalled = true,
            ProxyEnabled = true,
            ProxyInstalled = false
        };

        // Act & Assert
        status.AllSatisfied.Should().BeFalse();
    }

    [Fact]
    public void DependencyStatus_CanSetAllProperties()
    {
        // Arrange & Act
        var status = new DependencyStatus
        {
            HonInstalled = true,
            HonPath = "C:\\Games\\HoN",
            ProxyInstalled = true,
            ProxyPath = "C:\\Games\\HoN\\proxy.exe",
            ProxyEnabled = true,
            ProxyDownloadUrl = "https://github.com/example/proxy"
        };

        // Assert
        status.HonInstalled.Should().BeTrue();
        status.HonPath.Should().Be("C:\\Games\\HoN");
        status.ProxyInstalled.Should().BeTrue();
        status.ProxyPath.Should().Be("C:\\Games\\HoN\\proxy.exe");
        status.ProxyEnabled.Should().BeTrue();
        status.ProxyDownloadUrl.Should().Be("https://github.com/example/proxy");
    }

    #endregion

    #region DependencyChecker Constructor Tests

    [Fact]
    public void Constructor_WithValidConfig_ShouldNotThrow()
    {
        // Arrange
        var config = new HoNConfiguration();

        // Act
        var checker = new DependencyChecker(config);

        // Assert
        checker.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithConfigContainingHonData_ShouldNotThrow()
    {
        // Arrange
        var config = new HoNConfiguration
        {
            HonData = new HoNData
            {
                HonInstallDirectory = "C:\\TestHoN",
                EnableProxy = true
            }
        };

        // Act
        var checker = new DependencyChecker(config);

        // Assert
        checker.Should().NotBeNull();
    }

    #endregion

    #region GetDependencyStatus Tests

    [Fact]
    public void GetDependencyStatus_WithEmptyConfig_ShouldReturnDefaultStatus()
    {
        // Arrange
        var config = new HoNConfiguration();
        var checker = new DependencyChecker(config);

        // Act
        var status = checker.GetDependencyStatus();

        // Assert
        status.Should().NotBeNull();
        status.HonInstalled.Should().BeFalse();
        status.ProxyInstalled.Should().BeFalse();
    }

    [Fact]
    public void GetDependencyStatus_WithNullHonData_ShouldReturnDefaultStatus()
    {
        // Arrange
        var config = new HoNConfiguration { HonData = null! };
        var checker = new DependencyChecker(config);

        // Act
        var status = checker.GetDependencyStatus();

        // Assert
        status.Should().NotBeNull();
        status.HonInstalled.Should().BeFalse();
    }

    [Fact]
    public void GetDependencyStatus_WithNonExistentInstallDir_ShouldReturnNotInstalled()
    {
        // Arrange
        var config = new HoNConfiguration
        {
            HonData = new HoNData
            {
                HonInstallDirectory = "C:\\NonExistentPath12345_" + Guid.NewGuid().ToString()
            }
        };
        var checker = new DependencyChecker(config);

        // Act
        var status = checker.GetDependencyStatus();

        // Assert
        status.HonInstalled.Should().BeFalse();
        // HonPath is only set when directory exists AND it contains HoN exe
        // For non-existent directory, HonPath should be null
        // Note: If path somehow exists, test will still pass as HonInstalled is false
    }

    [Fact]
    public void GetDependencyStatus_WithProxyEnabled_ShouldSetProxyEnabled()
    {
        // Arrange
        var config = new HoNConfiguration
        {
            HonData = new HoNData
            {
                EnableProxy = true
            }
        };
        var checker = new DependencyChecker(config);

        // Act
        var status = checker.GetDependencyStatus();

        // Assert
        status.ProxyEnabled.Should().BeTrue();
    }

    [Fact]
    public void GetDependencyStatus_WithProxyDisabled_ShouldSetProxyDisabled()
    {
        // Arrange
        var config = new HoNConfiguration
        {
            HonData = new HoNData
            {
                EnableProxy = false
            }
        };
        var checker = new DependencyChecker(config);

        // Act
        var status = checker.GetDependencyStatus();

        // Assert
        status.ProxyEnabled.Should().BeFalse();
    }

    [Fact]
    public void GetDependencyStatus_ShouldAlwaysReturnProxyDownloadUrl()
    {
        // Arrange
        var config = new HoNConfiguration();
        var checker = new DependencyChecker(config);

        // Act
        var status = checker.GetDependencyStatus();

        // Assert
        status.ProxyDownloadUrl.Should().NotBeNullOrEmpty();
        status.ProxyDownloadUrl.Should().Contain("github");
    }

    [Fact]
    public void GetDependencyStatus_WithEmptyInstallDirectory_ShouldNotSetProxyPath()
    {
        // Arrange
        var config = new HoNConfiguration
        {
            HonData = new HoNData
            {
                HonInstallDirectory = ""
            }
        };
        var checker = new DependencyChecker(config);

        // Act
        var status = checker.GetDependencyStatus();

        // Assert
        status.ProxyPath.Should().BeNull();
    }

    [Fact]
    public void GetDependencyStatus_WithInstallDirectory_ShouldSetProxyPath()
    {
        // Arrange
        var config = new HoNConfiguration
        {
            HonData = new HoNData
            {
                HonInstallDirectory = "C:\\Games\\HoN"
            }
        };
        var checker = new DependencyChecker(config);

        // Act
        var status = checker.GetDependencyStatus();

        // Assert
        // ProxyPath is set even if directory doesn't exist (just combines path)
        status.ProxyPath.Should().Contain("proxy.exe");
    }

    #endregion

    #region CheckAllDependenciesAsync Tests

    [Fact]
    public async Task CheckAllDependenciesAsync_WithEmptyConfig_ShouldReturnFalse()
    {
        // Arrange
        var config = new HoNConfiguration();
        var checker = new DependencyChecker(config);

        // Act
        var result = await checker.CheckAllDependenciesAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CheckAllDependenciesAsync_WithNonExistentDirectory_ShouldReturnFalse()
    {
        // Arrange
        var config = new HoNConfiguration
        {
            HonData = new HoNData
            {
                HonInstallDirectory = "C:\\NonExistentPath12345"
            }
        };
        var checker = new DependencyChecker(config);

        // Act
        var result = await checker.CheckAllDependenciesAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CheckAllDependenciesAsync_ShouldBeAsyncCompatible()
    {
        // Arrange
        var config = new HoNConfiguration();
        var checker = new DependencyChecker(config);

        // Act
        var task = checker.CheckAllDependenciesAsync();

        // Assert
        task.Should().NotBeNull();
        await task; // Should complete without error
    }

    #endregion

    #region DependencyStatus Boundary Tests

    [Fact]
    public void DependencyStatus_AllSatisfied_EdgeCase_AllFalse()
    {
        // Arrange
        var status = new DependencyStatus
        {
            HonInstalled = false,
            ProxyEnabled = false,
            ProxyInstalled = false
        };

        // Act & Assert
        // HonInstalled must be true for AllSatisfied
        status.AllSatisfied.Should().BeFalse();
    }

    [Fact]
    public void DependencyStatus_AllSatisfied_EdgeCase_OnlyProxyInstalled()
    {
        // Arrange
        var status = new DependencyStatus
        {
            HonInstalled = false,
            ProxyEnabled = true,
            ProxyInstalled = true
        };

        // Act & Assert
        // HonInstalled must be true for AllSatisfied
        status.AllSatisfied.Should().BeFalse();
    }

    [Theory]
    [InlineData(true, true, true, true)]   // All enabled and installed
    [InlineData(true, false, false, true)] // Hon installed, proxy disabled
    [InlineData(true, true, false, false)] // Hon installed, proxy enabled but not installed
    [InlineData(false, false, false, false)] // Nothing installed
    [InlineData(false, true, true, false)] // Proxy installed but Hon not installed
    public void DependencyStatus_AllSatisfied_VariousCombinations(
        bool honInstalled, bool proxyEnabled, bool proxyInstalled, bool expectedSatisfied)
    {
        // Arrange
        var status = new DependencyStatus
        {
            HonInstalled = honInstalled,
            ProxyEnabled = proxyEnabled,
            ProxyInstalled = proxyInstalled
        };

        // Act & Assert
        status.AllSatisfied.Should().Be(expectedSatisfied);
    }

    #endregion
}
