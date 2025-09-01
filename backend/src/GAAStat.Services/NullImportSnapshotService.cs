using GAAStat.Services.Interfaces;
using GAAStat.Services.Models;

namespace GAAStat.Services;

/// <summary>
/// Null implementation of IImportSnapshotService for when snapshot functionality is disabled
/// </summary>
public class NullImportSnapshotService : IImportSnapshotService
{
    public Task<ServiceResult<int>> CreateSnapshotAsync(string description, bool compress = true)
    {
        return Task.FromResult(ServiceResult<int>.Success(0));
    }

    public Task<ServiceResult<SnapshotCreationResult>> CreatePreImportSnapshotAsync(string fileName, string importType = "Excel")
    {
        var result = new SnapshotCreationResult
        {
            SnapshotId = 0,
            Description = $"Snapshot functionality disabled - {fileName}",
            CreatedAt = DateTime.UtcNow,
            MatchCount = 0,
            PlayerStatCount = 0,
            UncompressedSizeBytes = 0,
            CompressedSizeBytes = 0,
            CompressionRatio = 0,
            CreationDuration = TimeSpan.Zero,
            CompressionEnabled = false
        };
        
        return Task.FromResult(ServiceResult<SnapshotCreationResult>.Success(result));
    }

    public Task<ServiceResult<SnapshotRestoreResult>> RestoreFromSnapshotAsync(int snapshotId, bool validateIntegrity = true)
    {
        return Task.FromResult(ServiceResult<SnapshotRestoreResult>.Failed("Snapshot functionality is disabled"));
    }

    public Task<ServiceResult<IEnumerable<SnapshotSummary>>> GetSnapshotsAsync(int count = 10, bool includeMetrics = false)
    {
        return Task.FromResult(ServiceResult<IEnumerable<SnapshotSummary>>.Success(Enumerable.Empty<SnapshotSummary>()));
    }

    public Task<ServiceResult<SnapshotCleanupResult>> CleanupSnapshotsAsync(SnapshotCleanupPolicy? policy = null)
    {
        var result = new SnapshotCleanupResult
        {
            SnapshotsDeleted = 0,
            SpaceFreedBytes = 0,
            CleanupDuration = TimeSpan.Zero,
            DeletedSnapshotIds = new List<int>(),
            CleanupReasons = new List<string> { "Snapshot functionality is disabled - no cleanup performed" },
            PolicyTriggered = false,
            SnapshotsRemaining = 0,
            RemainingStorageBytes = 0
        };
        
        return Task.FromResult(ServiceResult<SnapshotCleanupResult>.Success(result));
    }

    public Task<ServiceResult<SnapshotValidationResult>> ValidateSnapshotAsync(int snapshotId, bool performIntegrityCheck = false)
    {
        return Task.FromResult(ServiceResult<SnapshotValidationResult>.Failed("Snapshot functionality is disabled"));
    }

    public Task<ServiceResult<SnapshotStorageStats>> GetStorageStatisticsAsync()
    {
        var result = new SnapshotStorageStats
        {
            TotalSnapshots = 0,
            CompressedSnapshots = 0,
            UncompressedSnapshots = 0,
            TotalStorageBytes = 0,
            CompressedStorageBytes = 0,
            UncompressedStorageBytes = 0,
            AverageCompressionRatio = 0,
            PotentialSavingsBytes = 0,
            OldestSnapshot = DateTime.MinValue,
            NewestSnapshot = DateTime.MinValue,
            Recommendations = new List<SnapshotStorageRecommendation>
            {
                new() { Type = "Info", Description = "Snapshot functionality is disabled", Priority = "Low" }
            },
            SnapshotsByType = new Dictionary<string, int>()
        };
        
        return Task.FromResult(ServiceResult<SnapshotStorageStats>.Success(result));
    }

    public Task<ServiceResult<SnapshotCompressionResult>> CompressSnapshotsAsync(int? snapshotId = null)
    {
        var result = new SnapshotCompressionResult
        {
            SnapshotsProcessed = 0,
            SnapshotsCompressed = 0,
            SnapshotsSkipped = 0,
            SpaceSavedBytes = 0,
            CompressionDuration = TimeSpan.Zero,
            AverageCompressionRatio = 0,
            CompressedSnapshotIds = new List<int>(),
            Errors = new List<string> { "Snapshot functionality is disabled - no compression performed" }
        };
        
        return Task.FromResult(ServiceResult<SnapshotCompressionResult>.Success(result));
    }
}