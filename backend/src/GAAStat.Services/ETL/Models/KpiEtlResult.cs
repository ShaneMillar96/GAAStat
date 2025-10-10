namespace GAAStat.Services.ETL.Models;

/// <summary>
/// Result object for KPI Definitions ETL operations
/// </summary>
public class KpiEtlResult : EtlResult
{
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
    /// Sheet name processed (typically "KPI Definitions")
    /// </summary>
    public string? SheetName { get; set; }
}
