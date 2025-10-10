namespace GAAStat.Api.Models;

/// <summary>
/// Generic dashboard API response wrapper
/// </summary>
/// <typeparam name="T">The type of data being returned</typeparam>
public class DashboardResponseDto<T>
{
    /// <summary>
    /// Indicates if the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The response data (null if unsuccessful)
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Duration of the operation in milliseconds
    /// </summary>
    public double DurationMs { get; set; }

    /// <summary>
    /// List of errors that occurred (empty if successful)
    /// </summary>
    public List<ErrorDto> Errors { get; set; } = new();

    /// <summary>
    /// List of warnings (operation can still be successful with warnings)
    /// </summary>
    public List<WarningDto> Warnings { get; set; } = new();
}

/// <summary>
/// Error details for API responses
/// </summary>
public class ErrorDto
{
    /// <summary>
    /// Error code (e.g., "NO_SEASON", "NO_DRUM_TEAM")
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable error message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Warning details for API responses
/// </summary>
public class WarningDto
{
    /// <summary>
    /// Warning code (e.g., "NO_MATCHES", "NO_STATS")
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable warning message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
