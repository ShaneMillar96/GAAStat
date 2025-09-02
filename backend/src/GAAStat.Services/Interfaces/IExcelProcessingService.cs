using GAAStat.Services.Models;

namespace GAAStat.Services.Interfaces;

/// <summary>
/// Main service for orchestrating Excel ETL processing operations
/// Coordinates parsing, validation, and database operations for GAA match data
/// </summary>
public interface IExcelProcessingService
{
    /// <summary>
    /// Processes uploaded Excel file through complete ETL pipeline
    /// Phase 1: Creates match headers with basic information
    /// </summary>
    /// <param name="fileStream">Excel file stream</param>
    /// <param name="fileName">Original file name</param>
    /// <param name="createdBy">User or system that initiated processing</param>
    /// <returns>Processing result with job ID and summary information</returns>
    Task<ServiceResult<ExcelProcessingResult>> ProcessExcelFileAsync(
        Stream fileStream, 
        string fileName, 
        string? createdBy = null);
    
    /// <summary>
    /// Validates uploaded Excel file before processing
    /// Checks file format, size, and basic GAA data structure
    /// </summary>
    /// <param name="fileStream">Excel file stream</param>
    /// <param name="fileName">Original file name</param>
    /// <param name="fileSizeBytes">File size in bytes</param>
    /// <returns>Validation result with detailed error messages</returns>
    Task<ServiceResult<ExcelFileAnalysis>> ValidateAndAnalyzeFileAsync(
        Stream fileStream, 
        string fileName, 
        long fileSizeBytes);
    
    /// <summary>
    /// Retrieves current processing status for an ETL job
    /// </summary>
    /// <param name="jobId">ETL job identifier</param>
    /// <returns>Current progress information</returns>
    Task<ServiceResult<EtlProgressUpdate?>> GetProcessingStatusAsync(int jobId);
    
    /// <summary>
    /// Retrieves validation errors for a completed or failed ETL job
    /// </summary>
    /// <param name="jobId">ETL job identifier</param>
    /// <returns>Collection of validation errors with details</returns>
    Task<ServiceResult<IEnumerable<ValidationError>>> GetValidationErrorsAsync(int jobId);
}