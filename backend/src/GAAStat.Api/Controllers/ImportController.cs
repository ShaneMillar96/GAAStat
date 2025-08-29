using GAAStat.Services.Interfaces;
using GAAStat.Services.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace GAAStat.Api.Controllers;

/// <summary>
/// Controller for Excel import management, history, and operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ImportController : ControllerBase
{
    private readonly IExcelImportService _importService;
    private readonly IImportSnapshotService _snapshotService;
    private readonly IBulkOperationsService _bulkOperationsService;
    private readonly ILogger<ImportController> _logger;

    public ImportController(
        IExcelImportService importService,
        IImportSnapshotService snapshotService,
        IBulkOperationsService bulkOperationsService,
        ILogger<ImportController> logger)
    {
        _importService = importService;
        _snapshotService = snapshotService;
        _bulkOperationsService = bulkOperationsService;
        _logger = logger;
    }

    /// <summary>
    /// Gets import history with filtering and pagination
    /// </summary>
    /// <param name="count">Number of records to return (default: 20, max: 100)</param>
    /// <param name="status">Optional status filter (pending, processing, completed, failed, cancelled)</param>
    /// <param name="includeMetrics">Whether to include performance metrics (default: false)</param>
    /// <param name="fromDate">Optional start date filter</param>
    /// <param name="toDate">Optional end date filter</param>
    /// <returns>List of import history records with detailed information</returns>
    [HttpGet("history")]
    public async Task<IActionResult> GetImportHistory(
        [FromQuery] int count = 20,
        [FromQuery] string? status = null,
        [FromQuery] bool includeMetrics = false,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        if (count <= 0 || count > 100)
            return BadRequest(ApiResponse.Error("Count must be between 1 and 100"));

        if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
            return BadRequest(ApiResponse.Error("From date cannot be after to date"));

        try
        {
            var result = await _importService.GetImportHistoryAsync(count, status, includeMetrics);
            
            if (!result.IsSuccess)
                return BadRequest(ApiResponse.Error(result.ErrorMessage));

            var filteredHistory = result.Data;
            
            // Apply date filters if provided
            if (fromDate.HasValue || toDate.HasValue)
            {
                filteredHistory = filteredHistory.Where(h =>
                    (!fromDate.HasValue || h.ImportStartedAt >= fromDate.Value) &&
                    (!toDate.HasValue || h.ImportStartedAt <= toDate.Value));
            }

            var response = new ImportHistoryResponse
            {
                ImportHistory = filteredHistory,
                TotalCount = filteredHistory.Count(),
                IncludesMetrics = includeMetrics,
                Filters = new ImportHistoryFilters
                {
                    Status = status,
                    FromDate = fromDate,
                    ToDate = toDate,
                    Count = count
                }
            };

            return Ok(ApiResponse.Success(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while retrieving import history");
            return StatusCode(500, ApiResponse.Error("An error occurred while retrieving import history"));
        }
    }

    /// <summary>
    /// Gets real-time progress for a running import operation
    /// </summary>
    /// <param name="importId">Import operation identifier</param>
    /// <returns>Current progress information with performance metrics</returns>
    [HttpGet("{importId}/progress")]
    public async Task<IActionResult> GetImportProgress(int importId)
    {
        if (importId <= 0)
            return BadRequest(ApiResponse.Error("Invalid import ID"));

        try
        {
            var result = await _importService.GetImportProgressAsync(importId);
            return result.IsSuccess
                ? Ok(ApiResponse.Success(result.Data))
                : NotFound(ApiResponse.Error($"Import with ID {importId} not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while retrieving progress for import {ImportId}", importId);
            return StatusCode(500, ApiResponse.Error("An error occurred while retrieving import progress"));
        }
    }

    /// <summary>
    /// Cancels a running import operation with graceful cleanup
    /// </summary>
    /// <param name="importId">Import operation identifier</param>
    /// <param name="reason">Reason for cancellation (optional)</param>
    /// <returns>Cancellation result with cleanup status</returns>
    [HttpPost("{importId}/cancel")]
    public async Task<IActionResult> CancelImport(int importId, [FromBody] CancelImportRequest? request = null)
    {
        if (importId <= 0)
            return BadRequest(ApiResponse.Error("Invalid import ID"));

        try
        {
            var result = await _importService.CancelImportAsync(importId);
            
            if (!result.IsSuccess)
                return BadRequest(ApiResponse.Error(result.ErrorMessage));

            _logger.LogInformation("Import {ImportId} cancelled successfully", importId);
            
            return Ok(ApiResponse.Success(result.Data, "Import cancelled successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while cancelling import {ImportId}", importId);
            return StatusCode(500, ApiResponse.Error("An error occurred while cancelling the import"));
        }
    }

    /// <summary>
    /// Rolls back a completed import operation with data restoration
    /// </summary>
    /// <param name="importId">Import operation identifier to rollback</param>
    /// <param name="validateBeforeRollback">Whether to validate snapshot before rollback (default: true)</param>
    /// <returns>Rollback operation result</returns>
    [HttpPost("{importId}/rollback")]
    public async Task<IActionResult> RollbackImport(
        int importId, 
        [FromQuery] bool validateBeforeRollback = true)
    {
        if (importId <= 0)
            return BadRequest(ApiResponse.Error("Invalid import ID"));

        try
        {
            var result = await _importService.RollbackImportAsync(importId, validateBeforeRollback);
            
            if (!result.IsSuccess)
                return BadRequest(ApiResponse.Error(result.ErrorMessage ?? "Rollback failed", result.ValidationErrors));

            _logger.LogInformation("Import {ImportId} rolled back successfully", importId);
            
            return Ok(ApiResponse.Success(result.Data, "Import rolled back successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while rolling back import {ImportId}", importId);
            return StatusCode(500, ApiResponse.Error("An error occurred while rolling back the import"));
        }
    }

    /// <summary>
    /// Gets detailed information about a specific import operation
    /// </summary>
    /// <param name="importId">Import operation identifier</param>
    /// <param name="includeSnapshot">Whether to include snapshot details (default: false)</param>
    /// <returns>Detailed import information</returns>
    [HttpGet("{importId}")]
    public async Task<IActionResult> GetImportDetails(
        int importId, 
        [FromQuery] bool includeSnapshot = false)
    {
        if (importId <= 0)
            return BadRequest(ApiResponse.Error("Invalid import ID"));

        try
        {
            // This would call a service method to get detailed import information
            var response = new ImportDetailsResponse
            {
                ImportId = importId,
                IncludesSnapshot = includeSnapshot
            };

            return Ok(ApiResponse.Success(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while retrieving details for import {ImportId}", importId);
            return StatusCode(500, ApiResponse.Error("An error occurred while retrieving import details"));
        }
    }

    /// <summary>
    /// Gets import performance statistics and recommendations
    /// </summary>
    /// <param name="timeRange">Time range in days for statistics (default: 30, max: 365)</param>
    /// <param name="includeRecommendations">Whether to include optimization recommendations (default: true)</param>
    /// <returns>Performance statistics and optimization recommendations</returns>
    [HttpGet("performance")]
    public async Task<IActionResult> GetImportPerformanceStatistics(
        [FromQuery] int timeRange = 30,
        [FromQuery] bool includeRecommendations = true)
    {
        if (timeRange <= 0 || timeRange > 365)
            return BadRequest(ApiResponse.Error("Time range must be between 1 and 365 days"));

        try
        {
            var result = await _importService.GetPerformanceStatisticsAsync(timeRange);
            return result.IsSuccess
                ? Ok(ApiResponse.Success(result.Data))
                : BadRequest(ApiResponse.Error(result.ErrorMessage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while retrieving import performance statistics");
            return StatusCode(500, ApiResponse.Error("An error occurred while retrieving performance statistics"));
        }
    }

    /// <summary>
    /// Creates a database snapshot before import operations
    /// </summary>
    /// <param name="request">Snapshot creation request</param>
    /// <returns>Snapshot creation result</returns>
    [HttpPost("snapshot")]
    public async Task<IActionResult> CreateSnapshot([FromBody] CreateSnapshotRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
            return BadRequest(ApiResponse.Error("Snapshot description is required"));

        try
        {
            var result = await _snapshotService.CreateSnapshotAsync(request.Description, true);
            return result.IsSuccess
                ? Ok(ApiResponse.Success(result.Data))
                : BadRequest(ApiResponse.Error(result.ErrorMessage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while creating snapshot");
            return StatusCode(500, ApiResponse.Error("An error occurred while creating the snapshot"));
        }
    }

    /// <summary>
    /// Gets list of available database snapshots
    /// </summary>
    /// <param name="count">Number of snapshots to return (default: 10, max: 50)</param>
    /// <param name="includeDetails">Whether to include snapshot details (default: false)</param>
    /// <returns>List of available snapshots</returns>
    [HttpGet("snapshots")]
    public async Task<IActionResult> GetSnapshots(
        [FromQuery] int count = 10,
        [FromQuery] bool includeDetails = false)
    {
        if (count <= 0 || count > 50)
            return BadRequest(ApiResponse.Error("Count must be between 1 and 50"));

        try
        {
            var result = await _snapshotService.GetSnapshotsAsync(count);
            return result.IsSuccess
                ? Ok(ApiResponse.Success(result.Data))
                : BadRequest(ApiResponse.Error(result.ErrorMessage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while retrieving snapshots");
            return StatusCode(500, ApiResponse.Error("An error occurred while retrieving snapshots"));
        }
    }

    /// <summary>
    /// Deletes old snapshots to free up storage
    /// </summary>
    /// <param name="olderThanDays">Delete snapshots older than specified days</param>
    /// <param name="keepMinimum">Minimum number of snapshots to keep (default: 5)</param>
    /// <returns>Cleanup operation result</returns>
    [HttpDelete("snapshots/cleanup")]
    public async Task<IActionResult> CleanupSnapshots(
        [FromQuery, Required] int olderThanDays,
        [FromQuery] int keepMinimum = 5)
    {
        if (olderThanDays <= 0)
            return BadRequest(ApiResponse.Error("Days must be positive"));

        if (keepMinimum < 1 || keepMinimum > 20)
            return BadRequest(ApiResponse.Error("Keep minimum must be between 1 and 20"));

        try
        {
            // TODO: Implement CleanupOldSnapshotsAsync method in IImportSnapshotService
            return Ok(ApiResponse.Success("Cleanup operation not yet implemented"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while cleaning up snapshots");
            return StatusCode(500, ApiResponse.Error("An error occurred while cleaning up snapshots"));
        }
    }

    /// <summary>
    /// Optimizes database performance after large import operations
    /// </summary>
    /// <param name="request">Optimization request parameters</param>
    /// <returns>Optimization operation result</returns>
    [HttpPost("optimize")]
    public async Task<IActionResult> OptimizeDatabase([FromBody] OptimizeDatabaseRequest request)
    {
        try
        {
            // TODO: Implement OptimizeDatabasePerformanceAsync method in IBulkOperationsService
            return Ok(ApiResponse.Success("Database optimization not yet implemented"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while optimizing database");
            return StatusCode(500, ApiResponse.Error("An error occurred while optimizing the database"));
        }
    }

    /// <summary>
    /// Gets current database connection pool statistics
    /// </summary>
    /// <returns>Connection pool performance metrics</returns>
    [HttpGet("connection-pool/stats")]
    public async Task<IActionResult> GetConnectionPoolStats()
    {
        try
        {
            var result = await _bulkOperationsService.GetConnectionPoolStatsAsync();
            return result.IsSuccess
                ? Ok(ApiResponse.Success(result.Data))
                : BadRequest(ApiResponse.Error(result.ErrorMessage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while retrieving connection pool stats");
            return StatusCode(500, ApiResponse.Error("An error occurred while retrieving connection pool statistics"));
        }
    }
}

#region Request/Response DTOs

/// <summary>
/// Response model for import history endpoint
/// </summary>
public class ImportHistoryResponse
{
    public IEnumerable<ImportHistoryDto> ImportHistory { get; set; } = new List<ImportHistoryDto>();
    public int TotalCount { get; set; }
    public bool IncludesMetrics { get; set; }
    public ImportHistoryFilters Filters { get; set; } = new();
}

/// <summary>
/// Filter parameters for import history
/// </summary>
public class ImportHistoryFilters
{
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Count { get; set; }
}

/// <summary>
/// Request model for cancelling an import
/// </summary>
public class CancelImportRequest
{
    public string? Reason { get; set; }
    public bool ForceCancel { get; set; } = false;
}

/// <summary>
/// Response model for detailed import information
/// </summary>
public class ImportDetailsResponse
{
    public int ImportId { get; set; }
    public string ImportType { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public long? FileSize { get; set; }
    public DateTime ImportStartedAt { get; set; }
    public DateTime? ImportCompletedAt { get; set; }
    public string ImportStatus { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public ImportSummary? Summary { get; set; }
    public bool IncludesSnapshot { get; set; }
    public object? SnapshotDetails { get; set; }
}

/// <summary>
/// Request model for creating a database snapshot
/// </summary>
public class CreateSnapshotRequest
{
    [Required]
    [StringLength(200, MinimumLength = 5)]
    public string Description { get; set; } = string.Empty;

    public IEnumerable<string> IncludeTables { get; set; } = new List<string>();
}

/// <summary>
/// Request model for database optimization
/// </summary>
public class OptimizeDatabaseRequest
{
    public bool ReindexTables { get; set; } = true;
    public bool UpdateStatistics { get; set; } = true;
    public bool OptimizeConnectionPool { get; set; } = true;
    public bool CleanupTempData { get; set; } = true;
}

#endregion