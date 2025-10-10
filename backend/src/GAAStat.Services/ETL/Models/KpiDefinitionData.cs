namespace GAAStat.Services.ETL.Models;

/// <summary>
/// Represents a single KPI definition extracted from Excel.
/// Maps directly to "KPI Definitions" sheet columns.
/// </summary>
public class KpiDefinitionData
{
    /// <summary>
    /// Event number for grouping (Excel Column A: "Event #")
    /// </summary>
    public int EventNumber { get; set; }

    /// <summary>
    /// Event name (Excel Column B: "Event Name")
    /// Example: "Kickout", "Attacks", "Shot from play"
    /// </summary>
    public string EventName { get; set; } = string.Empty;

    /// <summary>
    /// Outcome description (Excel Column C: "Outcome")
    /// Example: "Won clean", "Point", "Goal"
    /// </summary>
    public string Outcome { get; set; } = string.Empty;

    /// <summary>
    /// Team assignment (Excel Column D: "Assign to which team")
    /// Valid values: "Home", "Opposition", "Both"
    /// </summary>
    public string TeamAssignment { get; set; } = string.Empty;

    /// <summary>
    /// PSR (Possession Success Rate) value (Excel Column E: "PSR Value")
    /// Typically ranges from 0.0 to 3.0
    /// </summary>
    public decimal PsrValue { get; set; }

    /// <summary>
    /// Definition/description (Excel Column F: "Definition")
    /// </summary>
    public string Definition { get; set; } = string.Empty;

    /// <summary>
    /// Original Excel row number (for error reporting)
    /// </summary>
    public int SourceRowNumber { get; set; }
}
