using GAAStat.Services.Models;

namespace GAAStat.Services.Interfaces;

/// <summary>
/// Service for tracking ETL job progress and updating database records
/// Manages EtlJobProgress and EtlValidationError entities
/// </summary>
public interface IProgressTrackingService
{
    /// <summary>
    /// Creates initial ETL job record in database
    /// </summary>
    /// <param name="fileName">Original Excel file name</param>
    /// <param name="fileSizeBytes">File size in bytes</param>
    /// <param name="createdBy">User or system that initiated the job</param>
    /// <returns>Created job ID</returns>
    Task<ServiceResult<int>> CreateEtlJobAsync(string fileName, long fileSizeBytes, string? createdBy = null);
    
    /// <summary>
    /// Updates ETL job status (pending, processing, completed, failed)
    /// </summary>
    /// <param name="jobId">ETL job identifier</param>
    /// <param name="status">New job status</param>
    /// <param name="errorSummary">High-level error summary if job failed</param>
    /// <returns>Success or failure result</returns>
    Task<ServiceResult> UpdateEtlJobStatusAsync(int jobId, string status, string? errorSummary = null);
    
    /// <summary>
    /// Records detailed progress update for current processing stage
    /// </summary>
    /// <param name="progressUpdate">Progress information including stage, steps, and messages</param>
    /// <returns>Success or failure result</returns>
    Task<ServiceResult> RecordProgressUpdateAsync(EtlProgressUpdate progressUpdate);
    
    /// <summary>
    /// Records validation error encountered during ETL processing
    /// </summary>
    /// <param name="jobId">ETL job identifier</param>
    /// <param name="sheetName">Excel sheet where error occurred</param>
    /// <param name="rowNumber">Row number where error was found</param>
    /// <param name="columnName">Column name where error occurred</param>
    /// <param name="errorType">Category of validation error</param>
    /// <param name="errorMessage">Detailed error description</param>
    /// <param name="suggestedFix">Recommended action to resolve error</param>
    /// <returns>Success or failure result</returns>
    Task<ServiceResult> RecordValidationErrorAsync(
        int jobId,
        string? sheetName,
        int? rowNumber,
        string? columnName,
        string errorType,
        string errorMessage,
        string? suggestedFix = null);
    
    /// <summary>
    /// Records multiple validation errors in a single database operation
    /// </summary>
    /// <param name="jobId">ETL job identifier</param>
    /// <param name="validationErrors">Collection of validation errors</param>
    /// <returns>Success or failure result</returns>
    Task<ServiceResult> RecordValidationErrorsAsync(int jobId, IEnumerable<ValidationError> validationErrors);
    
    /// <summary>
    /// Retrieves current progress status for an ETL job
    /// </summary>
    /// <param name="jobId">ETL job identifier</param>
    /// <returns>Latest progress information</returns>
    Task<ServiceResult<EtlProgressUpdate?>> GetCurrentProgressAsync(int jobId);
    
    /// <summary>
    /// Marks ETL job as started and records start timestamp
    /// </summary>
    /// <param name="jobId">ETL job identifier</param>
    /// <returns>Success or failure result</returns>
    Task<ServiceResult> MarkJobStartedAsync(int jobId);
    
    /// <summary>
    /// Marks ETL job as completed and records completion timestamp
    /// </summary>
    /// <param name="jobId">ETL job identifier</param>
    /// <returns>Success or failure result</returns>
    Task<ServiceResult> MarkJobCompletedAsync(int jobId);
}

/// <summary>
/// Validation error information for batch recording
/// </summary>
public class ValidationError
{
    public string? SheetName { get; set; }
    public int? RowNumber { get; set; }
    public string? ColumnName { get; set; }
    public string ErrorType { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string? SuggestedFix { get; set; }
}