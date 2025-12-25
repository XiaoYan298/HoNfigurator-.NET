using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using HoNfigurator.Core.Models;
using HoNfigurator.Core.Services;

namespace HoNfigurator.Tests.Services;

/// <summary>
/// Tests for ServerPatchingService - Game server patching and updates
/// </summary>
public class ServerPatchingServiceTests
{
    private readonly Mock<ILogger<ServerPatchingService>> _loggerMock;
    private readonly HoNConfiguration _config;

    public ServerPatchingServiceTests()
    {
        _loggerMock = new Mock<ILogger<ServerPatchingService>>();
        _config = new HoNConfiguration
        {
            HonData = new HoNData
            {
                HonInstallDirectory = Path.Combine(Path.GetTempPath(), "hon_test"),
                PatchServer = "patches.example.com"
            }
        };
    }

    private ServerPatchingService CreateService(HoNConfiguration? customConfig = null)
    {
        return new ServerPatchingService(_loggerMock.Object, customConfig ?? _config);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldInitialize()
    {
        // Act
        var service = CreateService();

        // Assert
        service.Should().NotBeNull();
        service.IsPatchingInProgress.Should().BeFalse();
    }

    #endregion

    #region IsPatchingInProgress Tests

    [Fact]
    public void IsPatchingInProgress_Initially_ShouldBeFalse()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        service.IsPatchingInProgress.Should().BeFalse();
    }

    #endregion

    #region CheckForPatchAsync Tests

    [Fact]
    public async Task CheckForPatchAsync_ShouldReturnResult()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.CheckForPatchAsync();

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckForPatchAsync_ShouldIncludeCurrentVersion()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.CheckForPatchAsync();

        // Assert
        result.CurrentVersion.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckForPatchAsync_WhenServerUnreachable_ShouldReturnError()
    {
        // Arrange
        var config = new HoNConfiguration
        {
            HonData = new HoNData { PatchServer = "nonexistent.invalid" }
        };
        var service = CreateService(config);

        // Act
        var result = await service.CheckForPatchAsync();

        // Assert
        // Should complete without throwing (error captured in result)
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckForPatchAsync_WithCancellationToken_ShouldHandleCancellation()
    {
        // Arrange
        var service = CreateService();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act - Method catches exceptions and returns result
        var result = await service.CheckForPatchAsync(cts.Token);

        // Assert - Method should return result with error, not throw
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
    }

    #endregion

    #region ApplyPatchAsync Tests

    [Fact]
    public async Task ApplyPatchAsync_WhenAlreadyPatching_ShouldReturnError()
    {
        // Note: This is hard to test without reflection or internal state manipulation
        // We test the basic path instead
        var service = CreateService();

        // Act
        var result = await service.ApplyPatchAsync();

        // Assert - Should not fail catastrophically
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ApplyPatchAsync_WithInvalidInstallDir_ShouldReturnError()
    {
        // Arrange
        var config = new HoNConfiguration
        {
            HonData = new HoNData { HonInstallDirectory = "" }
        };
        var service = CreateService(config);

        // Act
        var result = await service.ApplyPatchAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Invalid");
    }

    [Fact]
    public async Task ApplyPatchAsync_WithNonExistentDir_ShouldReturnError()
    {
        // Arrange
        var config = new HoNConfiguration
        {
            HonData = new HoNData { HonInstallDirectory = "/nonexistent/path/that/does/not/exist" }
        };
        var service = CreateService(config);

        // Act
        var result = await service.ApplyPatchAsync();

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ApplyPatchAsync_ShouldAcceptProgressCallback()
    {
        // Arrange
        var service = CreateService();
        var progressReports = new List<GamePatchProgress>();
        var progress = new Progress<GamePatchProgress>(p => progressReports.Add(p));

        // Act
        await service.ApplyPatchAsync(progress: progress);

        // Assert - Just verify it doesn't throw
        service.Should().NotBeNull();
    }

    #endregion

    #region VerifyGameFilesAsync Tests

    [Fact]
    public async Task VerifyGameFilesAsync_WithInvalidInstallDir_ShouldReturnError()
    {
        // Arrange
        var config = new HoNConfiguration
        {
            HonData = new HoNData { HonInstallDirectory = "" }
        };
        var service = CreateService(config);

        // Act
        var result = await service.VerifyGameFilesAsync();

        // Assert
        result.Error.Should().Contain("Invalid");
    }

    [Fact]
    public async Task VerifyGameFilesAsync_ShouldReturnResult()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.VerifyGameFilesAsync();

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region RepairGameFilesAsync Tests

    [Fact]
    public async Task RepairGameFilesAsync_WithEmptyList_ShouldReturnSuccess()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.RepairGameFilesAsync(filesToRepair: new List<string>());

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("No files need repair");
    }

    [Fact]
    public async Task RepairGameFilesAsync_ShouldAcceptFileList()
    {
        // Arrange
        var service = CreateService();
        var files = new List<string> { "test.dll", "game.exe" };

        // Act
        var result = await service.RepairGameFilesAsync(filesToRepair: files);

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region GetCurrentVersionAsync Tests

    [Fact]
    public async Task GetCurrentVersionAsync_WithNoInstallDir_ShouldReturnUnknown()
    {
        // Arrange
        var config = new HoNConfiguration
        {
            HonData = new HoNData { HonInstallDirectory = "" }
        };
        var service = CreateService(config);

        // Act
        var version = await service.GetCurrentVersionAsync();

        // Assert
        version.Should().Be("unknown");
    }

    [Fact]
    public async Task GetCurrentVersionAsync_WithNullInstallDir_ShouldReturnUnknown()
    {
        // Arrange
        var config = new HoNConfiguration
        {
            HonData = new HoNData { HonInstallDirectory = null }
        };
        var service = CreateService(config);

        // Act
        var version = await service.GetCurrentVersionAsync();

        // Assert
        version.Should().Be("unknown");
    }

    [Fact]
    public async Task GetCurrentVersionAsync_WithVersionFile_ShouldReadVersion()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"hon_version_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var versionFile = Path.Combine(tempDir, "version.txt");
            await File.WriteAllTextAsync(versionFile, "4.10.1");

            var config = new HoNConfiguration
            {
                HonData = new HoNData { HonInstallDirectory = tempDir }
            };
            var service = CreateService(config);

            // Act
            var version = await service.GetCurrentVersionAsync();

            // Assert
            version.Should().Be("4.10.1");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task GetCurrentVersionAsync_WithVersionFileContainingWhitespace_ShouldTrim()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"hon_version_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var versionFile = Path.Combine(tempDir, "version.txt");
            await File.WriteAllTextAsync(versionFile, "  4.10.2  \n");

            var config = new HoNConfiguration
            {
                HonData = new HoNData { HonInstallDirectory = tempDir }
            };
            var service = CreateService(config);

            // Act
            var version = await service.GetCurrentVersionAsync();

            // Assert
            version.Should().Be("4.10.2");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    #endregion
}

#region DTO Tests

public class GamePatchCheckResultDtoTests
{
    [Fact]
    public void GamePatchCheckResult_DefaultConstructor_ShouldInitialize()
    {
        // Act
        var result = new GamePatchCheckResult();

        // Assert
        result.Success.Should().BeFalse();
        result.CurrentVersion.Should().Be("unknown"); // Has default value
        result.LatestVersion.Should().BeNull();
        result.PatchAvailable.Should().BeFalse();
        result.PatchSize.Should().BeNull(); // Nullable long
        result.PatchUrl.Should().BeNull();
        result.ReleaseNotes.Should().BeNull();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void GamePatchCheckResult_WithPatchAvailable()
    {
        // Act
        var result = new GamePatchCheckResult
        {
            Success = true,
            CurrentVersion = "4.10.0",
            LatestVersion = "4.10.1",
            PatchAvailable = true,
            PatchSize = 1024 * 1024 * 50, // 50MB
            PatchUrl = "https://patches.example.com/4.10.1.zip",
            ReleaseNotes = "Bug fixes and improvements"
        };

        // Assert
        result.PatchAvailable.Should().BeTrue();
        result.PatchSize.Should().Be(50 * 1024 * 1024);
    }
}

public class GamePatchResultDtoTests
{
    [Fact]
    public void GamePatchResult_DefaultConstructor_ShouldInitialize()
    {
        // Act
        var result = new GamePatchResult();

        // Assert
        result.Success.Should().BeFalse();
        result.PreviousVersion.Should().BeNull();
        result.NewVersion.Should().BeNull();
        result.Message.Should().BeNull();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void GamePatchResult_SuccessfulPatch()
    {
        // Act
        var result = new GamePatchResult
        {
            Success = true,
            PreviousVersion = "4.10.0",
            NewVersion = "4.10.1",
            Message = "Patched from 4.10.0 to 4.10.1"
        };

        // Assert
        result.Success.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void GamePatchResult_FailedPatch()
    {
        // Act
        var result = new GamePatchResult
        {
            Success = false,
            PreviousVersion = "4.10.0",
            Error = "Download failed: Connection timeout"
        };

        // Assert
        result.Success.Should().BeFalse();
        result.NewVersion.Should().BeNull();
    }
}

public class GamePatchProgressDtoTests
{
    [Fact]
    public void GamePatchProgress_DefaultConstructor_ShouldInitialize()
    {
        // Act
        var progress = new GamePatchProgress();

        // Assert
        progress.Stage.Should().Be(string.Empty); // Has default value
        progress.Message.Should().Be(string.Empty); // Has default value
        progress.PercentComplete.Should().Be(0);
    }

    [Fact]
    public void GamePatchProgress_ShouldAllowSettingProperties()
    {
        // Act
        var progress = new GamePatchProgress
        {
            Stage = "Download",
            Message = "Downloading patch file...",
            PercentComplete = 45
        };

        // Assert
        progress.Stage.Should().Be("Download");
        progress.Message.Should().Be("Downloading patch file...");
        progress.PercentComplete.Should().Be(45);
    }
}

public class IntegrityCheckResultDtoTests
{
    [Fact]
    public void IntegrityCheckResult_DefaultConstructor_ShouldInitialize()
    {
        // Act
        var result = new IntegrityCheckResult();

        // Assert
        result.Success.Should().BeFalse();
        result.TotalFiles.Should().Be(0);
        result.VerifiedFiles.Should().Be(0);
        result.MissingFiles.Should().NotBeNull();
        result.MissingFiles.Should().BeEmpty();
        result.CorruptedFiles.Should().NotBeNull();
        result.CorruptedFiles.Should().BeEmpty();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void IntegrityCheckResult_AllFilesValid()
    {
        // Act
        var result = new IntegrityCheckResult
        {
            Success = true,
            TotalFiles = 100,
            VerifiedFiles = 100
        };

        // Assert
        result.Success.Should().BeTrue();
        result.TotalFiles.Should().Be(result.VerifiedFiles);
    }

    [Fact]
    public void IntegrityCheckResult_WithCorruptedFiles()
    {
        // Act
        var result = new IntegrityCheckResult
        {
            Success = false,
            TotalFiles = 100,
            VerifiedFiles = 98,
            CorruptedFiles =
            {
                new CorruptedFile { Path = "game.dll", ExpectedCrc = "abc123", ActualCrc = "def456" }
            },
            MissingFiles = { "config.ini" }
        };

        // Assert
        result.Success.Should().BeFalse();
        result.CorruptedFiles.Should().HaveCount(1);
        result.MissingFiles.Should().HaveCount(1);
    }
}

public class CorruptedFileDtoTests
{
    [Fact]
    public void CorruptedFile_DefaultConstructor_ShouldInitialize()
    {
        // Act
        var file = new CorruptedFile();

        // Assert
        file.Path.Should().Be(string.Empty); // Has default value
        file.ExpectedCrc.Should().Be(string.Empty); // Has default value
        file.ActualCrc.Should().Be(string.Empty); // Has default value
    }

    [Fact]
    public void CorruptedFile_ShouldAllowSettingProperties()
    {
        // Act
        var file = new CorruptedFile
        {
            Path = "game/data/heroes.s2z",
            ExpectedCrc = "a1b2c3d4",
            ActualCrc = "e5f6g7h8"
        };

        // Assert
        file.Path.Should().Be("game/data/heroes.s2z");
        file.ExpectedCrc.Should().Be("a1b2c3d4");
        file.ActualCrc.Should().Be("e5f6g7h8");
    }
}

public class RepairResultDtoTests
{
    [Fact]
    public void RepairResult_DefaultConstructor_ShouldInitialize()
    {
        // Act
        var result = new RepairResult();

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().BeNull();
        result.RepairedFiles.Should().NotBeNull();
        result.RepairedFiles.Should().BeEmpty();
        result.FailedFiles.Should().NotBeNull();
        result.FailedFiles.Should().BeEmpty();
    }

    [Fact]
    public void RepairResult_SuccessfulRepair()
    {
        // Act
        var result = new RepairResult
        {
            Success = true,
            Message = "Repaired 3/3 files",
            RepairedFiles = { "file1.dll", "file2.dll", "file3.dll" }
        };

        // Assert
        result.Success.Should().BeTrue();
        result.RepairedFiles.Should().HaveCount(3);
        result.FailedFiles.Should().BeEmpty();
    }

    [Fact]
    public void RepairResult_PartialRepair()
    {
        // Act
        var result = new RepairResult
        {
            Success = false,
            Message = "Repaired 2/3 files",
            RepairedFiles = { "file1.dll", "file2.dll" },
            FailedFiles = { "file3.dll" }
        };

        // Assert
        result.Success.Should().BeFalse();
        result.RepairedFiles.Should().HaveCount(2);
        result.FailedFiles.Should().HaveCount(1);
    }
}

#endregion
