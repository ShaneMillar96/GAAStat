using System;

namespace GAAStat.Services.ETL.Models;

/// <summary>
/// Represents player statistics data extracted from Excel sheet.
/// Contains 86 statistical fields plus player identification.
/// </summary>
public class PlayerStatisticsData
{
    // === Player Identification ===

    /// <summary>
    /// Player jersey number (1-99)
    /// </summary>
    public int JerseyNumber { get; set; }

    /// <summary>
    /// Player full name (first + last)
    /// </summary>
    public string PlayerName { get; set; } = string.Empty;

    /// <summary>
    /// Position code (GK/DEF/MID/FWD)
    /// </summary>
    public string PositionCode { get; set; } = string.Empty;

    // === Summary Statistics (8 fields) ===

    /// <summary>
    /// Minutes played (0-120)
    /// </summary>
    public int MinutesPlayed { get; set; }

    /// <summary>
    /// Total engagements
    /// </summary>
    public int TotalEngagements { get; set; }

    /// <summary>
    /// Total engagements per PSR
    /// </summary>
    public decimal? TePerPsr { get; set; }

    /// <summary>
    /// Scores in GAA notation (e.g., "1-03(1f)")
    /// </summary>
    public string? Scores { get; set; }

    /// <summary>
    /// PSR (Possession Success Rate)
    /// </summary>
    public int Psr { get; set; }

    /// <summary>
    /// PSR per total possessions
    /// </summary>
    public decimal? PsrPerTp { get; set; }

    // === Possession Play (13 fields) ===

    public int Tp { get; set; }
    public int Tow { get; set; }
    public int Interceptions { get; set; }
    public int Tpl { get; set; }
    public int Kp { get; set; }
    public int Hp { get; set; }
    public int Ha { get; set; }
    public int Turnovers { get; set; }
    public int Ineffective { get; set; }
    public int ShotShort { get; set; }
    public int ShotSave { get; set; }
    public int Fouled { get; set; }
    public int Woodwork { get; set; }

    // === Kickout Analysis - Drum (4 fields) ===

    public int KoDrumKow { get; set; }
    public int KoDrumWc { get; set; }
    public int KoDrumBw { get; set; }
    public int KoDrumSw { get; set; }

    // === Kickout Analysis - Opposition (4 fields) ===

    public int KoOppKow { get; set; }
    public int KoOppWc { get; set; }
    public int KoOppBw { get; set; }
    public int KoOppSw { get; set; }

    // === Attacking Play (5 fields) ===

    public int Ta { get; set; }
    public int Kr { get; set; }
    public int Kl { get; set; }
    public int Cr { get; set; }
    public int Cl { get; set; }

    // === Shots from Play (11 fields) ===

    public int ShotsPlayTotal { get; set; }
    public int ShotsPlayPoints { get; set; }
    public int ShotsPlay2Points { get; set; }
    public int ShotsPlayGoals { get; set; }
    public int ShotsPlayWide { get; set; }
    public int ShotsPlayShort { get; set; }
    public int ShotsPlaySave { get; set; }
    public int ShotsPlayWoodwork { get; set; }
    public int ShotsPlayBlocked { get; set; }
    public int ShotsPlay45 { get; set; }
    public decimal? ShotsPlayPercentage { get; set; }

    // === Scoreable Frees (11 fields) ===

    public int FreesTotal { get; set; }
    public int FreesPoints { get; set; }
    public int Frees2Points { get; set; }
    public int FreesGoals { get; set; }
    public int FreesWide { get; set; }
    public int FreesShort { get; set; }
    public int FreesSave { get; set; }
    public int FreesWoodwork { get; set; }
    public int Frees45 { get; set; }
    public int FreesQf { get; set; }
    public decimal? FreesPercentage { get; set; }

    // === Total Shots (2 fields) ===

    public int TotalShots { get; set; }
    public decimal? TotalShotsPercentage { get; set; }

    // === Assists (3 fields) ===

    public int AssistsTotal { get; set; }
    public int AssistsPoint { get; set; }
    public int AssistsGoal { get; set; }

    // === Tackles (4 fields) ===

    public int TacklesTotal { get; set; }
    public int TacklesContested { get; set; }
    public int TacklesMissed { get; set; }
    public decimal? TacklesPercentage { get; set; }

    // === Frees Conceded (5 fields) ===

    public int FreesConcededTotal { get; set; }
    public int FreesConcededAttack { get; set; }
    public int FreesConcededMidfield { get; set; }
    public int FreesConcededDefense { get; set; }
    public int FreesConcededPenalty { get; set; }

    // === 50m Free Conceded (4 fields) ===

    public int Frees50mTotal { get; set; }
    public int Frees50mDelay { get; set; }
    public int Frees50mDissent { get; set; }
    public int Frees50m3v3 { get; set; }

    // === Bookings (3 fields) ===

    public int YellowCards { get; set; }
    public int BlackCards { get; set; }
    public int RedCards { get; set; }

    // === Throw Up (2 fields) ===

    public int ThrowUpWon { get; set; }
    public int ThrowUpLost { get; set; }

    // === Goalkeeper Stats (5 fields) ===

    public int GkTotalKickouts { get; set; }
    public int GkKickoutRetained { get; set; }
    public int GkKickoutLost { get; set; }
    public decimal? GkKickoutPercentage { get; set; }
    public int GkSaves { get; set; }
}
