using System;
using System.Collections.Generic;

namespace GAAStat.Dal.Models.Application;

/// <summary>
/// Represents a GAA match
/// </summary>
public class Match
{
    /// <summary>
    /// Primary key
    /// </summary>
    public int MatchId { get; set; }

    /// <summary>
    /// Foreign key to Competition
    /// </summary>
    public int CompetitionId { get; set; }

    /// <summary>
    /// Sequential match number in season
    /// </summary>
    public int MatchNumber { get; set; }

    /// <summary>
    /// Foreign key to home team
    /// </summary>
    public int HomeTeamId { get; set; }

    /// <summary>
    /// Foreign key to away team
    /// </summary>
    public int AwayTeamId { get; set; }

    /// <summary>
    /// Date of the match
    /// </summary>
    public DateTime MatchDate { get; set; }

    /// <summary>
    /// Venue: Home, Away, or Neutral
    /// </summary>
    public string Venue { get; set; } = string.Empty;

    /// <summary>
    /// Home team score for 1st half in GAA notation (e.g., "0-04")
    /// </summary>
    public string? HomeScoreFirstHalf { get; set; }

    /// <summary>
    /// Home team score for 2nd half in GAA notation (e.g., "1-07")
    /// </summary>
    public string? HomeScoreSecondHalf { get; set; }

    /// <summary>
    /// Home team full-time score in GAA notation (e.g., "1-11")
    /// </summary>
    public string? HomeScoreFullTime { get; set; }

    /// <summary>
    /// Away team score for 1st half in GAA notation (e.g., "0-10")
    /// </summary>
    public string? AwayScoreFirstHalf { get; set; }

    /// <summary>
    /// Away team score for 2nd half in GAA notation (e.g., "0-06")
    /// </summary>
    public string? AwayScoreSecondHalf { get; set; }

    /// <summary>
    /// Away team full-time score in GAA notation (e.g., "0-16")
    /// </summary>
    public string? AwayScoreFullTime { get; set; }

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
    /// The competition this match belongs to
    /// </summary>
    public virtual Competition Competition { get; set; } = null!;

    /// <summary>
    /// The home team
    /// </summary>
    public virtual Team HomeTeam { get; set; } = null!;

    /// <summary>
    /// The away team
    /// </summary>
    public virtual Team AwayTeam { get; set; } = null!;

    /// <summary>
    /// Team statistics for this match (6 records: 3 periods × 2 teams)
    /// </summary>
    public virtual ICollection<MatchTeamStatistics> TeamStatistics { get; set; } = new List<MatchTeamStatistics>();

    /// <summary>
    /// Player statistics for this match
    /// </summary>
    public virtual ICollection<PlayerMatchStatistics> PlayerStatistics { get; set; } = new List<PlayerMatchStatistics>();
}
