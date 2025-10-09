using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GAAStat.Dal.Contexts;
using GAAStat.Dal.Models;
using GAAStat.Dal.Models.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GAAStat.Services.ETL.Services;

/// <summary>
/// Maps position codes (GK, DEF, MID, FWD) to database position IDs.
/// Caches position mappings for performance.
/// </summary>
public class PositionDetectionService
{
    private readonly GAAStatDbContext _dbContext;
    private readonly ILogger<PositionDetectionService> _logger;

    // In-memory cache of position code → position ID
    private Dictionary<string, int>? _positionCache;

    public PositionDetectionService(
        GAAStatDbContext dbContext,
        ILogger<PositionDetectionService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets position ID for a position code.
    /// Uses cache to avoid repeated database queries.
    /// </summary>
    /// <param name="positionCode">Position code (GK, DEF, MID, FWD)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Position ID</returns>
    /// <exception cref="InvalidOperationException">If position code not found</exception>
    public async Task<int> GetPositionIdAsync(
        string positionCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(positionCode))
            throw new ArgumentException("Position code cannot be empty", nameof(positionCode));

        // Ensure cache is loaded
        await EnsureCacheLoadedAsync(cancellationToken);

        var normalizedCode = positionCode.Trim().ToUpperInvariant();

        if (_positionCache!.TryGetValue(normalizedCode, out var positionId))
        {
            return positionId;
        }

        throw new InvalidOperationException(
            $"Position code '{positionCode}' not found in database. " +
            $"Valid codes: {string.Join(", ", _positionCache.Keys)}");
    }

    /// <summary>
    /// Gets all position mappings for bulk operations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of position code → position ID</returns>
    public async Task<Dictionary<string, int>> GetAllPositionMappingsAsync(
        CancellationToken cancellationToken = default)
    {
        await EnsureCacheLoadedAsync(cancellationToken);
        return new Dictionary<string, int>(_positionCache!);
    }

    /// <summary>
    /// Validates that position code exists.
    /// </summary>
    /// <param name="positionCode">Position code to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if position exists</returns>
    public async Task<bool> PositionExistsAsync(
        string positionCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(positionCode))
            return false;

        await EnsureCacheLoadedAsync(cancellationToken);

        var normalizedCode = positionCode.Trim().ToUpperInvariant();
        return _positionCache!.ContainsKey(normalizedCode);
    }

    /// <summary>
    /// Gets position details by code.
    /// </summary>
    /// <param name="positionCode">Position code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Position entity or null if not found</returns>
    public async Task<Position?> GetPositionByCodeAsync(
        string positionCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(positionCode))
            return null;

        var normalizedCode = positionCode.Trim().ToUpperInvariant();

        return await _dbContext.Positions
            .Where(p => p.Code.ToUpper() == normalizedCode)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Refreshes the position cache from database.
    /// Call this if positions are added/updated during runtime.
    /// </summary>
    public async Task RefreshCacheAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Refreshing position cache from database");
        _positionCache = await LoadPositionCacheAsync(cancellationToken);
    }

    /// <summary>
    /// Ensures position cache is loaded (lazy initialization).
    /// </summary>
    private async Task EnsureCacheLoadedAsync(CancellationToken cancellationToken)
    {
        if (_positionCache == null)
        {
            _positionCache = await LoadPositionCacheAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Loads position mappings from database into cache.
    /// </summary>
    private async Task<Dictionary<string, int>> LoadPositionCacheAsync(
        CancellationToken cancellationToken)
    {
        var positions = await _dbContext.Positions
            .ToListAsync(cancellationToken);

        if (positions.Count == 0)
        {
            _logger.LogWarning("No positions found in database. Ensure seed data is loaded.");
        }

        var cache = positions.ToDictionary(
            p => p.Code.ToUpperInvariant(),
            p => p.PositionId);

        _logger.LogInformation("Loaded {Count} positions into cache: {Codes}",
            cache.Count,
            string.Join(", ", cache.Keys));

        return cache;
    }

    /// <summary>
    /// Validates and normalizes position code.
    /// Returns normalized code or throws if invalid.
    /// </summary>
    /// <param name="positionCode">Position code to validate</param>
    /// <returns>Normalized position code (uppercase)</returns>
    /// <exception cref="ArgumentException">If position code is invalid</exception>
    public string ValidateAndNormalizePositionCode(string positionCode)
    {
        if (string.IsNullOrWhiteSpace(positionCode))
            throw new ArgumentException("Position code cannot be empty", nameof(positionCode));

        var normalized = positionCode.Trim().ToUpperInvariant();

        // Validate against known codes (GK, DEF, MID, FWD)
        var validCodes = new[] { "GK", "DEF", "MID", "FWD" };

        if (!validCodes.Contains(normalized))
        {
            throw new ArgumentException(
                $"Invalid position code '{positionCode}'. Valid codes: {string.Join(", ", validCodes)}",
                nameof(positionCode));
        }

        return normalized;
    }
}
