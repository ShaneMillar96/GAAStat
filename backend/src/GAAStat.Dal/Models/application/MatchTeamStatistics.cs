using System;

namespace GAAStat.Dal.Models.Application;

/// <summary>
/// Represents team-level statistics for a specific period of a match
/// 6 records per match: 3 periods (1st, 2nd, Full) × 2 teams
/// </summary>
public class MatchTeamStatistics
{
    /// <summary>
    /// Primary key
    /// </summary>
    public int MatchTeamStatId { get; set; }

    /// <summary>
    /// Foreign key to Match
    /// </summary>
    public int MatchId { get; set; }

    /// <summary>
    /// Foreign key to Team
    /// </summary>
    public int TeamId { get; set; }

    /// <summary>
    /// Period: 1st, 2nd, or Full
    /// </summary>
    public string Period { get; set; } = string.Empty;

    /// <summary>
    /// Scoreline in GAA notation (e.g., "0-04", "1-11")
    /// </summary>
    public string? Scoreline { get; set; }

    /// <summary>
    /// Total possession as decimal (0-1, e.g., 0.3545 = 35.45%)
    /// </summary>
    public decimal? TotalPossession { get; set; }

    // Score Source Statistics (8 fields)
    public int ScoreSourceKickoutLong { get; set; }
    public int ScoreSourceKickoutShort { get; set; }
    public int ScoreSourceOppKickoutLong { get; set; }
    public int ScoreSourceOppKickoutShort { get; set; }
    public int ScoreSourceTurnover { get; set; }
    public int ScoreSourcePossessionLost { get; set; }
    public int ScoreSourceShotShort { get; set; }
    public int ScoreSourceThrowUpIn { get; set; }

    // Shot Source Statistics (8 fields)
    public int ShotSourceKickoutLong { get; set; }
    public int ShotSourceKickoutShort { get; set; }
    public int ShotSourceOppKickoutLong { get; set; }
    public int ShotSourceOppKickoutShort { get; set; }
    public int ShotSourceTurnover { get; set; }
    public int ShotSourcePossessionLost { get; set; }
    public int ShotSourceShotShort { get; set; }
    public int ShotSourceThrowUpIn { get; set; }

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
    /// The match these statistics belong to
    /// </summary>
    public virtual Match Match { get; set; } = null!;

    /// <summary>
    /// The team these statistics belong to
    /// </summary>
    public virtual Team Team { get; set; } = null!;
}
