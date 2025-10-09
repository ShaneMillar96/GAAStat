using System;

namespace GAAStat.Dal.Models.Application;

/// <summary>
/// Represents a KPI or metric definition with PSR values
/// Maps to Excel "KPI Definitions" sheet
/// </summary>
public class KpiDefinition
{
    /// <summary>
    /// Primary key
    /// </summary>
    public int KpiId { get; set; }

    /// <summary>
    /// Event number for grouping related events
    /// </summary>
    public int EventNumber { get; set; }

    /// <summary>
    /// Event name (e.g., "Kickout", "Attacks", "Shot from play")
    /// </summary>
    public string EventName { get; set; } = string.Empty;

    /// <summary>
    /// Outcome of the event (e.g., "Won clean", "Point", "Goal")
    /// </summary>
    public string Outcome { get; set; } = string.Empty;

    /// <summary>
    /// Team assignment: Home, Opposition, or Both
    /// </summary>
    public string TeamAssignment { get; set; } = string.Empty;

    /// <summary>
    /// Possession Success Rate value for this outcome
    /// </summary>
    public decimal PsrValue { get; set; }

    /// <summary>
    /// Definition/description of this KPI
    /// </summary>
    public string Definition { get; set; } = string.Empty;

    /// <summary>
    /// Record creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
