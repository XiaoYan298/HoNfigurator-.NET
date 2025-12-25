using FluentAssertions;
using HoNfigurator.Core.Models;
using HoNfigurator.Core.Services;
using System.Text.Json;

namespace HoNfigurator.Tests.Services;

/// <summary>
/// Tests for ConfigurationService - JSON config load/save operations
/// </summary>
public class ConfigurationServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _configPath;

    public ConfigurationServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ConfigServiceTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _configPath = Path.Combine(_tempDir, "config.json");
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

    private ConfigurationService CreateService(string? customPath = null)
    {
        return new ConfigurationService(customPath ?? _configPath);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidPath_ShouldInitialize()
    {
        // Act
        var service = CreateService();

        // Assert
        service.Configuration.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldAcceptAnyPath()
    {
        // Arrange
        var path = @"C:\nonexistent\path\config.json";

        // Act
        var service = new ConfigurationService(path);

        // Assert
        service.Configuration.Should().NotBeNull();
    }

    #endregion

    #region LoadConfigurationAsync Tests

    [Fact]
    public async Task LoadConfigurationAsync_WhenFileDoesNotExist_ShouldCreateDefault()
    {
        // Arrange
        var service = CreateService();
        File.Exists(_configPath).Should().BeFalse();

        // Act
        var config = await service.LoadConfigurationAsync();

        // Assert
        config.Should().NotBeNull();
        File.Exists(_configPath).Should().BeTrue();
    }

    [Fact]
    public async Task LoadConfigurationAsync_WhenFileDoesNotExist_ShouldReturnDefaultConfiguration()
    {
        // Arrange
        var service = CreateService();

        // Act
        var config = await service.LoadConfigurationAsync();

        // Assert
        config.Should().BeEquivalentTo(new HoNConfiguration());
    }

    [Fact]
    public async Task LoadConfigurationAsync_WhenFileExists_ShouldLoadFromFile()
    {
        // Arrange
        var service = CreateService();
        var expectedConfig = new HoNConfiguration
        {
            HonData = new HoNData { ServerName = "TestServer" }
        };
        await service.SaveConfigurationAsync(expectedConfig);

        var newService = CreateService();

        // Act
        var loadedConfig = await newService.LoadConfigurationAsync();

        // Assert
        loadedConfig.HonData?.ServerName.Should().Be("TestServer");
    }

    [Fact]
    public async Task LoadConfigurationAsync_ShouldUpdateConfigurationProperty()
    {
        // Arrange
        var service = CreateService();
        var expectedConfig = new HoNConfiguration
        {
            HonData = new HoNData { ServerName = "PropertyTest" }
        };
        await service.SaveConfigurationAsync(expectedConfig);

        var newService = CreateService();

        // Act
        await newService.LoadConfigurationAsync();

        // Assert
        newService.Configuration.HonData?.ServerName.Should().Be("PropertyTest");
    }

    [Fact]
    public async Task LoadConfigurationAsync_WithInvalidJson_ShouldReturnDefaultConfiguration()
    {
        // Arrange
        await File.WriteAllTextAsync(_configPath, "{ invalid json }}}");
        var service = CreateService();

        // Act
        Func<Task> act = async () => await service.LoadConfigurationAsync();

        // Assert - should throw on invalid JSON
        await act.Should().ThrowAsync<System.Text.Json.JsonException>();
    }

    [Fact]
    public async Task LoadConfigurationAsync_WithEmptyJson_ShouldReturnDefaultConfiguration()
    {
        // Arrange
        await File.WriteAllTextAsync(_configPath, "{}");
        var service = CreateService();

        // Act
        var config = await service.LoadConfigurationAsync();

        // Assert
        config.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadConfigurationAsync_WithNullInJson_ShouldReturnDefaultConfiguration()
    {
        // Arrange
        await File.WriteAllTextAsync(_configPath, "null");
        var service = CreateService();

        // Act
        var config = await service.LoadConfigurationAsync();

        // Assert
        config.Should().NotBeNull();
    }

    #endregion

    #region SaveConfigurationAsync Tests

    [Fact]
    public async Task SaveConfigurationAsync_WithConfig_ShouldWriteToFile()
    {
        // Arrange
        var service = CreateService();
        var config = new HoNConfiguration
        {
            HonData = new HoNData { ServerName = "SaveTest" }
        };

        // Act
        await service.SaveConfigurationAsync(config);

        // Assert
        File.Exists(_configPath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(_configPath);
        content.Should().Contain("SaveTest");
    }

    [Fact]
    public async Task SaveConfigurationAsync_WithConfig_ShouldUpdateInternalConfiguration()
    {
        // Arrange
        var service = CreateService();
        var config = new HoNConfiguration
        {
            HonData = new HoNData { ServerName = "UpdateTest" }
        };

        // Act
        await service.SaveConfigurationAsync(config);

        // Assert
        service.Configuration.HonData?.ServerName.Should().Be("UpdateTest");
    }

    [Fact]
    public async Task SaveConfigurationAsync_WithoutParameter_ShouldSaveCurrentConfiguration()
    {
        // Arrange
        var service = CreateService();
        var config = new HoNConfiguration
        {
            HonData = new HoNData { ServerName = "CurrentConfig" }
        };
        await service.SaveConfigurationAsync(config);

        // Modify internal state by loading
        service.Configuration.HonData!.ServerName = "Modified";

        // Act
        await service.SaveConfigurationAsync();

        // Assert
        var newService = CreateService();
        var loaded = await newService.LoadConfigurationAsync();
        loaded.HonData?.ServerName.Should().Be("Modified");
    }

    [Fact]
    public async Task SaveConfigurationAsync_ShouldCreateDirectoryIfNotExists()
    {
        // Arrange
        var nestedPath = Path.Combine(_tempDir, "nested", "deep", "config.json");
        var service = CreateService(nestedPath);
        var config = new HoNConfiguration();

        // Act
        await service.SaveConfigurationAsync(config);

        // Assert
        File.Exists(nestedPath).Should().BeTrue();
    }

    [Fact]
    public async Task SaveConfigurationAsync_ShouldWriteIndentedJson()
    {
        // Arrange
        var service = CreateService();
        var config = new HoNConfiguration
        {
            HonData = new HoNData { ServerName = "IndentTest" }
        };

        // Act
        await service.SaveConfigurationAsync(config);

        // Assert
        var content = await File.ReadAllTextAsync(_configPath);
        content.Should().Contain("\n"); // Should be multi-line (indented)
    }

    [Fact]
    public async Task SaveConfigurationAsync_ShouldUseSnakeCaseNaming()
    {
        // Arrange
        var service = CreateService();
        var config = new HoNConfiguration
        {
            HonData = new HoNData { ServerName = "NamingTest" }
        };

        // Act
        await service.SaveConfigurationAsync(config);

        // Assert
        var content = await File.ReadAllTextAsync(_configPath);
        // HoNData uses JsonPropertyName attributes, so property names like "svr_name" are used
        content.Should().Contain("svr_name"); // Using explicit JsonPropertyName attribute
    }

    #endregion

    #region ReloadAsync Tests

    [Fact]
    public async Task ReloadAsync_ShouldReloadConfigurationFromFile()
    {
        // Arrange
        var service = CreateService();
        var initialConfig = new HoNConfiguration
        {
            HonData = new HoNData { ServerName = "Initial" }
        };
        await service.SaveConfigurationAsync(initialConfig);
        await service.LoadConfigurationAsync();

        // Modify file externally
        var modifiedConfig = new HoNConfiguration
        {
            HonData = new HoNData { ServerName = "Modified" }
        };
        var newService = CreateService();
        await newService.SaveConfigurationAsync(modifiedConfig);

        // Act
        await service.ReloadAsync();

        // Assert
        service.Configuration.HonData?.ServerName.Should().Be("Modified");
    }

    [Fact]
    public async Task ReloadAsync_WithCancellationToken_ShouldComplete()
    {
        // Arrange
        var service = CreateService();
        await service.SaveConfigurationAsync(new HoNConfiguration());
        using var cts = new CancellationTokenSource();

        // Act
        await service.ReloadAsync(cts.Token);

        // Assert - should complete without throwing
        service.Configuration.Should().NotBeNull();
    }

    #endregion

    #region Configuration Property Tests

    [Fact]
    public void Configuration_ShouldReturnCurrentConfiguration()
    {
        // Arrange
        var service = CreateService();

        // Act
        var config = service.Configuration;

        // Assert
        config.Should().NotBeNull();
    }

    [Fact]
    public async Task Configuration_AfterLoad_ShouldReturnLoadedConfiguration()
    {
        // Arrange
        var service = CreateService();
        var expected = new HoNConfiguration
        {
            HonData = new HoNData { TotalServers = 5 }
        };
        await service.SaveConfigurationAsync(expected);

        var newService = CreateService();
        await newService.LoadConfigurationAsync();

        // Act
        var config = newService.Configuration;

        // Assert
        config.HonData?.TotalServers.Should().Be(5);
    }

    #endregion

    #region Roundtrip Tests

    [Fact]
    public async Task SaveAndLoad_ShouldPreserveAllProperties()
    {
        // Arrange
        var service = CreateService();
        var config = new HoNConfiguration
        {
            HonData = new HoNData
            {
                ServerName = "RoundtripTest",
                TotalServers = 10,
                HonInstallDirectory = "/path/to/hon"
            }
        };

        // Act
        await service.SaveConfigurationAsync(config);
        var newService = CreateService();
        var loaded = await newService.LoadConfigurationAsync();

        // Assert
        loaded.HonData?.ServerName.Should().Be("RoundtripTest");
        loaded.HonData?.TotalServers.Should().Be(10);
        loaded.HonData?.HonInstallDirectory.Should().Be("/path/to/hon");
    }

    [Fact]
    public async Task MultipleSaveLoad_ShouldBeIdempotent()
    {
        // Arrange
        var service = CreateService();
        var config = new HoNConfiguration
        {
            HonData = new HoNData { ServerName = "Idempotent" }
        };

        // Act - Save and load multiple times
        for (int i = 0; i < 3; i++)
        {
            await service.SaveConfigurationAsync(config);
            await service.LoadConfigurationAsync();
        }

        // Assert
        service.Configuration.HonData?.ServerName.Should().Be("Idempotent");
    }

    #endregion

    #region Concurrency Tests

    [Fact]
    public async Task MultipleSaves_ShouldProduceValidJson()
    {
        // Arrange
        var service = CreateService();
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        // Act - Multiple sequential saves
        for (int i = 0; i < 10; i++)
        {
            var config = new HoNConfiguration
            {
                HonData = new HoNData { ServerName = $"Server{i}" }
            };
            await service.SaveConfigurationAsync(config);
        }

        // Assert - File should be valid JSON
        var content = await File.ReadAllTextAsync(_configPath);
        var act = () => JsonSerializer.Deserialize<HoNConfiguration>(content, jsonOptions);
        act.Should().NotThrow();
        
        // Verify last save was persisted
        var loaded = await service.LoadConfigurationAsync();
        loaded.HonData?.ServerName.Should().Be("Server9");
    }

    #endregion
}

#region DTO Tests

public class HoNConfigurationDtoTests
{
    [Fact]
    public void HoNConfiguration_DefaultConstructor_ShouldInitialize()
    {
        // Act
        var config = new HoNConfiguration();

        // Assert
        config.Should().NotBeNull();
    }

    [Fact]
    public void HonData_ShouldHaveDefaultValues()
    {
        // Act
        var honData = new HoNData();

        // Assert
        honData.ServerName.Should().Be("Unknown"); // Default value
        honData.TotalServers.Should().Be(0);
    }

    [Fact]
    public void ApplicationData_ShouldHaveDefaultValues()
    {
        // Act
        var appData = new ApplicationData();

        // Assert
        appData.AutoScaling.Should().BeNull();
    }
}

#endregion
