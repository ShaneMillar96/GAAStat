namespace GAAStat.Api.Models;

/// <summary>
/// Response model for ETL upload operations
/// </summary>
public class EtlUploadResponse
{
    /// <summary>
    /// Indicates if the ETL operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Number of matches processed
    /// </summary>
    public int MatchesProcessed { get; set; }

    /// <summary>
    /// Number of team statistics records created
    /// </summary>
    public int TeamStatisticsCreated { get; set; }

    /// <summary>
    /// Duration of ETL operation in seconds
    /// </summary>
    public double DurationSeconds { get; set; }

    /// <summary>
    /// List of warnings encountered during processing
    /// </summary>
    public List<EtlWarningDto> Warnings { get; set; } = new();

    /// <summary>
    /// List of errors encountered during processing
    /// </summary>
    public List<EtlErrorDto> Errors { get; set; } = new();
}

/// <summary>
/// Error details from ETL operation
/// </summary>
public class EtlErrorDto
{
    /// <summary>
    /// Error code (e.g., FILE_NOT_FOUND, VALIDATION_FAILED)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Sheet name where error occurred (if applicable)
    /// </summary>
    public string? SheetName { get; set; }
}

/// <summary>
/// Warning details from ETL operation
/// </summary>
public class EtlWarningDto
{
    /// <summary>
    /// Warning code (e.g., POSSESSION_SUM, PERIOD_MISMATCH)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable warning message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Sheet name where warning occurred (if applicable)
    /// </summary>
    public string? SheetName { get; set; }
}
