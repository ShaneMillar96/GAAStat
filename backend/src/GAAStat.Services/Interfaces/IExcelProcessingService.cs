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
    
    /// <summary>
    /// Processes team-level match statistics from Excel sheets into database
    /// Extracts team statistics with first half, second half, and full game data for both teams
    /// </summary>
    /// <param name="fileStream">Excel file stream</param>
    /// <param name="teamSheets">List of team statistics sheets to process</param>
    /// <param name="matchIdMap">Mapping of sheet names to match IDs</param>
    /// <param name="jobId">ETL job identifier for progress tracking</param>
    /// <param name="cancellationToken">Cancellation token for operation timeout</param>
    /// <returns>Processing result with statistics created and performance metrics</returns>
    Task<ServiceResult<TeamStatsProcessingResult>> ProcessTeamStatisticsAsync(
        Stream fileStream,
        List<SheetInfo> teamSheets,
        Dictionary<string, int> matchIdMap,
        int jobId,
        CancellationToken cancellationToken = default);
    
    // DISABLED: Kickout analysis method removed due to processing issues with complex visual Excel layout
    // /// <summary>
    // /// Processes kickout analysis data from Excel sheet into database
    // /// Extracts event-level kickout tracking data and creates aggregated statistics
    // /// </summary>
    // /// <param name="fileStream">Excel file stream</param>
    // /// <param name="jobId">ETL job identifier for progress tracking</param>
    // /// <param name="cancellationToken">Cancellation token for operation timeout</param>
    // /// <returns>Processing result with events extracted and records created</returns>
    // Task<ServiceResult<KickoutAnalysisResult>> ProcessKickoutAnalysisAsync(
    //     Stream fileStream,
    //     int jobId,
    //     CancellationToken cancellationToken = default);
}