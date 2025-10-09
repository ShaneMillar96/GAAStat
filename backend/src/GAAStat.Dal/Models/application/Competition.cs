using System;
using System.Collections.Generic;

namespace GAAStat.Dal.Models.Application;

/// <summary>
/// Represents a competition type within a season (Championship, League, Cup, Friendly)
/// </summary>
public class Competition
{
    /// <summary>
    /// Primary key
    /// </summary>
    public int CompetitionId { get; set; }

    /// <summary>
    /// Foreign key to Season
    /// </summary>
    public int SeasonId { get; set; }

    /// <summary>
    /// Competition name (e.g., "Championship", "League")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Competition type: Championship, League, Cup, or Friendly
    /// </summary>
    public string Type { get; set; } = string.Empty;

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
    /// The season this competition belongs to
    /// </summary>
    public virtual Season Season { get; set; } = null!;

    /// <summary>
    /// Matches in this competition
    /// </summary>
    public virtual ICollection<Match> Matches { get; set; } = new List<Match>();
}
