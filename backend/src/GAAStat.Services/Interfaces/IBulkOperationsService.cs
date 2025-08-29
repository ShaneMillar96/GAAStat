using GAAStat.Dal.Models.application;
using GAAStat.Services.Models;

namespace GAAStat.Services.Interfaces;

/// <summary>
/// Service for high-performance bulk database operations
/// Optimized for importing large datasets with minimal performance impact
/// </summary>
public interface IBulkOperationsService
{
    /// <summary>
    /// Performs bulk insert of matches with optimized performance
    /// </summary>
    /// <param name="matches">Collection of matches to insert</param>
    /// <param name="config">Bulk operation configuration</param>
    /// <param name="progressCallback">Optional progress tracking</param>
    /// <returns>Bulk operation result with performance metrics</returns>
    Task<ServiceResult<BulkOperationResult>> BulkInsertMatchesAsync(
        IEnumerable<Match> matches, 
        BulkOperationConfig? config = null,
        IProgress<BulkOperationProgress>? progressCallback = null);

    /// <summary>
    /// Performs bulk insert of player statistics with batch processing
    /// </summary>
    /// <param name="playerStats">Collection of player statistics to insert</param>
    /// <param name="config">Bulk operation configuration</param>
    /// <param name="progressCallback">Optional progress tracking</param>
    /// <returns>Bulk operation result with performance metrics</returns>
    Task<ServiceResult<BulkOperationResult>> BulkInsertPlayerStatsAsync(
        IEnumerable<MatchPlayerStat> playerStats, 
        BulkOperationConfig? config = null,
        IProgress<BulkOperationProgress>? progressCallback = null);

    /// <summary>
    /// Performs bulk upsert (insert or update) for matches
    /// </summary>
    /// <param name="matches">Collection of matches to upsert</param>
    /// <param name="config">Bulk operation configuration</param>
    /// <param name="progressCallback">Optional progress tracking</param>
    /// <returns>Bulk operation result with insert/update counts</returns>
    Task<ServiceResult<BulkUpsertResult>> BulkUpsertMatchesAsync(
        IEnumerable<Match> matches,
        BulkOperationConfig? config = null,
        IProgress<BulkOperationProgress>? progressCallback = null);

    /// <summary>
    /// Clears all match-related data using optimized bulk delete operations
    /// </summary>
    /// <param name="config">Bulk operation configuration</param>
    /// <returns>Clear operation result with performance metrics</returns>
    Task<ServiceResult<BulkClearResult>> BulkClearMatchDataAsync(BulkOperationConfig? config = null);

    /// <summary>
    /// Performs bulk insert of teams with duplicate detection
    /// </summary>
    /// <param name="teams">Collection of teams to insert</param>
    /// <param name="config">Bulk operation configuration</param>
    /// <returns>Bulk operation result with duplicate handling details</returns>
    Task<ServiceResult<BulkOperationResult>> BulkInsertTeamsAsync(
        IEnumerable<Team> teams, 
        BulkOperationConfig? config = null);

    /// <summary>
    /// Performs bulk insert of competitions with duplicate detection
    /// </summary>
    /// <param name="competitions">Collection of competitions to insert</param>
    /// <param name="config">Bulk operation configuration</param>
    /// <returns>Bulk operation result with duplicate handling details</returns>
    Task<ServiceResult<BulkOperationResult>> BulkInsertCompetitionsAsync(
        IEnumerable<Competition> competitions, 
        BulkOperationConfig? config = null);

    /// <summary>
    /// Gets current database connection pool statistics
    /// </summary>
    /// <returns>Connection pool performance metrics</returns>
    Task<ServiceResult<ConnectionPoolStats>> GetConnectionPoolStatsAsync();

    /// <summary>
    /// Optimizes database connection pool based on current workload
    /// </summary>
    /// <param name="expectedConcurrency">Expected concurrent operations</param>
    /// <returns>Pool optimization result</returns>
    Task<ServiceResult<PoolOptimizationResult>> OptimizeConnectionPoolAsync(int expectedConcurrency);
}