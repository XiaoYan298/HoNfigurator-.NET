using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HoNfigurator.Core.Models;
using System.Security.Cryptography;
using System.Text;

namespace HoNfigurator.Core.Services;

/// <summary>
/// Interface for replay upload service
/// </summary>
public interface IReplayUploadService
{
    Task<ReplayUploadResult> UploadReplayAsync(string filePath, string matchId);
    Task<ReplayUploadResult> UploadReplayAsync(byte[] data, string matchId, string fileName);
    Task<string?> GetShareableLinkAsync(string matchId);
    Task<bool> DeleteUploadedReplayAsync(string matchId);
    Task<List<UploadedReplay>> GetUploadedReplaysAsync(int count = 50);
    ReplayUploadSettings Settings { get; }
}

/// <summary>
/// Result of replay upload operation
/// </summary>
public class ReplayUploadResult
{
    public bool Success { get; set; }
    public string? Url { get; set; }
    public string? ShareableLink { get; set; }
    public string? Error { get; set; }
    public long FileSizeBytes { get; set; }
    public DateTime UploadTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Information about an uploaded replay
/// </summary>
public class UploadedReplay
{
    public string MatchId { get; set; } = "";
    public string FileName { get; set; } = "";
    public string Url { get; set; } = "";
    public string ShareableLink { get; set; } = "";
    public long FileSizeBytes { get; set; }
    public DateTime UploadTime { get; set; }
}

/// <summary>
/// Service for uploading replays to cloud storage and generating shareable links
/// </summary>
public class ReplayUploadService : IReplayUploadService
{
    private readonly ILogger<ReplayUploadService> _logger;
    private readonly HoNConfiguration _config;
    private readonly ReplayUploadSettings _settings;
    private readonly string _uploadedReplaysPath;
    private readonly List<UploadedReplay> _uploadedReplays = new();
    private readonly object _lock = new();

    public ReplayUploadSettings Settings => _settings;

    public ReplayUploadService(ILogger<ReplayUploadService> logger, HoNConfiguration config)
    {
        _logger = logger;
        _config = config;
        _settings = config.ApplicationData?.ReplayUpload ?? new ReplayUploadSettings();
        
        // Initialize local storage for uploaded replays info
        _uploadedReplaysPath = Path.Combine(AppContext.BaseDirectory, "data", "uploaded_replays.json");
        LoadUploadedReplays();
    }

    private void LoadUploadedReplays()
    {
        try
        {
            if (File.Exists(_uploadedReplaysPath))
            {
                var json = File.ReadAllText(_uploadedReplaysPath);
                var replays = System.Text.Json.JsonSerializer.Deserialize<List<UploadedReplay>>(json);
                if (replays != null)
                {
                    lock (_lock)
                    {
                        _uploadedReplays.Clear();
                        _uploadedReplays.AddRange(replays);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load uploaded replays list");
        }
    }

    private void SaveUploadedReplays()
    {
        try
        {
            var dir = Path.GetDirectoryName(_uploadedReplaysPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            List<UploadedReplay> toSave;
            lock (_lock)
            {
                toSave = _uploadedReplays.ToList();
            }
            
            var json = System.Text.Json.JsonSerializer.Serialize(toSave, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            File.WriteAllText(_uploadedReplaysPath, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save uploaded replays list");
        }
    }

    public async Task<ReplayUploadResult> UploadReplayAsync(string filePath, string matchId)
    {
        if (!File.Exists(filePath))
        {
            return new ReplayUploadResult 
            { 
                Success = false, 
                Error = "File not found" 
            };
        }

        var data = await File.ReadAllBytesAsync(filePath);
        var fileName = Path.GetFileName(filePath);
        return await UploadReplayAsync(data, matchId, fileName);
    }

    public async Task<ReplayUploadResult> UploadReplayAsync(byte[] data, string matchId, string fileName)
    {
        if (!_settings.Enabled)
        {
            return new ReplayUploadResult 
            { 
                Success = false, 
                Error = "Replay upload is disabled" 
            };
        }

        _logger.LogInformation("Uploading replay for match {MatchId}, size: {Size} bytes", matchId, data.Length);

        // Generate unique file name
        var uniqueId = GenerateShortId();
        var uploadFileName = $"{matchId}_{uniqueId}.honreplay";

        ReplayUploadResult result;

        for (int attempt = 1; attempt <= _settings.RetryCount; attempt++)
        {
            try
            {
                result = _settings.Provider.ToLowerInvariant() switch
                {
                    "local" => await UploadToLocalAsync(data, uploadFileName),
                    "azure" => await UploadToAzureBlobAsync(data, uploadFileName),
                    "s3" => await UploadToS3Async(data, uploadFileName),
                    "ftp" => await UploadToFtpAsync(data, uploadFileName),
                    _ => await UploadToLocalAsync(data, uploadFileName)
                };

                if (result.Success)
                {
                    result.FileSizeBytes = data.Length;
                    result.ShareableLink = GenerateShareableLink(matchId, uniqueId);

                    // Track uploaded replay
                    var uploadedReplay = new UploadedReplay
                    {
                        MatchId = matchId,
                        FileName = uploadFileName,
                        Url = result.Url ?? "",
                        ShareableLink = result.ShareableLink,
                        FileSizeBytes = data.Length,
                        UploadTime = DateTime.UtcNow
                    };

                    lock (_lock)
                    {
                        _uploadedReplays.Insert(0, uploadedReplay);
                        // Keep only last 1000 entries
                        if (_uploadedReplays.Count > 1000)
                            _uploadedReplays.RemoveRange(1000, _uploadedReplays.Count - 1000);
                    }
                    SaveUploadedReplays();

                    _logger.LogInformation("Replay uploaded successfully: {Url}", result.Url);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Upload attempt {Attempt}/{Max} failed", attempt, _settings.RetryCount);
                if (attempt < _settings.RetryCount)
                {
                    await Task.Delay(TimeSpan.FromSeconds(_settings.RetryDelaySeconds * attempt));
                }
            }
        }

        return new ReplayUploadResult 
        { 
            Success = false, 
            Error = "Upload failed after all retries" 
        };
    }

    private async Task<ReplayUploadResult> UploadToLocalAsync(byte[] data, string fileName)
    {
        var uploadDir = string.IsNullOrEmpty(_settings.BasePath) 
            ? Path.Combine(AppContext.BaseDirectory, "uploads", "replays")
            : _settings.BasePath;

        if (!Directory.Exists(uploadDir))
            Directory.CreateDirectory(uploadDir);

        var filePath = Path.Combine(uploadDir, fileName);
        await File.WriteAllBytesAsync(filePath, data);

        var baseUrl = string.IsNullOrEmpty(_settings.BaseUrl) 
            ? "/api/replays/uploaded" 
            : _settings.BaseUrl.TrimEnd('/');

        return new ReplayUploadResult
        {
            Success = true,
            Url = $"{baseUrl}/{fileName}"
        };
    }

    private async Task<ReplayUploadResult> UploadToAzureBlobAsync(byte[] data, string fileName)
    {
        // Azure Blob Storage implementation
        // This would require Azure.Storage.Blobs package
        // For now, fallback to local storage
        _logger.LogWarning("Azure Blob storage not implemented, using local storage");
        return await UploadToLocalAsync(data, fileName);
    }

    private async Task<ReplayUploadResult> UploadToS3Async(byte[] data, string fileName)
    {
        // AWS S3 implementation
        // This would require AWSSDK.S3 package
        _logger.LogWarning("AWS S3 storage not implemented, using local storage");
        return await UploadToLocalAsync(data, fileName);
    }

    private async Task<ReplayUploadResult> UploadToFtpAsync(byte[] data, string fileName)
    {
        // FTP implementation
        _logger.LogWarning("FTP storage not implemented, using local storage");
        return await UploadToLocalAsync(data, fileName);
    }

    private string GenerateShortId()
    {
        var bytes = new byte[6];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    private string GenerateShareableLink(string matchId, string uniqueId)
    {
        var baseUrl = string.IsNullOrEmpty(_settings.BaseUrl) 
            ? "/replay" 
            : _settings.BaseUrl.TrimEnd('/') + "/share";

        return $"{baseUrl}/{matchId}/{uniqueId}";
    }

    public async Task<string?> GetShareableLinkAsync(string matchId)
    {
        await Task.CompletedTask;
        
        lock (_lock)
        {
            var replay = _uploadedReplays.FirstOrDefault(r => r.MatchId == matchId);
            return replay?.ShareableLink;
        }
    }

    public async Task<bool> DeleteUploadedReplayAsync(string matchId)
    {
        await Task.CompletedTask;

        UploadedReplay? replay;
        lock (_lock)
        {
            replay = _uploadedReplays.FirstOrDefault(r => r.MatchId == matchId);
            if (replay != null)
            {
                _uploadedReplays.Remove(replay);
            }
        }

        if (replay == null) return false;

        // Try to delete the actual file if using local storage
        if (_settings.Provider.ToLowerInvariant() == "local")
        {
            var uploadDir = string.IsNullOrEmpty(_settings.BasePath) 
                ? Path.Combine(AppContext.BaseDirectory, "uploads", "replays")
                : _settings.BasePath;
            var filePath = Path.Combine(uploadDir, replay.FileName);
            
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete uploaded replay file");
                }
            }
        }

        SaveUploadedReplays();
        return true;
    }

    public async Task<List<UploadedReplay>> GetUploadedReplaysAsync(int count = 50)
    {
        await Task.CompletedTask;
        
        lock (_lock)
        {
            return _uploadedReplays.Take(count).ToList();
        }
    }
}
