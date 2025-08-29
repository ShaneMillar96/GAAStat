namespace GAAStat.Services.Models;

/// <summary>
/// Snapshot restoration operation result
/// </summary>
public class SnapshotRestoreResult
{
    public int SnapshotId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime SnapshotCreatedAt { get; set; }
    public DateTime RestoredAt { get; set; }
    public int MatchesRestored { get; set; }
    public int PlayerStatsRestored { get; set; }
    public TimeSpan RestoreDuration { get; set; }
    public bool RestorationSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Enhanced snapshot summary for UI display with performance metrics
/// </summary>
public class SnapshotSummary
{
    public int SnapshotId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int MatchCount { get; set; }
    public int PlayerStatCount { get; set; }
    public long DataSizeBytes { get; set; }
    public long CompressedSizeBytes { get; set; }
    public bool IsCompressed { get; set; }
    public decimal CompressionRatio { get; set; }
    public bool CanRestore { get; set; }
    public string? Notes { get; set; }
    public TimeSpan? CreationDuration { get; set; }
    public string ImportType { get; set; } = string.Empty;
    public string? AssociatedFileName { get; set; }
    public bool IntegrityVerified { get; set; }
    public DateTime? LastValidated { get; set; }
}

/// <summary>
/// Result of snapshot creation with performance metrics
/// </summary>
public class SnapshotCreationResult
{
    public int SnapshotId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public TimeSpan CreationDuration { get; set; }
    public long UncompressedSizeBytes { get; set; }
    public long CompressedSizeBytes { get; set; }
    public decimal CompressionRatio { get; set; }
    public int MatchCount { get; set; }
    public int PlayerStatCount { get; set; }
    public bool CompressionEnabled { get; set; }
    public string? CompressionAlgorithm { get; set; }
}

/// <summary>
/// Enhanced snapshot validation result with diagnostics
/// </summary>
public class SnapshotValidationResult
{
    public bool IsValid { get; set; }
    public bool Exists { get; set; }
    public bool DataIntact { get; set; }
    public bool CanDecompress { get; set; }
    public bool IntegrityCheckPassed { get; set; }
    public IEnumerable<string> ValidationErrors { get; set; } = new List<string>();
    public IEnumerable<string> ValidationWarnings { get; set; } = new List<string>();
    public long DataSizeBytes { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime ValidationPerformed { get; set; }
    public TimeSpan ValidationDuration { get; set; }
    public Dictionary<string, object> Diagnostics { get; set; } = new();
}

/// <summary>
/// Snapshot cleanup policy configuration
/// </summary>
public class SnapshotCleanupPolicy
{
    /// <summary>
    /// Delete snapshots older than this number of days
    /// </summary>
    public int RetentionDays { get; set; } = 30;
    
    /// <summary>
    /// Maximum number of snapshots to keep regardless of age
    /// </summary>
    public int MaxSnapshots { get; set; } = 50;
    
    /// <summary>
    /// Delete failed snapshots after this number of days
    /// </summary>
    public int FailedSnapshotRetentionDays { get; set; } = 7;
    
    /// <summary>
    /// Maximum total storage size in MB before cleanup triggers
    /// </summary>
    public long MaxTotalSizeMb { get; set; } = 5000;
    
    /// <summary>
    /// Whether to preserve snapshots associated with successful imports longer
    /// </summary>
    public bool PreserveImportSnapshots { get; set; } = true;
    
    /// <summary>
    /// Extended retention for import snapshots in days
    /// </summary>
    public int ImportSnapshotRetentionDays { get; set; } = 90;
}

/// <summary>
/// Result of snapshot cleanup operation
/// </summary>
public class SnapshotCleanupResult
{
    public int SnapshotsDeleted { get; set; }
    public long SpaceFreedBytes { get; set; }
    public TimeSpan CleanupDuration { get; set; }
    public IEnumerable<int> DeletedSnapshotIds { get; set; } = new List<int>();
    public IEnumerable<string> CleanupReasons { get; set; } = new List<string>();
    public bool PolicyTriggered { get; set; }
    public string? PolicyDescription { get; set; }
    public int SnapshotsRemaining { get; set; }
    public long RemainingStorageBytes { get; set; }
}

/// <summary>
/// Snapshot storage statistics for monitoring
/// </summary>
public class SnapshotStorageStats
{
    public int TotalSnapshots { get; set; }
    public int CompressedSnapshots { get; set; }
    public int UncompressedSnapshots { get; set; }
    public long TotalStorageBytes { get; set; }
    public long CompressedStorageBytes { get; set; }
    public long UncompressedStorageBytes { get; set; }
    public decimal AverageCompressionRatio { get; set; }
    public long PotentialSavingsBytes { get; set; }
    public DateTime OldestSnapshot { get; set; }
    public DateTime NewestSnapshot { get; set; }
    public IEnumerable<SnapshotStorageRecommendation> Recommendations { get; set; } = new List<SnapshotStorageRecommendation>();
    public Dictionary<string, int> SnapshotsByType { get; set; } = new();
}

/// <summary>
/// Storage optimization recommendation
/// </summary>
public class SnapshotStorageRecommendation
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public long PotentialSavingsBytes { get; set; }
    public string Priority { get; set; } = string.Empty; // Low, Medium, High, Critical
    public string? ActionRequired { get; set; }
}

/// <summary>
/// Result of snapshot compression operation
/// </summary>
public class SnapshotCompressionResult
{
    public int SnapshotsProcessed { get; set; }
    public int SnapshotsCompressed { get; set; }
    public int SnapshotsSkipped { get; set; }
    public long SpaceSavedBytes { get; set; }
    public TimeSpan CompressionDuration { get; set; }
    public decimal AverageCompressionRatio { get; set; }
    public IEnumerable<int> CompressedSnapshotIds { get; set; } = new List<int>();
    public IEnumerable<string> Errors { get; set; } = new List<string>();
    public string? CompressionAlgorithm { get; set; }
}