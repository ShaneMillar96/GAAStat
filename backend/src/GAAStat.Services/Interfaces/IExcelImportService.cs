using GAAStat.Services.Models;

namespace GAAStat.Services.Interfaces;

/// <summary>
/// Enhanced service for Excel import operations with advanced processing capabilities
/// Supports all 31 sheet types, parallel processing, bulk operations, and real-time progress tracking
/// </summary>
public interface IExcelImportService
{
    /// <summary>
    /// Imports GAA match statistics from Excel file with enhanced processing and progress tracking
    /// </summary>
    /// <param name="excelStream">Excel file stream</param>
    /// <param name="fileName">Original file name for audit trail</param>
    /// <param name="config">Bulk operation configuration for performance optimization</param>
    /// <param name="progressCallback">Optional callback for real-time progress updates</param>
    /// <returns>Import operation result with comprehensive summary statistics</returns>
    Task<ServiceResult<ImportSummary>> ImportMatchDataAsync(
        Stream excelStream, 
        string fileName, 
        BulkOperationConfig? config = null,
        IProgress<ImportProgress>? progressCallback = null);
    
    /// <summary>
    /// Performs comprehensive Excel file validation with cross-sheet consistency checks
    /// </summary>
    /// <param name="excelStream">Excel file stream</param>
    /// <param name="fileName">Original file name</param>
    /// <param name="performCrossSheetValidation">Whether to perform cross-sheet validation</param>
    /// <returns>Enhanced validation result with detailed cross-sheet analysis</returns>
    Task<ServiceResult<CrossSheetValidationResult>> ValidateExcelFileAsync(
        Stream excelStream, 
        string fileName, 
        bool performCrossSheetValidation = true);
    
    /// <summary>
    /// Processes all 31 sheet types with parallel processing and cumulative statistics
    /// </summary>
    /// <param name="excelStream">Excel file stream</param>
    /// <param name="fileName">Original file name</param>
    /// <param name="config">Processing configuration</param>
    /// <param name="progressCallback">Progress tracking callback</param>
    /// <returns>Detailed processing result for all sheet types</returns>
    Task<ServiceResult<ComprehensiveImportResult>> ProcessAllSheetTypesAsync(
        Stream excelStream, 
        string fileName,
        BulkOperationConfig? config = null,
        IProgress<ImportProgress>? progressCallback = null);
    
    /// <summary>
    /// Rolls back a completed import operation with automatic snapshot restoration
    /// </summary>
    /// <param name="importId">Import history ID to rollback</param>
    /// <param name="validateBeforeRollback">Whether to validate snapshot integrity before rollback</param>
    /// <returns>Rollback operation result with comprehensive metrics</returns>
    Task<ServiceResult<ImportSummary>> RollbackImportAsync(int importId, bool validateBeforeRollback = true);
    
    /// <summary>
    /// Gets current import progress for a running import operation
    /// </summary>
    /// <param name="importId">Import operation ID</param>
    /// <returns>Current progress information</returns>
    Task<ServiceResult<ImportProgress>> GetImportProgressAsync(int importId);
    
    /// <summary>
    /// Gets import history with enhanced filtering and performance metrics
    /// </summary>
    /// <param name="count">Number of records to return</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="includeMetrics">Whether to include performance metrics</param>
    /// <returns>List of import history records with enhanced details</returns>
    Task<ServiceResult<IEnumerable<ImportHistoryDto>>> GetImportHistoryAsync(
        int count = 20, 
        string? status = null, 
        bool includeMetrics = false);
    
    /// <summary>
    /// Cancels a running import operation gracefully
    /// </summary>
    /// <param name="importId">Import operation ID to cancel</param>
    /// <returns>Cancellation result with rollback status</returns>
    Task<ServiceResult<ImportCancellationResult>> CancelImportAsync(int importId);
    
    /// <summary>
    /// Gets import performance statistics for monitoring and optimization
    /// </summary>
    /// <param name="timeRange">Time range for statistics (in days)</param>
    /// <returns>Performance statistics and recommendations</returns>
    Task<ServiceResult<ImportPerformanceStats>> GetPerformanceStatisticsAsync(int timeRange = 30);
}