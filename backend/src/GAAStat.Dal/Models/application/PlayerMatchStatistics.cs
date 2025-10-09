using System;

namespace GAAStat.Dal.Models.Application;

/// <summary>
/// Represents individual player performance metrics for a match (86+ fields)
/// Maps to Excel "Player Stats" sheets
/// </summary>
public class PlayerMatchStatistics
{
    /// <summary>
    /// Primary key
    /// </summary>
    public int PlayerMatchStatId { get; set; }

    /// <summary>
    /// Foreign key to Match
    /// </summary>
    public int MatchId { get; set; }

    /// <summary>
    /// Foreign key to Player
    /// </summary>
    public int PlayerId { get; set; }

    // =========================================================================
    // Summary Statistics (6 fields)
    // =========================================================================

    /// <summary>
    /// Minutes played (0-120, allowing extra time)
    /// </summary>
    public int MinutesPlayed { get; set; }

    /// <summary>
    /// Total Engagements
    /// </summary>
    public int TotalEngagements { get; set; }

    /// <summary>
    /// Total Engagements per Possession Success Rate
    /// </summary>
    public decimal? TePerPsr { get; set; }

    /// <summary>
    /// Scores in GAA notation (e.g., "1-03(1f)" = 1 goal, 3 points, 1 from free)
    /// </summary>
    public string? Scores { get; set; }

    /// <summary>
    /// Possession Success Rate
    /// </summary>
    public int Psr { get; set; }

    /// <summary>
    /// PSR per Total Possessions
    /// </summary>
    public decimal? PsrPerTp { get; set; }

    // =========================================================================
    // Possession Play (13 fields)
    // =========================================================================

    /// <summary>
    /// Total Possessions
    /// </summary>
    public int Tp { get; set; }

    /// <summary>
    /// Turnover Won
    /// </summary>
    public int Tow { get; set; }

    /// <summary>
    /// Interceptions
    /// </summary>
    public int Interceptions { get; set; }

    /// <summary>
    /// Turnover Possession Lost
    /// </summary>
    public int Tpl { get; set; }

    /// <summary>
    /// Kick Pass
    /// </summary>
    public int Kp { get; set; }

    /// <summary>
    /// Hand Pass
    /// </summary>
    public int Hp { get; set; }

    /// <summary>
    /// Hand Pass Attempted
    /// </summary>
    public int Ha { get; set; }

    /// <summary>
    /// Turnovers
    /// </summary>
    public int Turnovers { get; set; }

    /// <summary>
    /// Ineffective plays
    /// </summary>
    public int Ineffective { get; set; }

    /// <summary>
    /// Shot Short
    /// </summary>
    public int ShotShort { get; set; }

    /// <summary>
    /// Shot Save
    /// </summary>
    public int ShotSave { get; set; }

    /// <summary>
    /// Fouled
    /// </summary>
    public int Fouled { get; set; }

    /// <summary>
    /// Woodwork (shot hit post/crossbar)
    /// </summary>
    public int Woodwork { get; set; }

    // =========================================================================
    // Kickout Analysis - Drum (4 fields)
    // =========================================================================

    /// <summary>
    /// Drum Kickout Won
    /// </summary>
    public int KoDrumKow { get; set; }

    /// <summary>
    /// Drum Kickout Won Clean
    /// </summary>
    public int KoDrumWc { get; set; }

    /// <summary>
    /// Drum Kickout Break Won
    /// </summary>
    public int KoDrumBw { get; set; }

    /// <summary>
    /// Drum Kickout Short Won
    /// </summary>
    public int KoDrumSw { get; set; }

    // =========================================================================
    // Kickout Analysis - Opposition (4 fields)
    // =========================================================================

    /// <summary>
    /// Opposition Kickout Won
    /// </summary>
    public int KoOppKow { get; set; }

    /// <summary>
    /// Opposition Kickout Won Clean
    /// </summary>
    public int KoOppWc { get; set; }

    /// <summary>
    /// Opposition Kickout Break Won
    /// </summary>
    public int KoOppBw { get; set; }

    /// <summary>
    /// Opposition Kickout Short Won
    /// </summary>
    public int KoOppSw { get; set; }

    // =========================================================================
    // Attacking Play (5 fields)
    // =========================================================================

    /// <summary>
    /// Total Attacks
    /// </summary>
    public int Ta { get; set; }

    /// <summary>
    /// Kick Retained
    /// </summary>
    public int Kr { get; set; }

    /// <summary>
    /// Kick Lost
    /// </summary>
    public int Kl { get; set; }

    /// <summary>
    /// Carry Retained
    /// </summary>
    public int Cr { get; set; }

    /// <summary>
    /// Carry Lost
    /// </summary>
    public int Cl { get; set; }

    // =========================================================================
    // Shots from Play (11 fields)
    // =========================================================================

    /// <summary>
    /// Total shots from play
    /// </summary>
    public int ShotsPlayTotal { get; set; }

    /// <summary>
    /// Points scored from play
    /// </summary>
    public int ShotsPlayPoints { get; set; }

    /// <summary>
    /// 2-pointers scored from play (outside 40m arc)
    /// </summary>
    public int ShotsPlay2Points { get; set; }

    /// <summary>
    /// Goals scored from play
    /// </summary>
    public int ShotsPlayGoals { get; set; }

    /// <summary>
    /// Wide shots from play
    /// </summary>
    public int ShotsPlayWide { get; set; }

    /// <summary>
    /// Short shots from play
    /// </summary>
    public int ShotsPlayShort { get; set; }

    /// <summary>
    /// Shots saved by goalkeeper from play
    /// </summary>
    public int ShotsPlaySave { get; set; }

    /// <summary>
    /// Shots hit woodwork from play
    /// </summary>
    public int ShotsPlayWoodwork { get; set; }

    /// <summary>
    /// Shots blocked from play
    /// </summary>
    public int ShotsPlayBlocked { get; set; }

    /// <summary>
    /// 45-yard frees awarded from play
    /// </summary>
    public int ShotsPlay45 { get; set; }

    /// <summary>
    /// Shooting percentage from play (0-1)
    /// </summary>
    public decimal? ShotsPlayPercentage { get; set; }

    // =========================================================================
    // Scoreable Frees (11 fields)
    // =========================================================================

    /// <summary>
    /// Total scoreable frees
    /// </summary>
    public int FreesTotal { get; set; }

    /// <summary>
    /// Points scored from frees
    /// </summary>
    public int FreesPoints { get; set; }

    /// <summary>
    /// 2-pointers scored from frees
    /// </summary>
    public int Frees2Points { get; set; }

    /// <summary>
    /// Goals scored from frees
    /// </summary>
    public int FreesGoals { get; set; }

    /// <summary>
    /// Wide frees
    /// </summary>
    public int FreesWide { get; set; }

    /// <summary>
    /// Short frees
    /// </summary>
    public int FreesShort { get; set; }

    /// <summary>
    /// Frees saved by goalkeeper
    /// </summary>
    public int FreesSave { get; set; }

    /// <summary>
    /// Frees hit woodwork
    /// </summary>
    public int FreesWoodwork { get; set; }

    /// <summary>
    /// 45-yard frees awarded from frees
    /// </summary>
    public int Frees45 { get; set; }

    /// <summary>
    /// Quick frees
    /// </summary>
    public int FreesQf { get; set; }

    /// <summary>
    /// Free-taking percentage (0-1)
    /// </summary>
    public decimal? FreesPercentage { get; set; }

    // =========================================================================
    // Total Shots (2 fields)
    // =========================================================================

    /// <summary>
    /// Total shots (play + frees)
    /// </summary>
    public int TotalShots { get; set; }

    /// <summary>
    /// Overall shooting percentage (0-1)
    /// </summary>
    public decimal? TotalShotsPercentage { get; set; }

    // =========================================================================
    // Assists (3 fields)
    // =========================================================================

    /// <summary>
    /// Total assists
    /// </summary>
    public int AssistsTotal { get; set; }

    /// <summary>
    /// Point assists
    /// </summary>
    public int AssistsPoint { get; set; }

    /// <summary>
    /// Goal assists
    /// </summary>
    public int AssistsGoal { get; set; }

    // =========================================================================
    // Tackles (4 fields)
    // =========================================================================

    /// <summary>
    /// Total tackles
    /// </summary>
    public int TacklesTotal { get; set; }

    /// <summary>
    /// Contested tackles
    /// </summary>
    public int TacklesContested { get; set; }

    /// <summary>
    /// Missed tackles
    /// </summary>
    public int TacklesMissed { get; set; }

    /// <summary>
    /// Tackle success rate (0-1)
    /// </summary>
    public decimal? TacklesPercentage { get; set; }

    // =========================================================================
    // Frees Conceded (5 fields)
    // =========================================================================

    /// <summary>
    /// Total frees conceded
    /// </summary>
    public int FreesConcededTotal { get; set; }

    /// <summary>
    /// Frees conceded in attacking third
    /// </summary>
    public int FreesConcededAttack { get; set; }

    /// <summary>
    /// Frees conceded in midfield
    /// </summary>
    public int FreesConcededMidfield { get; set; }

    /// <summary>
    /// Frees conceded in defensive third
    /// </summary>
    public int FreesConcededDefense { get; set; }

    /// <summary>
    /// Penalties conceded
    /// </summary>
    public int FreesConcededPenalty { get; set; }

    // =========================================================================
    // 50m Free Conceded (4 fields)
    // =========================================================================

    /// <summary>
    /// Total 50m frees conceded
    /// </summary>
    public int Frees50MTotal { get; set; }

    /// <summary>
    /// 50m frees for delaying the game
    /// </summary>
    public int Frees50MDelay { get; set; }

    /// <summary>
    /// 50m frees for dissent
    /// </summary>
    public int Frees50MDissent { get; set; }

    /// <summary>
    /// 50m frees for 3v3 infringement
    /// </summary>
    public int Frees50M3V3 { get; set; }

    // =========================================================================
    // Bookings (3 fields)
    // =========================================================================

    /// <summary>
    /// Yellow cards received
    /// </summary>
    public int YellowCards { get; set; }

    /// <summary>
    /// Black cards received
    /// </summary>
    public int BlackCards { get; set; }

    /// <summary>
    /// Red cards received
    /// </summary>
    public int RedCards { get; set; }

    // =========================================================================
    // Throw Up (2 fields)
    // =========================================================================

    /// <summary>
    /// Throw-ups won
    /// </summary>
    public int ThrowUpWon { get; set; }

    /// <summary>
    /// Throw-ups lost
    /// </summary>
    public int ThrowUpLost { get; set; }

    // =========================================================================
    // Goalkeeper Stats (5 fields)
    // =========================================================================

    /// <summary>
    /// Total kickouts (GK only)
    /// </summary>
    public int GkTotalKickouts { get; set; }

    /// <summary>
    /// Kickouts retained (GK only)
    /// </summary>
    public int GkKickoutRetained { get; set; }

    /// <summary>
    /// Kickouts lost (GK only)
    /// </summary>
    public int GkKickoutLost { get; set; }

    /// <summary>
    /// Kickout retention percentage (GK only, 0-1)
    /// </summary>
    public decimal? GkKickoutPercentage { get; set; }

    /// <summary>
    /// Saves made (GK only)
    /// </summary>
    public int GkSaves { get; set; }

    // =========================================================================
    // Timestamps
    // =========================================================================

    /// <summary>
    /// Record creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Record last updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// The match these statistics belong to
    /// </summary>
    public virtual Match Match { get; set; } = null!;

    /// <summary>
    /// The player these statistics belong to
    /// </summary>
    public virtual Player Player { get; set; } = null!;
}
