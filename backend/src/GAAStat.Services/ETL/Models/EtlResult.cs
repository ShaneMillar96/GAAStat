namespace GAAStat.Services.ETL.Models;

/// <summary>
/// Represents the result of an ETL pipeline execution.
/// Contains success/failure status, processing statistics, and error details.
/// </summary>
public class EtlResult
{
    /// <summary>
    /// Indicates whether the ETL process completed successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Timestamp when ETL process started
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Timestamp when ETL process ended
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Total duration of ETL process
    /// </summary>
    public TimeSpan Duration => EndTime.HasValue ? EndTime.Value - StartTime : TimeSpan.Zero;

    /// <summary>
    /// Number of matches successfully processed
    /// </summary>
    public int MatchesProcessed { get; set; }

    /// <summary>
    /// Number of match team statistics records created (should be MatchesProcessed × 6)
    /// </summary>
    public int TeamStatisticsCreated { get; set; }

    /// <summary>
    /// Number of seasons created
    /// </summary>
    public int SeasonsCreated { get; set; }

    /// <summary>
    /// Number of competitions created
    /// </summary>
    public int CompetitionsCreated { get; set; }

    /// <summary>
    /// Number of teams created
    /// </summary>
    public int TeamsCreated { get; set; }

    /// <summary>
    /// List of errors encountered during processing
    /// </summary>
    public List<EtlError> Errors { get; set; } = new();

    /// <summary>
    /// List of warnings encountered during processing
    /// </summary>
    public List<EtlWarning> Warnings { get; set; } = new();

    /// <summary>
    /// Adds an error to the result
    /// </summary>
    public void AddError(string code, string message, string? sheetName = null)
    {
        Errors.Add(new EtlError
        {
            Code = code,
            Message = message,
            SheetName = sheetName,
            Timestamp = DateTime.UtcNow
        });
        Success = false;
    }

    /// <summary>
    /// Adds a warning to the result
    /// </summary>
    public void AddWarning(string code, string message, string? sheetName = null)
    {
        Warnings.Add(new EtlWarning
        {
            Code = code,
            Message = message,
            SheetName = sheetName,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static EtlResult CreateSuccess()
    {
        return new EtlResult
        {
            Success = true,
            StartTime = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a failed result with an error
    /// </summary>
    public static EtlResult CreateFailure(string code, string message)
    {
        var result = new EtlResult
        {
            Success = false,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow
        };
        result.AddError(code, message);
        return result;
    }
}

/// <summary>
/// Represents an error encountered during ETL processing
/// </summary>
public class EtlError
{
    /// <summary>
    /// Error code for categorization
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Detailed error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Sheet name where error occurred (if applicable)
    /// </summary>
    public string? SheetName { get; set; }

    /// <summary>
    /// Timestamp when error occurred
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Represents a warning encountered during ETL processing
/// </summary>
public class EtlWarning
{
    /// <summary>
    /// Warning code for categorization
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Warning message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Sheet name where warning occurred (if applicable)
    /// </summary>
    public string? SheetName { get; set; }

    /// <summary>
    /// Timestamp when warning occurred
    /// </summary>
    public DateTime Timestamp { get; set; }
}
