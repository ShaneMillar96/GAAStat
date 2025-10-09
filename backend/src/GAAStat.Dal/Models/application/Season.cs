using System;
using System.Collections.Generic;

namespace GAAStat.Dal.Models.Application;

/// <summary>
/// Represents a GAA season
/// </summary>
public class Season
{
    /// <summary>
    /// Primary key
    /// </summary>
    public int SeasonId { get; set; }

    /// <summary>
    /// Season year (e.g., 2025)
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Season name (e.g., "2025 Season")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Season start date (optional)
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Season end date (optional)
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// True if this is the currently active season
    /// </summary>
    public bool IsCurrent { get; set; }

    /// <summary>
    /// Record creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Record last updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    /// <summary>
    /// Competitions within this season
    /// </summary>
    public virtual ICollection<Competition> Competitions { get; set; } = new List<Competition>();
}
