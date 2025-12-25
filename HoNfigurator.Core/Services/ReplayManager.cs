using Microsoft.Extensions.Logging;
using System.IO.Compression;

namespace HoNfigurator.Core.Services;

/// <summary>
/// Manages replay files - storage, retrieval, and cleanup
/// </summary>
public class ReplayManager
{
    private readonly ILogger<ReplayManager> _logger;
    private readonly string _replaysPath;
    private readonly string _archivePath;

    public ReplayManager(ILogger<ReplayManager> logger, string replaysPath = "replays")
    {
        _logger = logger;
        _replaysPath = replaysPath;
        _archivePath = Path.Combine(replaysPath, "archive");
        
        Directory.CreateDirectory(_replaysPath);
        Directory.CreateDirectory(_archivePath);
    }

    public List<ReplayInfo> GetReplays(int limit = 100, int offset = 0)
    {
        var replays = new List<ReplayInfo>();
        
        if (!Directory.Exists(_replaysPath))
            return replays;

        var files = Directory.GetFiles(_replaysPath, "*.honreplay")
            .OrderByDescending(f => File.GetCreationTimeUtc(f))
            .Skip(offset)
            .Take(limit);

        foreach (var file in files)
        {
            var info = new FileInfo(file);
            replays.Add(new ReplayInfo
            {
                FileName = info.Name,
                FilePath = file,
                SizeBytes = info.Length,
                SizeMb = Math.Round(info.Length / 1024.0 / 1024.0, 2),
                CreatedAt = info.CreationTimeUtc,
                MatchId = ExtractMatchId(info.Name)
            });
        }

        return replays;
    }

    public ReplayInfo? GetReplay(string fileName)
    {
        var path = Path.Combine(_replaysPath, fileName);
        if (!File.Exists(path))
            return null;

        var info = new FileInfo(path);
        return new ReplayInfo
        {
            FileName = info.Name,
            FilePath = path,
            SizeBytes = info.Length,
            SizeMb = Math.Round(info.Length / 1024.0 / 1024.0, 2),
            CreatedAt = info.CreationTimeUtc,
            MatchId = ExtractMatchId(info.Name)
        };
    }

    public byte[]? GetReplayData(string fileName)
    {
        var path = Path.Combine(_replaysPath, fileName);
        if (!File.Exists(path))
            return null;

        return File.ReadAllBytes(path);
    }

    public async Task<string?> SaveReplayAsync(long matchId, byte[] data)
    {
        try
        {
            var fileName = $"M{matchId}.honreplay";
            var path = Path.Combine(_replaysPath, fileName);
            
            await File.WriteAllBytesAsync(path, data);
            _logger.LogInformation("Saved replay for match {MatchId}: {FileName}", matchId, fileName);
            
            return fileName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save replay for match {MatchId}", matchId);
            return null;
        }
    }

    public bool DeleteReplay(string fileName)
    {
        try
        {
            var path = Path.Combine(_replaysPath, fileName);
            if (!File.Exists(path))
                return false;

            File.Delete(path);
            _logger.LogInformation("Deleted replay: {FileName}", fileName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete replay: {FileName}", fileName);
            return false;
        }
    }

    public async Task<int> ArchiveOldReplaysAsync(int daysOld = 30)
    {
        var cutoff = DateTime.UtcNow.AddDays(-daysOld);
        var archived = 0;

        try
        {
            var oldFiles = Directory.GetFiles(_replaysPath, "*.honreplay")
                .Where(f => File.GetCreationTimeUtc(f) < cutoff);

            foreach (var file in oldFiles)
            {
                var fileName = Path.GetFileName(file);
                var archiveName = Path.Combine(_archivePath, fileName + ".gz");

                await using var input = File.OpenRead(file);
                await using var output = File.Create(archiveName);
                await using var gzip = new GZipStream(output, CompressionLevel.Optimal);
                await input.CopyToAsync(gzip);

                File.Delete(file);
                archived++;
            }

            if (archived > 0)
                _logger.LogInformation("Archived {Count} old replays", archived);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving old replays");
        }

        return archived;
    }

    public async Task<int> CleanupOldReplaysAsync(int daysOld = 90)
    {
        var cutoff = DateTime.UtcNow.AddDays(-daysOld);
        var deleted = 0;

        try
        {
            // Clean archived replays
            if (Directory.Exists(_archivePath))
            {
                foreach (var file in Directory.GetFiles(_archivePath, "*.gz"))
                {
                    if (File.GetCreationTimeUtc(file) < cutoff)
                    {
                        File.Delete(file);
                        deleted++;
                    }
                }
            }

            if (deleted > 0)
                _logger.LogInformation("Deleted {Count} old archived replays", deleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old replays");
        }

        return deleted;
    }

    public ReplayStats GetStats()
    {
        var stats = new ReplayStats();

        if (Directory.Exists(_replaysPath))
        {
            var files = Directory.GetFiles(_replaysPath, "*.honreplay");
            stats.TotalReplays = files.Length;
            stats.TotalSizeBytes = files.Sum(f => new FileInfo(f).Length);
            stats.TotalSizeMb = Math.Round(stats.TotalSizeBytes / 1024.0 / 1024.0, 2);

            if (files.Any())
            {
                stats.OldestReplay = files.Min(f => File.GetCreationTimeUtc(f));
                stats.NewestReplay = files.Max(f => File.GetCreationTimeUtc(f));
            }
        }

        if (Directory.Exists(_archivePath))
        {
            var archivedFiles = Directory.GetFiles(_archivePath, "*.gz");
            stats.ArchivedReplays = archivedFiles.Length;
            stats.ArchivedSizeBytes = archivedFiles.Sum(f => new FileInfo(f).Length);
            stats.ArchivedSizeMb = Math.Round(stats.ArchivedSizeBytes / 1024.0 / 1024.0, 2);
        }

        return stats;
    }

    private long ExtractMatchId(string fileName)
    {
        // Format: M123456.honreplay
        if (fileName.StartsWith("M") && fileName.EndsWith(".honreplay"))
        {
            var idPart = fileName[1..^10];
            if (long.TryParse(idPart, out var matchId))
                return matchId;
        }
        return 0;
    }
}

public class ReplayInfo
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public double SizeMb { get; set; }
    public DateTime CreatedAt { get; set; }
    public long MatchId { get; set; }
}

public class ReplayStats
{
    public int TotalReplays { get; set; }
    public long TotalSizeBytes { get; set; }
    public double TotalSizeMb { get; set; }
    public int ArchivedReplays { get; set; }
    public long ArchivedSizeBytes { get; set; }
    public double ArchivedSizeMb { get; set; }
    public DateTime? OldestReplay { get; set; }
    public DateTime? NewestReplay { get; set; }
}
