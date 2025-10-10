namespace GAAStat.Api.Models;

/// <summary>
/// Response model for KPI Definitions ETL upload operations
/// </summary>
public class KpiDefinitionsEtlResponse
{
    /// <summary>
    /// Indicates if the ETL operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Number of KPI definitions created
    /// </summary>
    public int KpiDefinitionsCreated { get; set; }

    /// <summary>
    /// Number of KPI definitions updated (for idempotent re-imports)
    /// </summary>
    public int KpiDefinitionsUpdated { get; set; }

    /// <summary>
    /// Number of KPI definitions skipped (validation failures)
    /// </summary>
    public int KpiDefinitionsSkipped { get; set; }

    /// <summary>
    /// Total number of rows processed from KPI Definitions sheet
    /// </summary>
    public int RowsProcessed { get; set; }

    /// <summary>
    /// Duration of ETL operation in seconds
    /// </summary>
    public double DurationSeconds { get; set; }

    /// <summary>
    /// Sheet name processed (typically "KPI Definitions")
    /// </summary>
    public string? SheetName { get; set; }

    /// <summary>
    /// List of warnings encountered during processing
    /// </summary>
    public List<EtlWarningDto> Warnings { get; set; } = new();

    /// <summary>
    /// List of errors encountered during processing
    /// </summary>
    public List<EtlErrorDto> Errors { get; set; } = new();
}
