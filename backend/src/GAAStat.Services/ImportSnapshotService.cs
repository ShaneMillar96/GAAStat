using GAAStat.Dal.Interfaces;
using GAAStat.Dal.Models.application;
using GAAStat.Services.Interfaces;
using GAAStat.Services.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.IO.Compression;
using System.Text;
using System.Diagnostics;

namespace GAAStat.Services;

/// <summary>
/// Enhanced service for managing import snapshots with compression, cleanup policies, and advanced rollback functionality
/// </summary>
public class ImportSnapshotService : IImportSnapshotService
{
    private readonly IGAAStatDbContext _context;
    private readonly ILogger<ImportSnapshotService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() 
    {
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public ImportSnapshotService(IGAAStatDbContext context, ILogger<ImportSnapshotService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Creates a snapshot of current match data before import operation
    /// </summary>
    public async Task<ServiceResult<int>> CreateSnapshotAsync(string description, bool compress = true)
    {
        var operationId = Guid.NewGuid().ToString()[..8];
        var startTime = DateTime.Now;
        
        try
        {
            _logger.LogInformation("Creating import snapshot: {Description}, Compression: {Compress} [Operation: {OperationId}]", 
                description, compress, operationId);
            
            var snapshot = new ImportSnapshot
            {
                CreatedAt = startTime,
                TotalMatches = await _context.Matches.CountAsync(),
                TotalPlayerRecords = await _context.MatchPlayerStats.CountAsync()
            };

            // Create snapshot data as JSON
            var snapshotData = await CreateSnapshotDataAsync();
            var matchesJson = JsonSerializer.Serialize(snapshotData.Matches, JsonOptions);
            var playerStatsJson = JsonSerializer.Serialize(snapshotData.PlayerStats, JsonOptions);
            
            var uncompressedSize = Encoding.UTF8.GetByteCount(matchesJson) + Encoding.UTF8.GetByteCount(playerStatsJson);
            
            if (compress)
            {
                snapshot.MatchesData = CompressData(matchesJson);
                snapshot.PlayerStatsData = CompressData(playerStatsJson);
                var compressedSize = (snapshot.MatchesData?.Length ?? 0) + (snapshot.PlayerStatsData?.Length ?? 0);
                snapshot.SnapshotSizeMb = compressedSize / (1024m * 1024m);
                snapshot.IsCompressed = true;
                snapshot.CompressionRatio = uncompressedSize > 0 ? (decimal)compressedSize / uncompressedSize : 1m;
            }
            else
            {
                snapshot.MatchesData = matchesJson;
                snapshot.PlayerStatsData = playerStatsJson;
                snapshot.SnapshotSizeMb = uncompressedSize / (1024m * 1024m);
                snapshot.IsCompressed = false;
                snapshot.CompressionRatio = 1m;
            }

            var duration = DateTime.Now - startTime;
            snapshot.CreationDurationMs = (int)duration.TotalMilliseconds;

            _context.ImportSnapshots.Add(snapshot);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Import snapshot created successfully: ID {SnapshotId}, Matches: {MatchCount}, PlayerStats: {PlayerStatCount}, " +
                "Size: {Size:F2}MB, Compressed: {Compressed}, Ratio: {Ratio:F2}, Duration: {Duration} [Operation: {OperationId}]",
                snapshot.Id, snapshot.TotalMatches, snapshot.TotalPlayerRecords, snapshot.SnapshotSizeMb, 
                compress, snapshot.CompressionRatio, duration, operationId);

            return ServiceResult<int>.Success(snapshot.Id, operationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create import snapshot [Operation: {OperationId}]", operationId);
            return ServiceResult<int>.Failed($"Failed to create snapshot: {ex.Message}", operationId);
        }
    }

    /// <summary>
    /// Restores database state from a specific snapshot
    /// </summary>
    public async Task<ServiceResult<SnapshotRestoreResult>> RestoreFromSnapshotAsync(int snapshotId)
    {
        var operationId = Guid.NewGuid().ToString()[..8];
        
        try
        {
            _logger.LogInformation("Starting restore from snapshot {SnapshotId} [Operation: {OperationId}]", snapshotId, operationId);
            
            var snapshot = await _context.ImportSnapshots
                .FirstOrDefaultAsync(s => s.Id == snapshotId);

            if (snapshot == null)
            {
                _logger.LogWarning("Snapshot {SnapshotId} not found [Operation: {OperationId}]", snapshotId, operationId);
                return ServiceResult<SnapshotRestoreResult>.Failed("Snapshot not found", operationId);
            }

            if (string.IsNullOrEmpty(snapshot.MatchesData) && string.IsNullOrEmpty(snapshot.PlayerStatsData))
            {
                _logger.LogError("Snapshot {SnapshotId} has no data [Operation: {OperationId}]", snapshotId, operationId);
                return ServiceResult<SnapshotRestoreResult>.Failed("Snapshot data is empty", operationId);
            }

            var startTime = DateTime.Now;
            var result = new SnapshotRestoreResult
            {
                SnapshotId = snapshotId,
                Description = $"Snapshot {snapshotId}",
                SnapshotCreatedAt = snapshot.CreatedAt,
                RestoredAt = startTime
            };

            // Begin transaction for atomic restore
            using var transaction = await ((Microsoft.EntityFrameworkCore.DbContext)_context).Database.BeginTransactionAsync();
            
            try
            {
                // Clear current data
                _logger.LogInformation("Clearing current match data [Operation: {OperationId}]", operationId);
                await ClearMatchDataAsync();

                // Restore data from snapshot
                _logger.LogInformation("Restoring data from snapshot [Operation: {OperationId}]", operationId);
                
                var matches = new List<Match>();
                var playerStats = new List<MatchPlayerStat>();
                
                if (!string.IsNullOrEmpty(snapshot.MatchesData))
                {
                    var matchesData = JsonSerializer.Deserialize<List<Match>>(snapshot.MatchesData);
                    if (matchesData != null) matches = matchesData;
                }
                
                if (!string.IsNullOrEmpty(snapshot.PlayerStatsData))
                {
                    var playerStatsData = JsonSerializer.Deserialize<List<MatchPlayerStat>>(snapshot.PlayerStatsData);
                    if (playerStatsData != null) playerStats = playerStatsData;
                }
                
                var snapshotData = new SnapshotData { Matches = matches, PlayerStats = playerStats };
                await RestoreSnapshotDataAsync(snapshotData);
                result.MatchesRestored = matches.Count;
                result.PlayerStatsRestored = playerStats.Count;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                result.RestoreDuration = DateTime.Now - startTime;
                result.RestorationSuccessful = true;

                _logger.LogInformation("Snapshot restore completed successfully: {MatchesRestored} matches, {PlayerStatsRestored} player stats in {Duration} [Operation: {OperationId}]",
                    result.MatchesRestored, result.PlayerStatsRestored, result.RestoreDuration, operationId);

                return ServiceResult<SnapshotRestoreResult>.Success(result, operationId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                result.RestorationSuccessful = false;
                result.ErrorMessage = ex.Message;
                result.RestoreDuration = DateTime.Now - startTime;
                
                _logger.LogError(ex, "Snapshot restore failed and rolled back [Operation: {OperationId}]", operationId);
                return ServiceResult<SnapshotRestoreResult>.Failed($"Restore failed: {ex.Message}", operationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore from snapshot {SnapshotId} [Operation: {OperationId}]", snapshotId, operationId);
            return ServiceResult<SnapshotRestoreResult>.Failed($"Failed to restore snapshot: {ex.Message}", operationId);
        }
    }

    /// <summary>
    /// Gets all available snapshots with metadata
    /// </summary>
    public async Task<ServiceResult<IEnumerable<SnapshotSummary>>> GetSnapshotsAsync(int count = 10)
    {
        try
        {
            var snapshots = await _context.ImportSnapshots
                .OrderByDescending(s => s.CreatedAt)
                .Take(count)
                .Select(s => new SnapshotSummary
                {
                    SnapshotId = s.Id,
                    Description = $"Snapshot {s.Id}",
                    CreatedAt = s.CreatedAt,
                    MatchCount = s.TotalMatches ?? 0,
                    PlayerStatCount = s.TotalPlayerRecords ?? 0,
                    DataSizeBytes = (long)((s.SnapshotSizeMb ?? 0) * 1024 * 1024),
                    CanRestore = !string.IsNullOrEmpty(s.MatchesData) || !string.IsNullOrEmpty(s.PlayerStatsData)
                })
                .ToListAsync();

            return ServiceResult<IEnumerable<SnapshotSummary>>.Success(snapshots);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve snapshots");
            return ServiceResult<IEnumerable<SnapshotSummary>>.Failed($"Failed to retrieve snapshots: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes old snapshots to manage storage space
    /// </summary>
    public async Task<ServiceResult<int>> CleanupOldSnapshotsAsync(int olderThanDays = 30)
    {
        try
        {
            var cutoffDate = DateTime.Now.AddDays(-olderThanDays);
            
            var oldSnapshots = await _context.ImportSnapshots
                .Where(s => s.CreatedAt < cutoffDate)
                .ToListAsync();

            if (!oldSnapshots.Any())
            {
                _logger.LogInformation("No old snapshots found for cleanup (older than {Days} days)", olderThanDays);
                return ServiceResult<int>.Success(0);
            }

            _context.ImportSnapshots.RemoveRange(oldSnapshots);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {Count} old snapshots (older than {Days} days)", oldSnapshots.Count, olderThanDays);
            
            return ServiceResult<int>.Success(oldSnapshots.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old snapshots");
            return ServiceResult<int>.Failed($"Failed to cleanup snapshots: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates that a snapshot exists and is restorable
    /// </summary>
    public async Task<ServiceResult<bool>> ValidateSnapshotAsync(int snapshotId)
    {
        try
        {
            var snapshot = await _context.ImportSnapshots
                .Select(s => new { s.Id, s.MatchesData, s.PlayerStatsData })
                .FirstOrDefaultAsync(s => s.Id == snapshotId);

            if (snapshot == null)
            {
                return ServiceResult<bool>.Failed("Snapshot not found");
            }

            var isRestorable = !string.IsNullOrEmpty(snapshot.MatchesData) || !string.IsNullOrEmpty(snapshot.PlayerStatsData);
            
            if (!isRestorable)
            {
                return ServiceResult<bool>.Failed("Snapshot data is empty or corrupted");
            }

            // Try to deserialize to validate JSON structure
            try
            {
                if (!string.IsNullOrEmpty(snapshot.MatchesData))
                    JsonSerializer.Deserialize<List<Match>>(snapshot.MatchesData);
                    
                if (!string.IsNullOrEmpty(snapshot.PlayerStatsData))
                    JsonSerializer.Deserialize<List<MatchPlayerStat>>(snapshot.PlayerStatsData);
            }
            catch (JsonException)
            {
                return ServiceResult<bool>.Failed("Snapshot data is corrupted");
            }

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate snapshot {SnapshotId}", snapshotId);
            return ServiceResult<bool>.Failed($"Failed to validate snapshot: {ex.Message}");
        }
    }

    #region Private Helper Methods

    private async Task<SnapshotData> CreateSnapshotDataAsync()
    {
        var matches = await _context.Matches
            .Include(m => m.MatchPlayerStats)
            .ToListAsync();

        return new SnapshotData
        {
            CreatedAt = DateTime.Now,
            Matches = matches,
            PlayerStats = matches.SelectMany(m => m.MatchPlayerStats).ToList()
        };
    }

    private async Task ClearMatchDataAsync()
    {
        // Delete in correct order to respect foreign key constraints
        await ((Microsoft.EntityFrameworkCore.DbContext)_context).Database.ExecuteSqlRawAsync("DELETE FROM match_kickout_stats");
        await ((Microsoft.EntityFrameworkCore.DbContext)_context).Database.ExecuteSqlRawAsync("DELETE FROM match_player_stats");
        // match_source_analyses table removed from schema
        await ((Microsoft.EntityFrameworkCore.DbContext)_context).Database.ExecuteSqlRawAsync("DELETE FROM matches");
    }

    private async Task RestoreSnapshotDataAsync(SnapshotData snapshotData)
    {
        if (snapshotData.Matches?.Any() == true)
        {
            // Reset identity tracking to avoid conflicts
            foreach (var match in snapshotData.Matches)
            {
                match.ImportedAt = DateTime.Now;
                _context.Matches.Add(match);
            }
        }

        if (snapshotData.PlayerStats?.Any() == true)
        {
            foreach (var playerStat in snapshotData.PlayerStats)
            {
                playerStat.ImportedAt = DateTime.Now;
                _context.MatchPlayerStats.Add(playerStat);
            }
        }
    }

    /// <summary>
    /// Creates an automated pre-import snapshot with standardized naming
    /// </summary>
    public async Task<ServiceResult<SnapshotCreationResult>> CreatePreImportSnapshotAsync(string fileName, string importType = "Excel")
    {
        var operationId = Guid.NewGuid().ToString()[..8];
        var startTime = DateTime.Now;
        
        try
        {
            var description = $"Pre-{importType} import snapshot for {fileName}";
            _logger.LogInformation("Creating automated pre-import snapshot: {Description} [Operation: {OperationId}]", description, operationId);
            
            var snapshot = new ImportSnapshot
            {
                CreatedAt = startTime,
                Description = description,
                ImportType = importType,
                AssociatedFileName = fileName,
                TotalMatches = await _context.Matches.CountAsync(),
                TotalPlayerRecords = await _context.MatchPlayerStats.CountAsync()
            };

            // Create snapshot data as JSON with compression
            var snapshotData = await CreateSnapshotDataAsync();
            var matchesJson = JsonSerializer.Serialize(snapshotData.Matches, JsonOptions);
            var playerStatsJson = JsonSerializer.Serialize(snapshotData.PlayerStats, JsonOptions);
            
            var uncompressedSize = Encoding.UTF8.GetByteCount(matchesJson) + Encoding.UTF8.GetByteCount(playerStatsJson);
            
            // Always compress pre-import snapshots for efficiency
            snapshot.MatchesData = CompressData(matchesJson);
            snapshot.PlayerStatsData = CompressData(playerStatsJson);
            var compressedSize = (snapshot.MatchesData?.Length ?? 0) + (snapshot.PlayerStatsData?.Length ?? 0);
            
            snapshot.SnapshotSizeMb = compressedSize / (1024m * 1024m);
            snapshot.IsCompressed = true;
            snapshot.CompressionRatio = uncompressedSize > 0 ? (decimal)compressedSize / uncompressedSize : 1m;

            var duration = DateTime.Now - startTime;
            snapshot.CreationDurationMs = (int)duration.TotalMilliseconds;

            _context.ImportSnapshots.Add(snapshot);
            await _context.SaveChangesAsync();

            var result = new SnapshotCreationResult
            {
                SnapshotId = snapshot.Id,
                Description = description,
                CreatedAt = snapshot.CreatedAt,
                CreationDuration = duration,
                UncompressedSizeBytes = uncompressedSize,
                CompressedSizeBytes = compressedSize,
                CompressionRatio = snapshot.CompressionRatio ?? 0m,
                MatchCount = snapshot.TotalMatches ?? 0,
                PlayerStatCount = snapshot.TotalPlayerRecords ?? 0,
                CompressionEnabled = true,
                CompressionAlgorithm = "Brotli"
            };

            _logger.LogInformation("Automated pre-import snapshot created: ID {SnapshotId}, " +
                "Size: {CompressedSize:F2}MB (from {UncompressedSize:F2}MB), Ratio: {Ratio:F2}, Duration: {Duration} [Operation: {OperationId}]",
                snapshot.Id, compressedSize / (1024m * 1024m), uncompressedSize / (1024m * 1024m), 
                snapshot.CompressionRatio, duration, operationId);

            return ServiceResult<SnapshotCreationResult>.Success(result, operationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create pre-import snapshot [Operation: {OperationId}]", operationId);
            return ServiceResult<SnapshotCreationResult>.Failed($"Failed to create pre-import snapshot: {ex.Message}", operationId);
        }
    }

    /// <summary>
    /// Restores database state from a specific snapshot with enhanced error handling
    /// </summary>
    public async Task<ServiceResult<SnapshotRestoreResult>> RestoreFromSnapshotAsync(int snapshotId, bool validateIntegrity = true)
    {
        var operationId = Guid.NewGuid().ToString()[..8];
        
        try
        {
            _logger.LogInformation("Starting enhanced restore from snapshot {SnapshotId}, Validation: {ValidateIntegrity} [Operation: {OperationId}]", 
                snapshotId, validateIntegrity, operationId);

            if (validateIntegrity)
            {
                var validationResult = await ValidateSnapshotAsync(snapshotId, true);
                if (!validationResult.IsSuccess || !validationResult.Data!.IsValid)
                {
                    return ServiceResult<SnapshotRestoreResult>.Failed($"Snapshot validation failed: {validationResult.ErrorMessage}", operationId);
                }
            }
            
            var snapshot = await _context.ImportSnapshots
                .FirstOrDefaultAsync(s => s.Id == snapshotId);

            if (snapshot == null)
            {
                _logger.LogWarning("Snapshot {SnapshotId} not found [Operation: {OperationId}]", snapshotId, operationId);
                return ServiceResult<SnapshotRestoreResult>.Failed("Snapshot not found", operationId);
            }

            if (string.IsNullOrEmpty(snapshot.MatchesData) && string.IsNullOrEmpty(snapshot.PlayerStatsData))
            {
                _logger.LogError("Snapshot {SnapshotId} has no data [Operation: {OperationId}]", snapshotId, operationId);
                return ServiceResult<SnapshotRestoreResult>.Failed("Snapshot data is empty", operationId);
            }

            var startTime = DateTime.Now;
            var result = new SnapshotRestoreResult
            {
                SnapshotId = snapshotId,
                Description = snapshot.Description ?? $"Snapshot {snapshotId}",
                SnapshotCreatedAt = snapshot.CreatedAt,
                RestoredAt = startTime
            };

            // Begin transaction for atomic restore
            using var transaction = await ((Microsoft.EntityFrameworkCore.DbContext)_context).Database.BeginTransactionAsync();
            
            try
            {
                // Clear current data
                _logger.LogInformation("Clearing current match data [Operation: {OperationId}]", operationId);
                await ClearMatchDataAsync();

                // Restore data from snapshot
                _logger.LogInformation("Restoring data from snapshot [Operation: {OperationId}]", operationId);
                
                var matches = new List<Match>();
                var playerStats = new List<MatchPlayerStat>();
                
                if (!string.IsNullOrEmpty(snapshot.MatchesData))
                {
                    var matchesJson = snapshot.IsCompressed == true 
                        ? DecompressData(snapshot.MatchesData) 
                        : snapshot.MatchesData;
                    var matchesData = JsonSerializer.Deserialize<List<Match>>(matchesJson, JsonOptions);
                    if (matchesData != null) matches = matchesData;
                }
                
                if (!string.IsNullOrEmpty(snapshot.PlayerStatsData))
                {
                    var playerStatsJson = snapshot.IsCompressed == true 
                        ? DecompressData(snapshot.PlayerStatsData) 
                        : snapshot.PlayerStatsData;
                    var playerStatsData = JsonSerializer.Deserialize<List<MatchPlayerStat>>(playerStatsJson, JsonOptions);
                    if (playerStatsData != null) playerStats = playerStatsData;
                }
                
                var snapshotData = new SnapshotData { Matches = matches, PlayerStats = playerStats };
                await RestoreSnapshotDataAsync(snapshotData);
                result.MatchesRestored = matches.Count;
                result.PlayerStatsRestored = playerStats.Count;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                result.RestoreDuration = DateTime.Now - startTime;
                result.RestorationSuccessful = true;

                _logger.LogInformation("Enhanced snapshot restore completed successfully: {MatchesRestored} matches, " +
                    "{PlayerStatsRestored} player stats in {Duration} [Operation: {OperationId}]",
                    result.MatchesRestored, result.PlayerStatsRestored, result.RestoreDuration, operationId);

                return ServiceResult<SnapshotRestoreResult>.Success(result, operationId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                result.RestorationSuccessful = false;
                result.ErrorMessage = ex.Message;
                result.RestoreDuration = DateTime.Now - startTime;
                
                _logger.LogError(ex, "Enhanced snapshot restore failed and rolled back [Operation: {OperationId}]", operationId);
                return ServiceResult<SnapshotRestoreResult>.Failed($"Restore failed: {ex.Message}", operationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore from snapshot {SnapshotId} [Operation: {OperationId}]", snapshotId, operationId);
            return ServiceResult<SnapshotRestoreResult>.Failed($"Failed to restore snapshot: {ex.Message}", operationId);
        }
    }

    /// <summary>
    /// Gets all available snapshots with enhanced metadata and filtering
    /// </summary>
    public async Task<ServiceResult<IEnumerable<SnapshotSummary>>> GetSnapshotsAsync(int count = 10, bool includeMetrics = false)
    {
        try
        {
            var query = _context.ImportSnapshots.AsQueryable();

            if (includeMetrics)
            {
                // Include additional performance metrics when requested
                query = query.OrderByDescending(s => s.CreatedAt);
            }
            else
            {
                query = query.OrderByDescending(s => s.CreatedAt);
            }

            var snapshots = await query
                .Take(count)
                .Select(s => new SnapshotSummary
                {
                    SnapshotId = s.Id,
                    Description = s.Description ?? $"Snapshot {s.Id}",
                    CreatedAt = s.CreatedAt,
                    MatchCount = s.TotalMatches ?? 0,
                    PlayerStatCount = s.TotalPlayerRecords ?? 0,
                    DataSizeBytes = (long)((s.SnapshotSizeMb ?? 0) * 1024 * 1024),
                    IsCompressed = s.IsCompressed ?? false,
                    CompressionRatio = s.CompressionRatio ?? 1m,
                    CanRestore = !string.IsNullOrEmpty(s.MatchesData) || !string.IsNullOrEmpty(s.PlayerStatsData),
                    CreationDuration = s.CreationDurationMs.HasValue ? TimeSpan.FromMilliseconds((double)s.CreationDurationMs.Value) : null,
                    ImportType = s.ImportType ?? "Unknown",
                    AssociatedFileName = s.AssociatedFileName,
                    IntegrityVerified = s.LastValidated.HasValue && s.LastValidated > DateTime.Now.AddDays(-7),
                    LastValidated = s.LastValidated
                })
                .ToListAsync();

            return ServiceResult<IEnumerable<SnapshotSummary>>.Success(snapshots);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve enhanced snapshots");
            return ServiceResult<IEnumerable<SnapshotSummary>>.Failed($"Failed to retrieve snapshots: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes old snapshots based on configurable cleanup policies
    /// </summary>
    public async Task<ServiceResult<SnapshotCleanupResult>> CleanupSnapshotsAsync(SnapshotCleanupPolicy? policy = null)
    {
        var operationId = Guid.NewGuid().ToString()[..8];
        var startTime = DateTime.Now;
        
        // Use default policy if none provided
        policy ??= new SnapshotCleanupPolicy();
        
        try
        {
            _logger.LogInformation("Starting snapshot cleanup with policy: RetentionDays={RetentionDays}, " +
                "MaxSnapshots={MaxSnapshots}, MaxTotalSizeMb={MaxTotalSizeMb} [Operation: {OperationId}]",
                policy.RetentionDays, policy.MaxSnapshots, policy.MaxTotalSizeMb, operationId);

            var deleteCandidates = new List<ImportSnapshot>();
            var deletionReasons = new List<string>();

            // Apply age-based cleanup
            var ageCutoff = DateTime.Now.AddDays(-policy.RetentionDays);
            var oldSnapshots = await _context.ImportSnapshots
                .Where(s => s.CreatedAt < ageCutoff)
                .ToListAsync();

            if (oldSnapshots.Any())
            {
                deleteCandidates.AddRange(oldSnapshots);
                deletionReasons.Add($"Age-based cleanup: {oldSnapshots.Count} snapshots older than {policy.RetentionDays} days");
            }

            // Apply count-based cleanup
            var totalSnapshots = await _context.ImportSnapshots.CountAsync();
            if (totalSnapshots > policy.MaxSnapshots)
            {
                var excessCount = totalSnapshots - policy.MaxSnapshots;
                var oldestExcess = await _context.ImportSnapshots
                    .OrderBy(s => s.CreatedAt)
                    .Take(excessCount)
                    .Where(s => !deleteCandidates.Contains(s)) // Don't double-count
                    .ToListAsync();

                if (oldestExcess.Any())
                {
                    deleteCandidates.AddRange(oldestExcess);
                    deletionReasons.Add($"Count-based cleanup: {oldestExcess.Count} excess snapshots beyond limit of {policy.MaxSnapshots}");
                }
            }

            // Apply size-based cleanup
            var totalSize = await _context.ImportSnapshots.SumAsync(s => s.SnapshotSizeMb ?? 0);
            if (totalSize > policy.MaxTotalSizeMb)
            {
                var excessMb = totalSize - policy.MaxTotalSizeMb;
                var sizeCandidates = await _context.ImportSnapshots
                    .OrderBy(s => s.CreatedAt) // Delete oldest first for size management
                    .Where(s => !deleteCandidates.Contains(s)) // Don't double-count
                    .ToListAsync();

                decimal currentSize = 0;
                var sizeDeleteList = new List<ImportSnapshot>();
                foreach (var snapshot in sizeCandidates)
                {
                    sizeDeleteList.Add(snapshot);
                    currentSize += snapshot.SnapshotSizeMb ?? 0;
                    if (currentSize >= excessMb) break;
                }

                if (sizeDeleteList.Any())
                {
                    deleteCandidates.AddRange(sizeDeleteList);
                    deletionReasons.Add($"Size-based cleanup: {sizeDeleteList.Count} snapshots to free {currentSize:F2}MB");
                }
            }

            // Filter out import snapshots if policy preserves them
            if (policy.PreserveImportSnapshots)
            {
                var importSnapshotCutoff = DateTime.Now.AddDays(-policy.ImportSnapshotRetentionDays);
                deleteCandidates = deleteCandidates
                    .Where(s => !s.ImportType?.Equals("Excel", StringComparison.OrdinalIgnoreCase) == true || 
                               s.CreatedAt < importSnapshotCutoff)
                    .ToList();
            }

            // Remove duplicates
            deleteCandidates = deleteCandidates.Distinct().ToList();

            if (!deleteCandidates.Any())
            {
                _logger.LogInformation("No snapshots require cleanup [Operation: {OperationId}]", operationId);
                return ServiceResult<SnapshotCleanupResult>.Success(new SnapshotCleanupResult
                {
                    SnapshotsDeleted = 0,
                    SpaceFreedBytes = 0,
                    CleanupDuration = DateTime.Now - startTime,
                    DeletedSnapshotIds = new List<int>(),
                    CleanupReasons = new List<string> { "No cleanup required" },
                    PolicyTriggered = false,
                    SnapshotsRemaining = totalSnapshots,
                    RemainingStorageBytes = (long)(totalSize * 1024 * 1024)
                });
            }

            // Calculate space to be freed
            var spaceFreed = deleteCandidates.Sum(s => (s.SnapshotSizeMb ?? 0) * 1024 * 1024);

            // Perform deletion
            _context.ImportSnapshots.RemoveRange(deleteCandidates);
            await _context.SaveChangesAsync();

            var result = new SnapshotCleanupResult
            {
                SnapshotsDeleted = deleteCandidates.Count,
                SpaceFreedBytes = (long)spaceFreed,
                CleanupDuration = DateTime.Now - startTime,
                DeletedSnapshotIds = deleteCandidates.Select(s => s.Id).ToList(),
                CleanupReasons = deletionReasons,
                PolicyTriggered = true,
                PolicyDescription = $"Retention: {policy.RetentionDays}d, Max: {policy.MaxSnapshots}, Size: {policy.MaxTotalSizeMb}MB",
                SnapshotsRemaining = totalSnapshots - deleteCandidates.Count,
                RemainingStorageBytes = (long)((totalSize - deleteCandidates.Sum(s => s.SnapshotSizeMb ?? 0)) * 1024 * 1024)
            };

            _logger.LogInformation("Snapshot cleanup completed: {Deleted} snapshots deleted, " +
                "{SpaceFreed:F2}MB freed, {Remaining} remaining [Operation: {OperationId}]",
                result.SnapshotsDeleted, result.SpaceFreedBytes / (1024m * 1024m), result.SnapshotsRemaining, operationId);

            return ServiceResult<SnapshotCleanupResult>.Success(result, operationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup snapshots [Operation: {OperationId}]", operationId);
            return ServiceResult<SnapshotCleanupResult>.Failed($"Failed to cleanup snapshots: {ex.Message}", operationId);
        }
    }

    /// <summary>
    /// Validates that a snapshot exists and is restorable with integrity checks
    /// </summary>
    public async Task<ServiceResult<SnapshotValidationResult>> ValidateSnapshotAsync(int snapshotId, bool performIntegrityCheck = false)
    {
        var startTime = DateTime.Now;
        var result = new SnapshotValidationResult
        {
            ValidationPerformed = startTime
        };

        try
        {
            var snapshot = await _context.ImportSnapshots
                .FirstOrDefaultAsync(s => s.Id == snapshotId);

            if (snapshot == null)
            {
                result.Exists = false;
                result.IsValid = false;
                result.ValidationErrors = new[] { "Snapshot not found" };
                return ServiceResult<SnapshotValidationResult>.Success(result);
            }

            result.Exists = true;
            result.CreatedAt = snapshot.CreatedAt;
            result.DataSizeBytes = (long)((snapshot.SnapshotSizeMb ?? 0) * 1024 * 1024);

            var errors = new List<string>();
            var warnings = new List<string>();
            var diagnostics = new Dictionary<string, object>();

            // Basic data presence check
            result.DataIntact = !string.IsNullOrEmpty(snapshot.MatchesData) || !string.IsNullOrEmpty(snapshot.PlayerStatsData);
            if (!result.DataIntact)
            {
                errors.Add("Snapshot data is empty or missing");
            }

            // Compression check
            if (snapshot.IsCompressed == true)
            {
                try
                {
                    if (!string.IsNullOrEmpty(snapshot.MatchesData))
                    {
                        DecompressData(snapshot.MatchesData);
                        diagnostics["MatchesDataDecompressible"] = true;
                    }

                    if (!string.IsNullOrEmpty(snapshot.PlayerStatsData))
                    {
                        DecompressData(snapshot.PlayerStatsData);
                        diagnostics["PlayerStatsDataDecompressible"] = true;
                    }

                    result.CanDecompress = true;
                }
                catch (Exception ex)
                {
                    result.CanDecompress = false;
                    errors.Add($"Decompression failed: {ex.Message}");
                }
            }
            else
            {
                result.CanDecompress = true; // Not compressed
            }

            // JSON structure validation
            try
            {
                if (!string.IsNullOrEmpty(snapshot.MatchesData))
                {
                    var matchesJson = snapshot.IsCompressed == true 
                        ? DecompressData(snapshot.MatchesData) 
                        : snapshot.MatchesData;
                    var matches = JsonSerializer.Deserialize<List<Match>>(matchesJson, JsonOptions);
                    diagnostics["MatchesCount"] = matches?.Count ?? 0;
                }

                if (!string.IsNullOrEmpty(snapshot.PlayerStatsData))
                {
                    var playerStatsJson = snapshot.IsCompressed == true 
                        ? DecompressData(snapshot.PlayerStatsData) 
                        : snapshot.PlayerStatsData;
                    var playerStats = JsonSerializer.Deserialize<List<MatchPlayerStat>>(playerStatsJson, JsonOptions);
                    diagnostics["PlayerStatsCount"] = playerStats?.Count ?? 0;
                }
            }
            catch (JsonException ex)
            {
                errors.Add($"JSON structure validation failed: {ex.Message}");
            }

            // Integrity check if requested
            if (performIntegrityCheck && result.DataIntact && result.CanDecompress)
            {
                try
                {
                    // Verify data integrity by checking record counts match metadata
                    var actualMatchCount = 0;
                    var actualPlayerStatCount = 0;

                    if (!string.IsNullOrEmpty(snapshot.MatchesData))
                    {
                        var matchesJson = snapshot.IsCompressed == true 
                            ? DecompressData(snapshot.MatchesData) 
                            : snapshot.MatchesData;
                        var matches = JsonSerializer.Deserialize<List<Match>>(matchesJson, JsonOptions);
                        actualMatchCount = matches?.Count ?? 0;
                    }

                    if (!string.IsNullOrEmpty(snapshot.PlayerStatsData))
                    {
                        var playerStatsJson = snapshot.IsCompressed == true 
                            ? DecompressData(snapshot.PlayerStatsData) 
                            : snapshot.PlayerStatsData;
                        var playerStats = JsonSerializer.Deserialize<List<MatchPlayerStat>>(playerStatsJson, JsonOptions);
                        actualPlayerStatCount = playerStats?.Count ?? 0;
                    }

                    var expectedMatchCount = snapshot.TotalMatches ?? 0;
                    var expectedPlayerStatCount = snapshot.TotalPlayerRecords ?? 0;

                    if (actualMatchCount != expectedMatchCount)
                    {
                        warnings.Add($"Match count mismatch: expected {expectedMatchCount}, found {actualMatchCount}");
                    }

                    if (actualPlayerStatCount != expectedPlayerStatCount)
                    {
                        warnings.Add($"Player stat count mismatch: expected {expectedPlayerStatCount}, found {actualPlayerStatCount}");
                    }

                    result.IntegrityCheckPassed = actualMatchCount == expectedMatchCount && actualPlayerStatCount == expectedPlayerStatCount;
                    diagnostics["IntegrityCheck"] = "Performed";
                }
                catch (Exception ex)
                {
                    errors.Add($"Integrity check failed: {ex.Message}");
                    result.IntegrityCheckPassed = false;
                }
            }

            result.ValidationErrors = errors;
            result.ValidationWarnings = warnings;
            result.Diagnostics = diagnostics;
            result.IsValid = !errors.Any() && result.DataIntact && result.CanDecompress;
            result.ValidationDuration = DateTime.Now - startTime;

            // Update last validated timestamp
            if (result.IsValid)
            {
                snapshot.LastValidated = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return ServiceResult<SnapshotValidationResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate snapshot {SnapshotId}", snapshotId);
            result.IsValid = false;
            result.ValidationErrors = new[] { $"Validation failed: {ex.Message}" };
            result.ValidationDuration = DateTime.Now - startTime;
            return ServiceResult<SnapshotValidationResult>.Success(result);
        }
    }

    /// <summary>
    /// Gets snapshot storage statistics for monitoring and maintenance
    /// </summary>
    public async Task<ServiceResult<SnapshotStorageStats>> GetStorageStatisticsAsync()
    {
        try
        {
            var snapshots = await _context.ImportSnapshots.ToListAsync();
            
            if (!snapshots.Any())
            {
                return ServiceResult<SnapshotStorageStats>.Success(new SnapshotStorageStats
                {
                    TotalSnapshots = 0,
                    Recommendations = new[] { new SnapshotStorageRecommendation
                    {
                        Type = "Info",
                        Description = "No snapshots found",
                        Priority = "Low"
                    }}
                });
            }

            var compressed = snapshots.Where(s => s.IsCompressed == true).ToList();
            var uncompressed = snapshots.Where(s => s.IsCompressed != true).ToList();

            var totalStorage = snapshots.Sum(s => (s.SnapshotSizeMb ?? 0) * 1024 * 1024);
            var compressedStorage = compressed.Sum(s => (s.SnapshotSizeMb ?? 0) * 1024 * 1024);
            var uncompressedStorage = uncompressed.Sum(s => (s.SnapshotSizeMb ?? 0) * 1024 * 1024);

            // Estimate potential savings from compression
            var avgCompressionRatio = compressed.Any() ? compressed.Average(s => s.CompressionRatio ?? 1m) : 0.3m;
            var potentialSavings = uncompressed.Sum(s => (s.SnapshotSizeMb ?? 0) * (1 - avgCompressionRatio) * 1024 * 1024);

            var stats = new SnapshotStorageStats
            {
                TotalSnapshots = snapshots.Count,
                CompressedSnapshots = compressed.Count,
                UncompressedSnapshots = uncompressed.Count,
                TotalStorageBytes = (long)totalStorage,
                CompressedStorageBytes = (long)compressedStorage,
                UncompressedStorageBytes = (long)uncompressedStorage,
                AverageCompressionRatio = compressed.Any() ? compressed.Average(s => s.CompressionRatio ?? 1m) : 1m,
                PotentialSavingsBytes = (long)potentialSavings,
                OldestSnapshot = snapshots.Min(s => s.CreatedAt),
                NewestSnapshot = snapshots.Max(s => s.CreatedAt),
                SnapshotsByType = snapshots.GroupBy(s => s.ImportType ?? "Unknown")
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            // Generate recommendations
            var recommendations = new List<SnapshotStorageRecommendation>();

            if (uncompressed.Any())
            {
                recommendations.Add(new SnapshotStorageRecommendation
                {
                    Type = "Compression",
                    Description = $"Compress {uncompressed.Count} uncompressed snapshots to save approximately {potentialSavings / (1024 * 1024):F2}MB",
                    PotentialSavingsBytes = (long)potentialSavings,
                    Priority = potentialSavings > 100 * 1024 * 1024 ? "High" : "Medium",
                    ActionRequired = "Run CompressSnapshotsAsync()"
                });
            }

            var oldSnapshots = snapshots.Where(s => s.CreatedAt < DateTime.Now.AddDays(-30)).Count();
            if (oldSnapshots > 10)
            {
                recommendations.Add(new SnapshotStorageRecommendation
                {
                    Type = "Cleanup",
                    Description = $"{oldSnapshots} snapshots are older than 30 days",
                    Priority = oldSnapshots > 50 ? "High" : "Medium",
                    ActionRequired = "Run CleanupSnapshotsAsync() with appropriate policy"
                });
            }

            if (totalStorage > 1024 * 1024 * 1024) // > 1GB
            {
                recommendations.Add(new SnapshotStorageRecommendation
                {
                    Type = "Storage",
                    Description = $"Total snapshot storage is {totalStorage / (1024 * 1024 * 1024):F2}GB",
                    Priority = totalStorage > 5L * 1024 * 1024 * 1024 ? "Critical" : "High",
                    ActionRequired = "Consider implementing automatic cleanup policies"
                });
            }

            stats.Recommendations = recommendations;

            return ServiceResult<SnapshotStorageStats>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get storage statistics");
            return ServiceResult<SnapshotStorageStats>.Failed($"Failed to get storage statistics: {ex.Message}");
        }
    }

    /// <summary>
    /// Compresses existing uncompressed snapshots to save storage space
    /// </summary>
    public async Task<ServiceResult<SnapshotCompressionResult>> CompressSnapshotsAsync(int? snapshotId = null)
    {
        var operationId = Guid.NewGuid().ToString()[..8];
        var startTime = DateTime.Now;
        
        try
        {
            _logger.LogInformation("Starting snapshot compression operation: SnapshotId={SnapshotId} [Operation: {OperationId}]", 
                snapshotId?.ToString() ?? "All", operationId);

            List<ImportSnapshot> snapshots;
            
            if (snapshotId.HasValue)
            {
                var snapshot = await _context.ImportSnapshots
                    .FirstOrDefaultAsync(s => s.Id == snapshotId.Value);
                
                if (snapshot == null)
                {
                    return ServiceResult<SnapshotCompressionResult>.Failed($"Snapshot {snapshotId} not found", operationId);
                }
                
                snapshots = new List<ImportSnapshot> { snapshot };
            }
            else
            {
                snapshots = await _context.ImportSnapshots
                    .Where(s => s.IsCompressed != true && 
                               (!string.IsNullOrEmpty(s.MatchesData) || !string.IsNullOrEmpty(s.PlayerStatsData)))
                    .ToListAsync();
            }

            if (!snapshots.Any())
            {
                return ServiceResult<SnapshotCompressionResult>.Success(new SnapshotCompressionResult
                {
                    SnapshotsProcessed = 0,
                    SnapshotsCompressed = 0,
                    CompressionDuration = TimeSpan.Zero,
                    CompressionAlgorithm = "Brotli"
                });
            }

            var result = new SnapshotCompressionResult
            {
                SnapshotsProcessed = snapshots.Count,
                CompressionAlgorithm = "Brotli"
            };

            var compressed = new List<int>();
            var skipped = new List<int>();
            var errors = new List<string>();
            long totalSpaceSaved = 0;
            var compressionRatios = new List<decimal>();

            foreach (var snapshot in snapshots)
            {
                try
                {
                    if (snapshot.IsCompressed == true)
                    {
                        skipped.Add(snapshot.Id);
                        continue;
                    }

                    var originalSize = 0L;
                    var compressedSize = 0L;

                    // Compress matches data
                    if (!string.IsNullOrEmpty(snapshot.MatchesData))
                    {
                        originalSize += Encoding.UTF8.GetByteCount(snapshot.MatchesData);
                        snapshot.MatchesData = CompressData(snapshot.MatchesData);
                        compressedSize += snapshot.MatchesData.Length;
                    }

                    // Compress player stats data
                    if (!string.IsNullOrEmpty(snapshot.PlayerStatsData))
                    {
                        originalSize += Encoding.UTF8.GetByteCount(snapshot.PlayerStatsData);
                        snapshot.PlayerStatsData = CompressData(snapshot.PlayerStatsData);
                        compressedSize += snapshot.PlayerStatsData.Length;
                    }

                    // Update snapshot metadata
                    snapshot.IsCompressed = true;
                    snapshot.CompressionRatio = originalSize > 0 ? (decimal)compressedSize / originalSize : 1m;
                    snapshot.SnapshotSizeMb = compressedSize / (1024m * 1024m);

                    var spaceSaved = originalSize - compressedSize;
                    totalSpaceSaved += spaceSaved;
                    compressionRatios.Add(snapshot.CompressionRatio ?? 0m);
                    compressed.Add(snapshot.Id);

                    _logger.LogDebug("Compressed snapshot {SnapshotId}: {OriginalSize:F2}MB -> {CompressedSize:F2}MB " +
                        "(ratio: {Ratio:F2}) [Operation: {OperationId}]",
                        snapshot.Id, originalSize / (1024m * 1024m), compressedSize / (1024m * 1024m), 
                        snapshot.CompressionRatio, operationId);
                }
                catch (Exception ex)
                {
                    errors.Add($"Snapshot {snapshot.Id}: {ex.Message}");
                    skipped.Add(snapshot.Id);
                    _logger.LogWarning(ex, "Failed to compress snapshot {SnapshotId} [Operation: {OperationId}]", 
                        snapshot.Id, operationId);
                }
            }

            if (compressed.Any())
            {
                await _context.SaveChangesAsync();
            }

            result.SnapshotsCompressed = compressed.Count;
            result.SnapshotsSkipped = skipped.Count;
            result.SpaceSavedBytes = totalSpaceSaved;
            result.CompressionDuration = DateTime.Now - startTime;
            result.AverageCompressionRatio = compressionRatios.Any() ? compressionRatios.Average() : 1m;
            result.CompressedSnapshotIds = compressed;
            result.Errors = errors;

            _logger.LogInformation("Snapshot compression completed: {Compressed} compressed, {Skipped} skipped, " +
                "{SpaceSaved:F2}MB saved, Average ratio: {AvgRatio:F2} [Operation: {OperationId}]",
                result.SnapshotsCompressed, result.SnapshotsSkipped, result.SpaceSavedBytes / (1024m * 1024m),
                result.AverageCompressionRatio, operationId);

            return ServiceResult<SnapshotCompressionResult>.Success(result, operationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compress snapshots [Operation: {OperationId}]", operationId);
            return ServiceResult<SnapshotCompressionResult>.Failed($"Failed to compress snapshots: {ex.Message}", operationId);
        }
    }

    #endregion

    #region Private Helper Methods (Compression)
    
    private static string CompressData(string data)
    {
        var bytes = Encoding.UTF8.GetBytes(data);
        using var input = new MemoryStream(bytes);
        using var output = new MemoryStream();
        using (var brotli = new BrotliStream(output, CompressionMode.Compress))
        {
            input.CopyTo(brotli);
        }
        return Convert.ToBase64String(output.ToArray());
    }

    private static string DecompressData(string compressedData)
    {
        var bytes = Convert.FromBase64String(compressedData);
        using var input = new MemoryStream(bytes);
        using var output = new MemoryStream();
        using (var brotli = new BrotliStream(input, CompressionMode.Decompress))
        {
            brotli.CopyTo(output);
        }
        return Encoding.UTF8.GetString(output.ToArray());
    }

    #endregion

    #region Private Models

    private class SnapshotData
    {
        public DateTime CreatedAt { get; set; }
        public List<Match>? Matches { get; set; }
        public List<MatchPlayerStat>? PlayerStats { get; set; }
    }

    #endregion
}