namespace GAAStat.Services.ETL.Models;

/// <summary>
/// Represents team statistics for a specific period within a match.
/// Each match has 6 records: Drum (1st, 2nd, Full) + Opposition (1st, 2nd, Full)
/// </summary>
public class TeamStatisticsData
{
    /// <summary>
    /// Team name ("Drum" or opposition team name)
    /// </summary>
    public string TeamName { get; set; } = string.Empty;

    /// <summary>
    /// Period identifier: "1st", "2nd", or "Full"
    /// </summary>
    public string Period { get; set; } = string.Empty;

    /// <summary>
    /// Scoreline for this period (GAA notation: "0-04", "1-11")
    /// </summary>
    public string? Scoreline { get; set; }

    /// <summary>
    /// Total possession percentage (0-1 decimal, e.g., 0.3545 = 35.45%)
    /// </summary>
    public decimal? TotalPossession { get; set; }

    #region Score Source Fields (8 fields)

    /// <summary>
    /// Scores from Drum kickout (long)
    /// </summary>
    public int? ScoreSourceKickoutLong { get; set; }

    /// <summary>
    /// Scores from Drum kickout (short)
    /// </summary>
    public int? ScoreSourceKickoutShort { get; set; }

    /// <summary>
    /// Scores from opposition kickout (long)
    /// </summary>
    public int? ScoreSourceOppKickoutLong { get; set; }

    /// <summary>
    /// Scores from opposition kickout (short)
    /// </summary>
    public int? ScoreSourceOppKickoutShort { get; set; }

    /// <summary>
    /// Scores from turnovers
    /// </summary>
    public int? ScoreSourceTurnover { get; set; }

    /// <summary>
    /// Scores from opposition possession lost
    /// </summary>
    public int? ScoreSourcePossessionLost { get; set; }

    /// <summary>
    /// Scores from shot short
    /// </summary>
    public int? ScoreSourceShotShort { get; set; }

    /// <summary>
    /// Scores from throw up/in
    /// </summary>
    public int? ScoreSourceThrowUpIn { get; set; }

    #endregion

    #region Shot Source Fields (8 fields)

    /// <summary>
    /// Shots from Drum kickout (long)
    /// </summary>
    public int? ShotSourceKickoutLong { get; set; }

    /// <summary>
    /// Shots from Drum kickout (short)
    /// </summary>
    public int? ShotSourceKickoutShort { get; set; }

    /// <summary>
    /// Shots from opposition kickout (long)
    /// </summary>
    public int? ShotSourceOppKickoutLong { get; set; }

    /// <summary>
    /// Shots from opposition kickout (short)
    /// </summary>
    public int? ShotSourceOppKickoutShort { get; set; }

    /// <summary>
    /// Shots from turnovers
    /// </summary>
    public int? ShotSourceTurnover { get; set; }

    /// <summary>
    /// Shots from opposition possession lost
    /// </summary>
    public int? ShotSourcePossessionLost { get; set; }

    /// <summary>
    /// Shots from shot short
    /// </summary>
    public int? ShotSourceShotShort { get; set; }

    /// <summary>
    /// Shots from throw up/in
    /// </summary>
    public int? ShotSourceThrowUpIn { get; set; }

    #endregion
}
