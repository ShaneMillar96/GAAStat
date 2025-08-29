using GAAStat.Services.Models;

namespace GAAStat.Services.Interfaces;

/// <summary>
/// Enhanced service for managing import snapshots and rollback functionality
/// Supports automated pre-import snapshots, compression, and advanced rollback scenarios
/// </summary>
public interface IImportSnapshotService
{
    /// <summary>
    /// Creates a snapshot of current match data before import operation
    /// </summary>
    /// <param name="description">Description of the snapshot</param>
    /// <param name="compress">Whether to compress snapshot data</param>
    /// <returns>Snapshot ID for future rollback operations</returns>
    Task<ServiceResult<int>> CreateSnapshotAsync(string description, bool compress = true);
    
    /// <summary>
    /// Creates an automated pre-import snapshot with standardized naming
    /// </summary>
    /// <param name="fileName">Import file name for snapshot description</param>
    /// <param name="importType">Type of import operation</param>
    /// <returns>Snapshot creation result with performance metrics</returns>
    Task<ServiceResult<SnapshotCreationResult>> CreatePreImportSnapshotAsync(string fileName, string importType = "Excel");
    
    /// <summary>
    /// Restores database state from a specific snapshot with enhanced error handling
    /// </summary>
    /// <param name="snapshotId">Snapshot ID to restore from</param>
    /// <param name="validateIntegrity">Whether to validate data integrity before restoration</param>
    /// <returns>Restoration result with detailed metrics</returns>
    Task<ServiceResult<SnapshotRestoreResult>> RestoreFromSnapshotAsync(int snapshotId, bool validateIntegrity = true);
    
    /// <summary>
    /// Gets all available snapshots with enhanced metadata and filtering
    /// </summary>
    /// <param name="count">Number of snapshots to return</param>
    /// <param name="includeMetrics">Whether to include performance metrics</param>
    /// <returns>List of available snapshots with enhanced details</returns>
    Task<ServiceResult<IEnumerable<SnapshotSummary>>> GetSnapshotsAsync(int count = 10, bool includeMetrics = false);
    
    /// <summary>
    /// Deletes old snapshots based on configurable cleanup policies
    /// </summary>
    /// <param name="policy">Cleanup policy defining retention rules</param>
    /// <returns>Cleanup operation result with statistics</returns>
    Task<ServiceResult<SnapshotCleanupResult>> CleanupSnapshotsAsync(SnapshotCleanupPolicy? policy = null);
    
    /// <summary>
    /// Validates that a snapshot exists and is restorable with integrity checks
    /// </summary>
    /// <param name="snapshotId">Snapshot ID to validate</param>
    /// <param name="performIntegrityCheck">Whether to perform deep integrity validation</param>
    /// <returns>Enhanced validation result with detailed diagnostics</returns>
    Task<ServiceResult<SnapshotValidationResult>> ValidateSnapshotAsync(int snapshotId, bool performIntegrityCheck = false);
    
    /// <summary>
    /// Gets snapshot storage statistics for monitoring and maintenance
    /// </summary>
    /// <returns>Storage statistics including size, compression ratios, and recommendations</returns>
    Task<ServiceResult<SnapshotStorageStats>> GetStorageStatisticsAsync();
    
    /// <summary>
    /// Compresses existing uncompressed snapshots to save storage space
    /// </summary>
    /// <param name="snapshotId">Specific snapshot ID, or null to compress all uncompressed snapshots</param>
    /// <returns>Compression operation result with space savings</returns>
    Task<ServiceResult<SnapshotCompressionResult>> CompressSnapshotsAsync(int? snapshotId = null);
}