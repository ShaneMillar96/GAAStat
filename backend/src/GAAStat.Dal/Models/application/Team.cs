using System;
using System.Collections.Generic;

namespace GAAStat.Dal.Models.Application;

/// <summary>
/// Represents a GAA team (Drum or opponent)
/// </summary>
public class Team
{
    /// <summary>
    /// Primary key
    /// </summary>
    public int TeamId { get; set; }

    /// <summary>
    /// Team name (e.g., "Drum", "Slaughtmanus")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Team abbreviation (optional, e.g., "DRM")
    /// </summary>
    public string? Abbreviation { get; set; }

    /// <summary>
    /// True only for Drum team
    /// </summary>
    public bool IsDrum { get; set; }

    /// <summary>
    /// False for inactive/historical teams
    /// </summary>
    public bool IsActive { get; set; }

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
    /// Matches where this team is home
    /// </summary>
    public virtual ICollection<Match> HomeMatches { get; set; } = new List<Match>();

    /// <summary>
    /// Matches where this team is away
    /// </summary>
    public virtual ICollection<Match> AwayMatches { get; set; } = new List<Match>();

    /// <summary>
    /// Team statistics across all matches
    /// </summary>
    public virtual ICollection<MatchTeamStatistics> TeamStatistics { get; set; } = new List<MatchTeamStatistics>();
}
